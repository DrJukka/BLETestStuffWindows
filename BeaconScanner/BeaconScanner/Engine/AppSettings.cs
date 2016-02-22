using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeaconScanner.Engine
{
    class AppSettings
    {
        private static double defaultLifecycle = 100;

        public static UInt16 ManufacturerCode
        {
            get
            {
                Object tmpVal = Windows.Storage.ApplicationData.Current.LocalSettings.Values["manufacturerCode"];
                if (tmpVal == null)
                {
                    return 0;
                }

                return (ushort)tmpVal;
            }
            set
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["manufacturerCode"] = value;
            }
        }

        public static UInt16 BeaconCode
        {
            get
            {
                Object tmpVal = Windows.Storage.ApplicationData.Current.LocalSettings.Values["beaconCode"];
                if (tmpVal == null)
                {
                    return 0;
                }

                return (ushort)tmpVal;
            }
            set
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["beaconCode"] = value;
            }
        }

        public static double Lifecycle
        {
            get
            {
                Object tmpVal = Windows.Storage.ApplicationData.Current.LocalSettings.Values["lifecycle"];
                if (tmpVal == null)
                {
                    return defaultLifecycle;
                }

                return (double)tmpVal;
            }
            set
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["lifecycle"] = value;
            }
        }
    }
}
