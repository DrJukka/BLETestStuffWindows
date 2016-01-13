using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{

    public class CommonData
    {
        private static Guid _btUuid = Guid.Parse("6f5b75e8-be27-4684-a776-3238826d1a91");
        private const string _serviceName = "BtSerieal background by drJukka";
        private const string _serviceDesc = "Service descriptor";

        private const string DEFAULTREPLY = "got it, thanks";

        public static Guid GUID
        {
            get { return _btUuid; }
        }

        public static string ServiceName
        {
            get { return _serviceName; }
        }

        public static string ServiceDescriptor
        {
            get { return _serviceDesc; }
        }

        public static string LastMessage
        {
            get
            {
                Object tmpVal = Windows.Storage.ApplicationData.Current.LocalSettings.Values["LastMessage"];
                if (tmpVal == null)
                {
                    return "";
                }

                return (string)tmpVal;
            }
            set
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["LastMessage"] = value;
            }
        }

        public static string ReplyMessage
        {
            get
            {
                Object tmpVal = Windows.Storage.ApplicationData.Current.LocalSettings.Values["ReplyMessage"];
                if (tmpVal == null)
                {
                    return DEFAULTREPLY;
                }

                return (string)tmpVal;
            }
            set
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["ReplyMessage"] = value;
            }
        }

        public static string TaskExitReason
        {
            get
            {
                Object tmpVal = Windows.Storage.ApplicationData.Current.LocalSettings.Values["TaskExitReason"];
                if (tmpVal == null)
                {
                    return "";
                }

                return (string)tmpVal;
            }
            set
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["TaskExitReason"] = value;
            }
        }


    }
}
