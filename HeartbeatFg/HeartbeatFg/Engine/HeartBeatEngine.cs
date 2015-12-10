
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration.Pnp;
using Windows.Storage.Streams;

namespace HeartbeatFg.Engine
{
    public delegate void ValueChangeCompletedHandler(HeartbeatMeasurement HeartbeatMeasurementValue);
    public delegate void DeviceConnectionUpdatedHandler(bool isConnected, string error);

    public class HeartBeatEngine
    {
        private GattDeviceService       _service = null;
        private GattCharacteristic      _characteristic = null;
        private DeviceViewModel         _selectedDevice = null;
        private static HeartBeatEngine  _instance = new HeartBeatEngine();
        
        public static HeartBeatEngine Instance
        {
            get { return _instance; }
        }


        public event ValueChangeCompletedHandler ValueChangeCompleted;
        public event DeviceConnectionUpdatedHandler DeviceConnectionUpdated;

        public void Deinitialize()
        {
            if (_characteristic != null)
            {
                _characteristic.ValueChanged -= Oncharacteristic_ValueChanged;
                _characteristic = null;
            }

            if (_service != null)
            {
                _service.Device.ConnectionStatusChanged -= OnConnectionStatusChanged;
                //_service.Dispose();// appears that we should not call this here!!
                _service = null;
            }
        }

        public async void InitializeServiceAsync(string deviceId)
        {
            try
            {
                Deinitialize();
                _service = await GattDeviceService.FromIdAsync(deviceId);

                if (_service != null)
                {
                    //we could be already connected, thus lets check that before we start monitoring for changes
                    if (DeviceConnectionUpdated != null && (_service.Device.ConnectionStatus == BluetoothConnectionStatus.Connected))
                    {
                        DeviceConnectionUpdated(true, null);
                    }

                    _service.Device.ConnectionStatusChanged += OnConnectionStatusChanged;

                    _characteristic = _service.GetCharacteristics(GattCharacteristicUuids.HeartRateMeasurement)[0];
                    _characteristic.ValueChanged += Oncharacteristic_ValueChanged;

                    var currentDescriptorValue = await _characteristic.ReadClientCharacteristicConfigurationDescriptorAsync();
                    if ((currentDescriptorValue.Status != GattCommunicationStatus.Success) ||
                    (currentDescriptorValue.ClientCharacteristicConfigurationDescriptor != GattClientCharacteristicConfigurationDescriptorValue.Notify))
                    {
                        // most likely we never get here, though if for any reason this value is not Notify, then we should really set it to be
                        await _characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: Accessing your device failed." + Environment.NewLine + e.Message);

                if(DeviceConnectionUpdated != null)
                {
                    DeviceConnectionUpdated(false, "Accessing device failed: " + e.Message);
                }
            }
        }

        private void Oncharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var data = new byte[args.CharacteristicValue.Length];

            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);

            HeartbeatMeasurement tmpMeasurement = HeartbeatMeasurement.GetHeartbeatMeasurementFromData(data, args.Timestamp);

            System.Diagnostics.Debug.WriteLine("Oncharacteristic_ValueChanged : " + tmpMeasurement.HeartbeatValue);

            if (ValueChangeCompleted != null)
            {
                ValueChangeCompleted(tmpMeasurement);
            }
        }

        private void OnConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            if (sender.ConnectionStatus == BluetoothConnectionStatus.Connected)
            {
                System.Diagnostics.Debug.WriteLine("Connected");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Disconnected");
            }

            if (DeviceConnectionUpdated != null)
            {
                DeviceConnectionUpdated(sender.ConnectionStatus == BluetoothConnectionStatus.Connected,null);
            }
        }
    }
}
