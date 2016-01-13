

using System;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Threading;
using Windows.Foundation;
using Windows.System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Windows.Devices.Bluetooth;

namespace BluetoothApp.Engine
{
    public delegate void ObexConnectionStatusChanged(bool connected, string error);
    public delegate void ObexErrorCallback(string error);
    public delegate void ObexMessage(string message);

    public class BTEngine
    {
        public event ObexConnectionStatusChanged ObexConnectionStatusChanged;
        public event ObexErrorCallback ObexErrorCallback;
        public event ObexMessage ObexMessage;

        public static readonly Guid btUuid = Guid.Parse("6f5b75e8-be27-4684-a776-3238826d1a91");

        private const uint SERVICE_NAME_ATTRIBUTE_ID = 0x100;
        private const byte SERVICE_NAME_ATTRIBUTE_TYPE = (4 << 3) | 5;
        private const string SERVICE_NAME = "BTSerial by DrJukka";

        // provider & listener are used for incoming connections
        private RfcommServiceProvider _provider;
        private StreamSocketListener _listener;
        private IAsyncAction receivingThread;
        private CancellationTokenSource cancellationTokenSource;
        private bool cancelReceiving = false;

        //seleted device used for connecting out
        private RfcommDeviceService _deviceService;

        //this is either outgoing or incoming socket connection
        private StreamSocket _streamSocket;

        private static BTEngine _instance = new BTEngine();

        public static BTEngine Instance
        {
            get { return _instance; }
        }

        public Guid BTGUID
        {
            get { return btUuid; }
        }


        public StreamSocket Socket
        {
            get { return _streamSocket; }
        }

        public RfcommDeviceService SelectedDevice
        {
            get { return _deviceService; }
            set { _deviceService = value; }
        }

        public async void InitializeReceiver()
        {
            // Initialize the provider for the hosted RFCOMM service // RfcommServiceId FromUuid(Guid uuid);
            _provider = await RfcommServiceProvider.CreateAsync(RfcommServiceId.FromUuid(btUuid));

            // Create a listener for this service and start listening
            _listener = new StreamSocketListener();
            _listener.ConnectionReceived += Listener_ConnectionReceived;

            await _listener.BindServiceNameAsync(_provider.ServiceId.AsString(), SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);

            // Set the SDP attributes and start advertising
            InitializeServiceSdpAttributes(_provider);
            _provider.StartAdvertising(_listener);
        }

        public void DeInitialize()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
            cancelReceiving = true;

            _provider.StopAdvertising();
            _provider = null;

            _listener.Dispose();
            _listener = null;

            _streamSocket.Dispose();
            _streamSocket = null;
        }

        public async void ConnectToDevice(RfcommDeviceService device)
        {
            //connect the socket   
            try
            {
                _streamSocket = new StreamSocket();

                await _streamSocket.ConnectAsync(
                 device.ConnectionHostName,
                 device.ConnectionServiceName,
                 SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);
            
                if (ObexConnectionStatusChanged != null)
                {
                    ObexConnectionStatusChanged(true, null);
                }
                ReceiveData();
            }
            catch (Exception ex)
            {
                if (ObexConnectionStatusChanged != null)
                {
                    ObexConnectionStatusChanged(false, "Cannot connect bluetooth device:" + ex.Message);
                }
            }
        }

        public async void SendData(string message)
        {
            if (_streamSocket == null || string.IsNullOrEmpty(message))
            {
                return;
            }
            try
            {
                DataWriter dwriter = new DataWriter(_streamSocket.OutputStream);
                UInt32 len = dwriter.MeasureString(message);
                dwriter.WriteUInt32(len);
                dwriter.WriteString(message);
                await dwriter.StoreAsync();
                await dwriter.FlushAsync();
            }
            catch (Exception ex)
            {
                if (ObexErrorCallback != null)
                {
                    ObexErrorCallback("Sending data from Bluetooth encountered error!" + ex.Message);
                }
            }
        }

        public void ReceiveData()
        {
            if (_streamSocket == null)
            {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            receivingThread = ThreadPool.RunAsync(async (s) =>
                 {
                     while (!cancelReceiving)
                     {
                         DataReader dreader = new DataReader(_streamSocket.InputStream);
                         uint sizeFieldCount = await dreader.LoadAsync(sizeof(uint)).AsTask(cancellationTokenSource.Token);

                         if (sizeFieldCount != sizeof(uint))
                         {
                             if (sizeFieldCount == 0)
                             {
                                 if (ObexConnectionStatusChanged != null) {
                                     ObexConnectionStatusChanged(false,null);
                                 }
                                 break;
                             }
                             continue;
                         }

                         uint stringLength;
                         uint actualStringLength;

                         try
                         {
                             stringLength = dreader.ReadUInt32();
                             actualStringLength = await dreader.LoadAsync(stringLength).AsTask(cancellationTokenSource.Token);

                             if (stringLength != actualStringLength)
                             {
                                  continue;
                             }
                             string text = dreader.ReadString(actualStringLength);

                             if (ObexMessage != null)
                             {
                                 ObexMessage(text);
                             }

                         }
                         catch (Exception ex)
                         {
                             if (ObexErrorCallback != null)
                             {
                                 ObexErrorCallback("Reading data from Bluetooth encountered error!" + ex.Message);
                             }
                         }
                     }
                 }, WorkItemPriority.High);
        }

        private void Listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            StreamSocket socket = args.Socket;
           
            _streamSocket = socket;

            if (ObexConnectionStatusChanged != null)
            {
                ObexConnectionStatusChanged(true, null);
            }                       
            ReceiveData();
        }

        public async Task<String> GetNameAttribute(RfcommDeviceService service)
        {   
            try {

                var attributes = await service.GetSdpRawAttributesAsync(BluetoothCacheMode.Uncached);
                if (!attributes.ContainsKey(SERVICE_NAME_ATTRIBUTE_ID))
                {
                    System.Diagnostics.Debug.WriteLine("Name attribute not found");
                    return "";
                }

                var attributeReader = DataReader.FromBuffer(attributes[SERVICE_NAME_ATTRIBUTE_ID]);
                var attributeType = attributeReader.ReadByte();
                if (attributeType != SERVICE_NAME_ATTRIBUTE_TYPE)
                {
                    System.Diagnostics.Debug.WriteLine("Name attribute type not right :" + attributeType + " , expecting : " + SERVICE_NAME_ATTRIBUTE_TYPE);
                    return "";
                }

                var nameLength = attributeReader.ReadByte();

                // The Service Name attribute requires UTF-8 encoding. 
                attributeReader.UnicodeEncoding = UnicodeEncoding.Utf8;
                return attributeReader.ReadString(nameLength);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetNameAttribute - Exception " + ex.Message); 
            }

            return "";
        }

        private void InitializeServiceSdpAttributes(RfcommServiceProvider provider)
        {
            DataWriter writer = new DataWriter();
                                                                                                                                                                                                                                                                                                     
            // First write the attribute type
            writer.WriteByte(SERVICE_NAME_ATTRIBUTE_TYPE);
            // Then write the data

            // The length of the UTF-8 encoded string.
            writer.WriteByte((byte)SERVICE_NAME.Length);
            // The UTF-8 encoded string.
            writer.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
            writer.WriteString(SERVICE_NAME);

            provider.SdpRawAttributes.Add(SERVICE_NAME_ATTRIBUTE_ID, writer.DetachBuffer());
        }
    }
}
