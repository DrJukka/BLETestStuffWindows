using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth.Background;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.Networking.Sockets;
using Common;

namespace BluetoothWatcher
{
    public sealed class BluetoothWatcherBackgroundTask : IBackgroundTask
    { 
        private StreamSocket socket = null;
        private DataReader reader = null;
        private DataWriter writer = null;

        private BackgroundTaskDeferral deferral = null;
        private IBackgroundTaskInstance taskInstance = null;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("Bluetooth BG running now !!");

            // Get the deferral to prevent the task from closing prematurely
            deferral = taskInstance.GetDeferral();

            // Setup our onCanceled callback and progress
            this.taskInstance = taskInstance;
            this.taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);
            this.taskInstance.Progress = 0;

            try
            {
                RfcommConnectionTriggerDetails details = (RfcommConnectionTriggerDetails)taskInstance.TriggerDetails;
                if (details != null)
                {
                    socket = details.Socket;
                    writer = new DataWriter(socket.OutputStream);
                    reader = new DataReader(socket.InputStream);
                }
                else
                {
                    CommonData.TaskExitReason = "Trigger details returned null";
                    deferral.Complete();
                }

                var result = await ReceiveDataAsync();
            }
            catch (Exception ex)
            {
                reader = null;
                writer = null;
                socket = null;

                CommonData.TaskExitReason = "Exception occurred while initializing the connection, hr = " + ex.HResult.ToString("X");
                deferral.Complete();

                Debug.WriteLine("Exception occurred while initializing the connection, hr = " + ex.HResult.ToString("X"));
            }
        }

        private void OnCanceled(IBackgroundTaskInstance taskInstance, BackgroundTaskCancellationReason reason)
        {
            CommonData.TaskExitReason = reason.ToString(); 
            deferral.Complete();
        }

        private async Task<int> ReceiveDataAsync()
        {
            while (true)
            {
                Debug.WriteLine("start reading");

                uint readLength = await reader.LoadAsync(sizeof(uint));
                if (readLength < sizeof(uint))
                {
                    CommonData.TaskExitReason =  "incoming reader got disconnected";
                    deferral.Complete();
                }
                uint currentLength = reader.ReadUInt32();

                Debug.WriteLine("message lenght " + currentLength);

                readLength = await reader.LoadAsync(currentLength);
                if (readLength < currentLength)
                {
                    CommonData.TaskExitReason = "incoming reader got no data";
                    deferral.Complete();
                }

                string message = reader.ReadString(currentLength);
                Debug.WriteLine("Got message " + message);

                //Store the message & update progress to invoke the progress event handler
                CommonData.LastMessage = message;
                this.taskInstance.Progress = this.taskInstance.Progress + 1;

                //reply back with stored reply message
                UInt32 len = writer.MeasureString(CommonData.ReplyMessage);
                writer.WriteUInt32(len);
                writer.WriteString(CommonData.ReplyMessage);
                await writer.StoreAsync();
                await writer.FlushAsync();
            }
        }
    }
}

