using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class AppSettings
    {
        private static ushort defaultMinValue = 90;
        private static ushort defaultMaxValue = 170;

        
        public static ushort MinHeartbeatValue
        {
            get
            {
                Object tmpVal = Windows.Storage.ApplicationData.Current.LocalSettings.Values["minHearthBeatValue"];
                if (tmpVal == null)
                {
                    return defaultMinValue;
                }

                return (ushort)tmpVal;
            }
            set
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["minHearthBeatValue"] = value;
            }
        }

        public static ushort MaxHeartbeatValue
        {
            get
            {
                Object tmpVal = Windows.Storage.ApplicationData.Current.LocalSettings.Values["maxHearthBeatValue"];
                if(tmpVal == null)
                {
                    return defaultMaxValue;
                }

                return (ushort)tmpVal;
            }
            set
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["maxHearthBeatValue"] = value;
            }
        }

        public static string SelectedDeviceName
        {
            get
            {
                Object tmpVal = Windows.Storage.ApplicationData.Current.LocalSettings.Values["selectedDeviceName"];
                if (tmpVal == null)
                {
                    return "";
                }

                return (string)tmpVal;
            }
            set
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["selectedDeviceName"] = value;
            }
        }

        public static bool AppLaunchedForMaxAlert
        {
            get
            {
                Object tmpVal = Windows.Storage.ApplicationData.Current.LocalSettings.Values["appLaunchedForMaxAlert"];
                if (tmpVal == null)
                {
                    return false;
                }

                return (bool)tmpVal;
            }
            set
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["appLaunchedForMaxAlert"] = value;
            }
        }

        public static int MaxAlertCounter
        {
            get
            {
                Object tmpVal = Windows.Storage.ApplicationData.Current.LocalSettings.Values["maxAlertCounter"];
                if (tmpVal == null)
                {
                    return 10;
                }

                return (int)tmpVal;
            }
            set
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["maxAlertCounter"] = value;
            }
        }

        public static bool AppLaunchedForMinAlert
        {
            get
            {
                Object tmpVal = Windows.Storage.ApplicationData.Current.LocalSettings.Values["appLaunchedForMinAlert"];
                if (tmpVal == null)
                {
                    return false;
                }

                return (bool)tmpVal;
            }
            set
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["appLaunchedForMinAlert"] = value;
            }
        }

        public static int MinAlertCounter
        {
            get
            {
                Object tmpVal = Windows.Storage.ApplicationData.Current.LocalSettings.Values["minAlertCounter"];
                if (tmpVal == null)
                {
                    return 10;
                }

                return (int)tmpVal;
            }
            set
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["minAlertCounter"] = value;
            }
        }
    }
}
