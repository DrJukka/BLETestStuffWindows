using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace BluetoothApp.Model
{
    public class DeviceViewModel
    {
        private DeviceInformation _item;
        public int Kind
        {
            get
            {
                return (int)_item.Kind;
            }
        }

        public string Name
        {
            get
            {
                return _item.Name;
            }
        }
        public string Id
        {
            get
            {
                return _item.Id;
            }
        }

        public DeviceViewModel(DeviceInformation item)
        {
            _item = item;
        }

        public string GetPropertyString(string property)
        {
            return _item.Properties[property].ToString();
        }
    }
}
