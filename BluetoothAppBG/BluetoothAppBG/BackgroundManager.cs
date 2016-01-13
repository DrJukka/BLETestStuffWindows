

using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Storage.Streams;

namespace BluetoothAppBG
{
    class BackgroundManager
    {
        private static readonly string BackgroundTaskName = "BluetoothWatcherBackgroundTask";
        private static readonly string BackgroundTaskEntryPoint = "BluetoothWatcher.BluetoothWatcherBackgroundTask";

        private BackgroundManager()
        {
        }

        public static bool IsBackgroundTaskRegistered()
        {
            return (GetBackgroundTask(BackgroundTaskName) != null);
        }

        public static void UnregisterBackgroundTask()
        {
            foreach (var taskValue in BackgroundTaskRegistration.AllTasks)
            {
                if (taskValue.Value.Name.Equals(BackgroundTaskName))
                {
                    taskValue.Value.Unregister(true);
                }
            }
        }

        public static bool RegisterBackgroundTaskEventHandlers(BackgroundTaskCompletedEventHandler complete, BackgroundTaskProgressEventHandler progress)
        {
            System.Diagnostics.Debug.WriteLine("RegisterBackgroundTaskEventHandlers");

            IBackgroundTaskRegistration registeredTask = GetBackgroundTask(BackgroundTaskName);
            if (registeredTask != null)
            {
                if (complete != null)
                {
                    registeredTask.Completed += complete;
                }

                if (progress != null)
                {
                    registeredTask.Progress += progress;
                }

                // we had the background task running already, thus re-using it
                return true;
            }

            return false;
        }

        public static bool UnRegisterBackgroundTaskEventHandlers(BackgroundTaskCompletedEventHandler complete, BackgroundTaskProgressEventHandler progress)
        {
            System.Diagnostics.Debug.WriteLine("UnRegisterBackgroundTaskEventHandlers");

            IBackgroundTaskRegistration registeredTask = GetBackgroundTask(BackgroundTaskName);
            if (registeredTask != null)
            {
                if (complete != null)
                {
                    registeredTask.Completed -= complete;
                }

                if (progress != null)
                {
                    registeredTask.Progress -= progress;
                }

                return true;
            }

            // background task was not found
            return false;
        }

        public static async Task<string> RegisterBackgroundTask(Guid uuid, string serviceName, string serviceDescriptor)
        {
            System.Diagnostics.Debug.WriteLine("RegisterBackgroundTask");

            try
            {
                // Applications registering for background trigger must request for permission.
                BackgroundAccessStatus backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();

                BackgroundTaskBuilder backgroundTaskBuilder = new BackgroundTaskBuilder();
                backgroundTaskBuilder.Name = BackgroundTaskName;
                backgroundTaskBuilder.TaskEntryPoint = BackgroundTaskEntryPoint;

                RfcommConnectionTrigger trigger = new RfcommConnectionTrigger();
                trigger.InboundConnection.LocalServiceId = RfcommServiceId.FromUuid(uuid);

                // TODO:  helper function to create sdpRecordBlob
                trigger.InboundConnection.SdpRecord = getsdpRecordBlob(serviceName, serviceDescriptor);

                //backgroundTaskBuilder.SetTrigger(new SystemTrigger(SystemTriggerType.TimeZoneChange, false));
                backgroundTaskBuilder.SetTrigger(trigger);

                BackgroundTaskRegistration backgroundTaskRegistration = backgroundTaskBuilder.Register();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: Accessing your device failed." + Environment.NewLine + e.Message);
                return "ERROR: Accessing your device failed." + Environment.NewLine + e.Message;
            }

            return null;
        }

        private static IBackgroundTaskRegistration GetBackgroundTask(string taskName)
        {
            foreach (var taskValue in BackgroundTaskRegistration.AllTasks.Values)
            {
                if (taskValue.Name.Equals(taskName))
                {
                    return taskValue;
                }
            }

            return null;
        }

        private static IBuffer getsdpRecordBlob(string serviceName, string serviceDescriptor)
        {
            DataWriter helperWriter = new DataWriter();
            DataWriter NameWriter = new DataWriter();

            // The length of the UTF-8 encoded string.
            NameWriter.WriteByte((byte)serviceName.Length);
            // The UTF-8 encoded string.
            NameWriter.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
            NameWriter.WriteString(serviceName);

            // UINT16 (0x09) value = 0x0100 [ServiceName]
            helperWriter.WriteByte(0x09);
            helperWriter.WriteByte(0x01);
            helperWriter.WriteByte(0x00);

            IBuffer serviceNameBuf = NameWriter.DetachBuffer();
            helperWriter.WriteByte(0x25); //TextString(0x25)
            helperWriter.WriteByte((byte)serviceNameBuf.Length);
            helperWriter.WriteBuffer(serviceNameBuf);

            DataWriter DescriptorWriter = new DataWriter();
            // The length of the UTF-8 encoded string.
            DescriptorWriter.WriteByte((byte)serviceDescriptor.Length);
            // The UTF-8 encoded string.
            DescriptorWriter.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
            DescriptorWriter.WriteString(serviceDescriptor);

            // UINT16 (0x09) value = 0x0101 [ServiceDescription]
            helperWriter.WriteByte(0x09);
            helperWriter.WriteByte(0x01);
            helperWriter.WriteByte(0x01);

            IBuffer descriptorBuf = DescriptorWriter.DetachBuffer();
            helperWriter.WriteByte(0x25); //TextString(0x25)
            helperWriter.WriteByte((byte)descriptorBuf.Length);
            helperWriter.WriteBuffer(descriptorBuf);

            DataWriter SdpRecordWriter = new DataWriter();
            SdpRecordWriter.WriteByte(0x35);

            IBuffer dataBuf = helperWriter.DetachBuffer();
            SdpRecordWriter.WriteByte((byte)dataBuf.Length);
            SdpRecordWriter.WriteBuffer(dataBuf);

            return SdpRecordWriter.DetachBuffer();
        }
    }
}
