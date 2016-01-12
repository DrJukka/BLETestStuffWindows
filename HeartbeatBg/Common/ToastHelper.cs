using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Common
{
    public class ToastHelper
    {
        public static ToastNotification PopToast(string title, string content)
        {
            return PopToast(title, content, null, null);
        }

        public static ToastNotification PopToast(string title, string content, string tag, string group)
        {
            string xml = $@"<toast activationType='foreground'>
                                            <visual>
                                                <binding template='ToastGeneric'>
                                                </binding>
                                            </visual>
                                        </toast>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            var binding = doc.SelectSingleNode("//binding");

            var el = doc.CreateElement("text");
            el.InnerText = title;

            binding.AppendChild(el);

            el = doc.CreateElement("text");
            el.InnerText = content;
            binding.AppendChild(el);

            return PopCustomToast(doc, tag, group);
        }

        public static ToastNotification PopAlarmLowLimit(string alarmString)
        {
            return PopAlarmToast(alarmString, "ms-winsoundevent:Notification.Looping.Alarm3");
        }

        public static ToastNotification PopAlarmHighLimit(string alarmString)
        {
            return PopAlarmToast(alarmString, "ms-winsoundevent:Notification.Looping.Alarm10");
        }

        public static ToastNotification PopAlarmToast(string alarmString, string alertSound)
        {
            string xml = "<toast launch = 'app-defined-string'>"
                + "<visual>"
                + "<binding template='ToastGeneric'>"
                + "<text>" + LiveTile.appName + "</text>"
                + "<text>" + alarmString + "</text>"
                + "</binding>"
                + "</visual>"
                + "<actions>"
                + "<action activationType='system' arguments='dismiss' content=''/>"
                + "<action activationType='protocol' content='start app' arguments='heartbeat-alert://alertNow' />"
                + "</actions>"
                + "<audio src='" + alertSound + "' loop='true'/>"
                + "</toast>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            return PopCustomToast(doc, null, null);
        }

        public static ToastNotification PopCustomToast(XmlDocument doc, string tag, string group)
        {
            var toast = new ToastNotification(doc);

            if (tag != null)
                toast.Tag = tag;

            if (group != null)
                toast.Group = group;

            ToastNotificationManager.CreateToastNotifier().Show(toast);

            return toast;
        }
    }
}
