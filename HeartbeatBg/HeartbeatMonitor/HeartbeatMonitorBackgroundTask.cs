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
            LiveTile.UpdateSecondaryTile("" + tmpMeasurement.HeartbeatValue);
            
            //Check if we are within the limits, and alert by starting the app if we are not
            alertType alert = await checkHeartbeatLevels(tmpMeasurement.HeartbeatValue);

            _deferral.Complete();
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            LiveTile.UpdateSecondaryTile("--");
            System.Diagnostics.Debug.WriteLine("Background " + sender.Task.Name + " Cancel Requested because " + reason);

            if (!reason.Equals(BackgroundTaskCancellationReason.Abort))
            {
                ToastHelper.PopToast("Background Cancelled", "Background Task Cancelled, reason: " + reason + ", please re-start the applications.");
            }
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

                        ToastHelper.PopAlarmLowLimit("Heartbeat value under the specified limit, current value " + currentValue);

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

                        ToastHelper.PopAlarmHighLimit("Heartbeat value over the specified limit, current value " + currentValue);

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
    }
}
