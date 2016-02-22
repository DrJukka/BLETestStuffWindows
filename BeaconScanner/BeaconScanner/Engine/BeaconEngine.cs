using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;

namespace BeaconScanner.Engine
{
    public delegate void Stopped(BluetoothLEAdvertisementWatcherStatus status);
    public delegate void Discovered(BluetoothLEAdvertisementReceivedEventArgs args);

    public class BeaconEngine
    {
        private BluetoothLEAdvertisementWatcher _Watcher;

        private static BeaconEngine _instance = new BeaconEngine();

        public static BeaconEngine Instance
        {
            get { return _instance; }
        }

        public Stopped Stopped;
        public Discovered Discovered;

        public  String Start(UInt16 manufacturerId, UInt16 beaconCode)
        {
            try {
                Stop();
                _Watcher = new BluetoothLEAdvertisementWatcher();

                _Watcher.Stopped += _Watcher_Stopped;
                _Watcher.Received += _Watcher_Received;

                if (manufacturerId != 0 && beaconCode != 0){
                    BluetoothLEManufacturerData manufacturerData = BeaconFactory.BeaconManufacturerData(manufacturerId, beaconCode);
                    _Watcher.AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);
                }
                _Watcher.Start();
            }catch (Exception ex){
                return ex.Message;
            }

            return null;
        }
        public bool IsRunning()
        {
            return _Watcher != null ? (_Watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started) : false;
        }

        public void Stop()
        {
            if(_Watcher == null){
                return;
            }

            _Watcher.Stopped -= _Watcher_Stopped;
            _Watcher.Received -= _Watcher_Received;
            _Watcher.Stop();
            _Watcher = null;
        }

        private void _Watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            if (Discovered != null){
                Discovered(args);
            }
        }

        private void _Watcher_Stopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("Bluetooth LE Advertisement Watcher stopped with status: "+ _Watcher.Status);
            if(Stopped != null){
                Stopped(_Watcher.Status);
            }
        }
    }
}
