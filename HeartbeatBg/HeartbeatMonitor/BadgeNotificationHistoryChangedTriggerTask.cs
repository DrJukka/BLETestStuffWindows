using System;
using System.Collections.Generic;

using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;
using Common;

namespace HeartbeatMonitor
{
    public sealed class BadgeNotificationHistoryChangedTriggerTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            System.Diagnostics.Debug.WriteLine("BadgeNotificationHistoryChangedTriggerTask : run ");

            var details = taskInstance.TriggerDetails as ToastNotificationHistoryChangedTriggerDetail;
            if (details == null)
                return;

            //Update the badge by taking the counter and deleting one
            IReadOnlyList<ToastNotification> TNList = ToastNotificationManager.History.GetHistory();
            System.Diagnostics.Debug.WriteLine("BadgeNotificationHistoryChangedTriggerTask : Count : " + TNList.Count);
            BadgeHelper.UpdateBadge((uint)TNList.Count);
        }
    }
}
