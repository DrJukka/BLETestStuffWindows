using System;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace HeartbeatBg
{
    public delegate void DeviceConnectionUpdatedHandler(bool isConnected, string error);

    class BLEDeviceEngine
    {
        private GattDeviceService _service = null;
        private GattCharacteristic _characteristic = null;
        private static BLEDeviceEngine _instance = new BLEDeviceEngine();

        public event DeviceConnectionUpdatedHandler DeviceConnectionUpdated;

        public GattCharacteristic Characteristic
        {
            get { return _characteristic; }
        }
        public static BLEDeviceEngine Instance
        {
            get { return _instance; }
        }

        public void Deinitialize()
        {
            _characteristic = null;

            if (_service != null)
            {
                _service.Device.ConnectionStatusChanged -= OnConnectionStatusChanged;
                _service = null;
            }
        }

        public async void InitializeServiceAsync(string deviceId, Guid characteristicUuid)
        {
            try
            {
                Deinitialize();

                _service = await GattDeviceService.FromIdAsync(deviceId);
                if (_service != null)
                {
                    _characteristic = _service.GetCharacteristics(characteristicUuid)[0];
                    if (DeviceConnectionUpdated != null && (_service.Device.ConnectionStatus == BluetoothConnectionStatus.Connected))
                    {
                        DeviceConnectionUpdated(true, null);
                    }
                    _service.Device.ConnectionStatusChanged += OnConnectionStatusChanged;
                }
                else if (DeviceConnectionUpdated != null)
                {
                    DeviceConnectionUpdated(false, "No services found from the selected device");
                }
            }
            catch (Exception e)
            {
                if (DeviceConnectionUpdated != null)
                {
                    DeviceConnectionUpdated(false, "Accessing device failed: " + e.Message);
                }
            }
        }

        private void OnConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            if (DeviceConnectionUpdated != null)
            {
                DeviceConnectionUpdated(sender.ConnectionStatus == BluetoothConnectionStatus.Connected, null);
            }
        }
    }
}
