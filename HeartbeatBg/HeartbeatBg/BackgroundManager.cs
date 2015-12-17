using Common;
using System;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace HeartbeatBg
{
    class BackgroundManager
    {
        private static readonly string HeartbeatMonitorBackgroundTaskName       = "HeartbeatMonitorBackgroundTask";
        private static readonly string HeartbeatMonitorBackgroundTaskEntryPoint = "HeartbeatMonitor.HeartbeatMonitorBackgroundTask";

        private static readonly string BadgeUpdateBackgroundTaskName            = "BadgeNotificationHistoryChangedTriggerTask";
        private static readonly string BadgeUpdateBackgroundTaskEntryPoint      = "HeartbeatMonitor.BadgeNotificationHistoryChangedTriggerTask";

        private BackgroundManager()
        {
        }

        public static bool IsBackgroundTaskRegistered()
        {
            return (GetBackgroundTask(HeartbeatMonitorBackgroundTaskName) != null); 
        }

        /// <summary>
        /// Unregisters background task
        /// </summary>
        /// <returns></returns>
        public static void UnregisterBackgroundTask()
        {
            foreach (var taskValue in BackgroundTaskRegistration.AllTasks)
            {
                if (taskValue.Value.Name.Equals(HeartbeatMonitorBackgroundTaskName))
                {
                    taskValue.Value.Unregister(true);
                }

                if (taskValue.Value.Name.Equals(BadgeUpdateBackgroundTaskName))
                {
                    taskValue.Value.Unregister(true);
                }
            }
        }

        /// <summary>
        /// Registers background task
        /// </summary>
        /// <returns></returns>
        public static bool RegisterBackgroundTaskEventHandlers(BackgroundTaskCompletedEventHandler complete, BackgroundTaskProgressEventHandler progress)
        {
            System.Diagnostics.Debug.WriteLine("RegisterBackgroundTaskEventHandlers");

            IBackgroundTaskRegistration registeredTask = GetBackgroundTask(HeartbeatMonitorBackgroundTaskName);
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

            IBackgroundTaskRegistration registeredTask = GetBackgroundTask(HeartbeatMonitorBackgroundTaskName);
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


        public static string RegisterBackgroundTask(GattCharacteristic characteristic)
        {
            System.Diagnostics.Debug.WriteLine("RegisterBackgroundTask");

            try
            {
                BackgroundTaskBuilder backgroundTaskBuilder = new BackgroundTaskBuilder();
                backgroundTaskBuilder.Name = HeartbeatMonitorBackgroundTaskName;
                backgroundTaskBuilder.TaskEntryPoint = HeartbeatMonitorBackgroundTaskEntryPoint;

                //backgroundTaskBuilder.SetTrigger(new SystemTrigger(SystemTriggerType.TimeZoneChange, false));
                backgroundTaskBuilder.SetTrigger(new GattCharacteristicNotificationTrigger(characteristic));

                BackgroundTaskRegistration backgroundTaskRegistration = backgroundTaskBuilder.Register();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: Accessing your device failed." + Environment.NewLine + e.Message);
                return "ERROR: Accessing your device failed." + Environment.NewLine + e.Message;
            }

            try
            {
                BackgroundTaskBuilder badgeUpdateTaskBuilder = new BackgroundTaskBuilder();
                badgeUpdateTaskBuilder.Name = BadgeUpdateBackgroundTaskName;
                badgeUpdateTaskBuilder.TaskEntryPoint = BadgeUpdateBackgroundTaskEntryPoint;

                badgeUpdateTaskBuilder.SetTrigger(new ToastNotificationHistoryChangedTrigger());

                BackgroundTaskRegistration backgroundTaskRegistration = badgeUpdateTaskBuilder.Register();
                System.Diagnostics.Debug.WriteLine("BadgeUpdateBackgroundTaskName registred");
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("BadgeNotification background failed to register. " + Environment.NewLine + e.Message);
                return "BadgeNotification background failed to register" + Environment.NewLine + e.Message;
            }

            return null;
        }

        /// <summary>
        /// Checks if a background task with the given name is registered.
        /// </summary>
        /// <param name="taskName">The name of the background task.</param>
        /// <returns>True, if registered. False otherwise.</returns>
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
    }
}
