using Common;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth.Background;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI.Notifications;

namespace HeartbeatMonitor
{
    public sealed class HeartbeatMonitorBackgroundTask : IBackgroundTask
    {
        // Heart Rate profile defined flag values
        const byte HEART_RATE_VALUE_FORMAT = 0x01;
        private IBackgroundTaskInstance _taskInstance = null;
        private BackgroundTaskDeferral _deferral;
    
        private enum alertType{
            noAlert,
            minLevelAlert,
            maxLevelAlert
        }

        public HeartbeatMonitorBackgroundTask()
        {
            System.Diagnostics.Debug.WriteLine("HeartbeatMonitorBackgroundTask constructed");
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            System.Diagnostics.Debug.WriteLine("WE are @ Background");
            _deferral = taskInstance.GetDeferral();
            _taskInstance = taskInstance;
            _taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);

            GattCharacteristicNotificationTriggerDetails details = (GattCharacteristicNotificationTriggerDetails)taskInstance.TriggerDetails;

            //get the characteristics data and get heartbeat value out from it
            byte[] ReceivedData = new byte[details.Value.Length];
            DataReader.FromBuffer(details.Value).ReadBytes(ReceivedData);

            HeartbeatMeasurement tmpMeasurement = HeartbeatMeasurement.GetHeartbeatMeasurementFromData(ReceivedData);

            System.Diagnostics.Debug.WriteLine("Background heartbeast values: " + tmpMeasurement.HeartbeatValue);

            // send heartbeat values via progress callback
            _taskInstance.Progress = tmpMeasurement.HeartbeatValue;

            //update the value to the Tile
            SendTileTextNotification("" + tmpMeasurement.HeartbeatValue, "Bpm");

            //Check if we are within the limits, and alert by starting the app if we are not
            alertType alert = await checkHeartbeatLevels(tmpMeasurement.HeartbeatValue);

            _deferral.Complete();
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            SendTileTextNotification("XXX", "Bpm");
            System.Diagnostics.Debug.WriteLine("Background " + sender.Task.Name + " Cancel Requested because " + reason);
         //   if (!reason.Equals(BackgroundTaskCancellationReason.Abort))
        //    {
                SendToastNotification("Background Task Cancelled, reason: " + reason + ", please re-start the applications.", null);
         //   }
        }

        private async Task<alertType> checkHeartbeatLevels(ushort currentValue)
        {
            if (currentValue < AppSettings.MinHeartbeatValue)
            {
                if (!AppSettings.AppLaunchedForMinAlert)
                {
                    System.Diagnostics.Debug.WriteLine("_minAlertResentCounter : " + AppSettings.MinAlertCounter);
                    if (AppSettings.MinAlertCounter <= 0)
                    {
                        AppSettings.MinAlertCounter = 10;
                        System.Diagnostics.Debug.WriteLine("launchAppViaURI now");
                        bool success = await launchAppViaURI("MinHeartBeatAlert?value=" + currentValue, "Heartbeat value under the specified limit, current value " + currentValue);

                        AppSettings.AppLaunchedForMinAlert = true;
                        return alertType.minLevelAlert;
                    }

                    AppSettings.MinAlertCounter--;
                }
            }
            else
            {
                AppSettings.AppLaunchedForMinAlert = false;
            }

            
            if (currentValue > AppSettings.MaxHeartbeatValue)
            {
                if (!AppSettings.AppLaunchedForMaxAlert)
                {
                    System.Diagnostics.Debug.WriteLine("_maxAlertResentCounter : " + AppSettings.MaxAlertCounter);
                    if (AppSettings.MaxAlertCounter <= 0)
                    {
                        AppSettings.MaxAlertCounter = 10;
                        System.Diagnostics.Debug.WriteLine("launchAppViaURI now");
                        bool success = await launchAppViaURI("MaxHeartBeatAlert?value=" + currentValue, "Heartbeat value over the specified limit, current value " + currentValue);

                        AppSettings.AppLaunchedForMaxAlert = true;
                        return alertType.maxLevelAlert;
                    }

                    AppSettings.MaxAlertCounter--;
                }
            }
            else
            {
                AppSettings.AppLaunchedForMaxAlert = false;
            }

            return alertType.noAlert;
        }

        public void SendToastNotification(string message, string imageName)
        {
            var notificationXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText01);
            var toastElements = notificationXml.GetElementsByTagName("text");
            toastElements[0].AppendChild(notificationXml.CreateTextNode(message));
            if (string.IsNullOrEmpty(imageName))
            {
                imageName = @"Assets/StoreLogo.png";
            }
            var imageElement = notificationXml.GetElementsByTagName("image");
            imageElement[0].Attributes[1].NodeValue = imageName;
            var toastNotification = new ToastNotification(notificationXml);
            ToastNotificationManager.CreateToastNotifier().Show(toastNotification);
        }

        private static void SendTileTextNotification(string number, string text)
        {
            var tileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare150x150Block);
            var tileAttributes = tileXml.GetElementsByTagName("text");
            tileAttributes[0].AppendChild(tileXml.CreateTextNode(number));
            tileAttributes[1].AppendChild(tileXml.CreateTextNode(text));

            var tileNotification = new TileNotification(tileXml);
            TileUpdateManager.CreateTileUpdaterForApplication().Update(tileNotification);
        }

        private async Task<System.Boolean> launchAppViaURI(string message, string toastMessage)
        {
            // The URI to launch
            var uriMonitor = new Uri(@"heartbeat-alert://" + Uri.EscapeUriString(message));
            // Launch the URI
            bool success = await Windows.System.Launcher.LaunchUriAsync(uriMonitor);
            if (!success)
            {
                System.Diagnostics.Debug.WriteLine("SendToastNotification now");
                SendToastNotification(toastMessage, null);
            }

            return success;
        }
    }
}
