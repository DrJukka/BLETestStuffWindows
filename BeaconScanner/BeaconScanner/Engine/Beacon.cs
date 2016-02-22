using System;
using System.ComponentModel;
using System.Threading;

namespace BeaconScanner.Engine
{
    public class Beacon : INotifyPropertyChanged, IDisposable
    {
        private const int BeaconNotInRangeTimeoutInSeconds = 2;
        private const int LastSeenTimerTimeoutInMilliseconds = 1000;
        public event PropertyChangedEventHandler PropertyChanged;
        private Timer _lastSeenTimer;
        private object _lockerObject;

        private bool[] _discoveryTestData  = new bool[100];
        public bool[] DiscoveryTestData
        {
            get { return _discoveryTestData; }
            set
            {
                _discoveryTestData = value;
                NotifyPropertyChanged("DiscoveryTestData");
            }
        }

        public Double ListeningCycle
        {
            get;
            set;
        }

        public UInt64 BluetoothAddress
        {
            get;
            set;
        }

        public UInt16 ManufacturerId
        {
            get;
            set;
        }

        public UInt16 Code
        {
            get;
            set;
        }

        private string _id1;
        public string Id1
        {
            get
            {
                return _id1;
            }
            set
            {
                if (string.IsNullOrEmpty(_id1) || !_id1.Equals(value))
                {
                    _id1 = value;
                    NotifyPropertyChanged("Id1");
                }
            }
        }

        private UInt16 _id2;
        public UInt16 Id2
        {
            get
            {
                return _id2;
            }
            set
            {
                if (_id2 != value)
                {
                    _id2 = value;
                    NotifyPropertyChanged("Id2");
                }
            }
        }

        private UInt16 _id3;
        public UInt16 Id3
        {
            get
            {
                return _id3;
            }
            set
            {
                if (_id3 != value)
                {
                    _id3 = value;
                    NotifyPropertyChanged("Id3");
                }
            }
        }

        private int _rawSignalStrengthInDBm;
        public int RawSignalStrengthInDBm
        {
            get
            {
                return _rawSignalStrengthInDBm;
            }
            set
            {
                if (_rawSignalStrengthInDBm != value)
                {
                    _rawSignalStrengthInDBm = value;
                    NotifyPropertyChanged("RawSignalStrengthInDBm");

                    CalculateDistance(_rawSignalStrengthInDBm, MeasuredPower);
                }
            }
        }

        private int _measuredPower;
        public int MeasuredPower
        {
            get
            {
                return _measuredPower;
            }
            set
            {
                if (_measuredPower != value)
                {
                    _measuredPower = value;
                    NotifyPropertyChanged("MeasuredPower");

                    CalculateDistance(RawSignalStrengthInDBm, _measuredPower);
                }
            }
        }

        /// <summary>
        /// Reserved for use by the manufacturer to implement special features.
        /// </summary>
        public byte MfgReserved
        {
            get;
            set;
        }

        private DateTimeOffset _timestamp;
        public DateTimeOffset Timestamp
        {
            get
            {
                return _timestamp;
            }
            set
            {
                _timestamp = value;
                NotifyPropertyChanged("Timestamp");

                TimeSpan timeElapsedSinceLastSeen = DateTime.Now - _timestamp;
                SecondsElapsedSinceLastSeen = (int)timeElapsedSinceLastSeen.TotalSeconds;
            }
        }

        private double _distance;
        public double Distance
        {
            get
            {
                return _distance;
            }
            private set
            {
                if (_distance != value)
                {
                    _distance = value;
                    NotifyPropertyChanged("Distance");

                    UpdateRange();
                }
            }
        }

        private int _range;
        public int Range
        {
            get
            {
                return _range;
            }
            set
            {
                if (_range != value)
                {
                    _range = value;
                    NotifyPropertyChanged("Range");
                }
            }
        }

        private int _secondsElapsedSinceLastSeen;
        public int SecondsElapsedSinceLastSeen
        {
            get
            {
                return _secondsElapsedSinceLastSeen;
            }
            private set
            {
                if (_secondsElapsedSinceLastSeen != value)
                {
                    _secondsElapsedSinceLastSeen = value;
                    NotifyPropertyChanged("SecondsElapsedSinceLastSeen");
                }
            }
        }

        public Beacon()
        {
            _lockerObject = new Object();
            _lastSeenTimer = new Timer(LastSeenTimerCallbackAsync, null,
                LastSeenTimerTimeoutInMilliseconds, LastSeenTimerTimeoutInMilliseconds);
        }

        public void Dispose()
        {
            if (_lastSeenTimer != null)
            {
                _lastSeenTimer.Dispose();
                _lastSeenTimer = null;
            }
        }

        /// <summary>
        /// Updates the beacon data, if the given beacon matches this one.
        /// </summary>
        /// <param name="beacon">The beacon with new data.</param>
        /// <returns>True, if the given beacon matches this one (and the data was updated). False otherwise.</returns>
        public bool Update(Beacon beacon)
        {
            bool matches = Matches(beacon);

            if (matches)
            {
                Timestamp = beacon.Timestamp;
                RawSignalStrengthInDBm = beacon.RawSignalStrengthInDBm;

                if (_lastSeenTimer != null)
                {
                    _lastSeenTimer.Dispose();
                    _lastSeenTimer = new Timer(LastSeenTimerCallbackAsync, null,
                        LastSeenTimerTimeoutInMilliseconds, LastSeenTimerTimeoutInMilliseconds);
                }
            }

            return matches;
        }

        /// <summary>
        /// Compares the given beacon to this.
        /// </summary>
        /// <param name="beacon">The beacon to compare to.</param>
        /// <returns>True, if the beacons match.</returns>
        public bool Matches(Beacon beacon)
        {
            return beacon.Id1.Equals(Id1)
                && beacon.Id2 == Id2
                && beacon.Id3 == Id3;
        }

        public override string ToString()
        {
            return BeaconFactory.FormatUuid(Id1) + ":" + Id2 + ":" + Id3;
        }

        private void CalculateDistance(int rawSignalStrengthInDBm, int measuredPower)
        {
            if (rawSignalStrengthInDBm != 0 && measuredPower != 0)
            {
                Distance = Math.Round(BeaconFactory.CalculateDistanceFromRssi(rawSignalStrengthInDBm, measuredPower), 1);
            }
        }

        private void UpdateRange()
        {
            if (SecondsElapsedSinceLastSeen >= BeaconNotInRangeTimeoutInSeconds)
            {
                Range = 0;
            }
            else
            {
                if (Distance <= 2.0d)
                {
                    Range = 4;
                }
                else if (Distance <= 5.0d)
                {
                    Range = 3;
                }
                else if (Distance <= 10.0d)
                {
                    Range = 2;
                }
                else
                {
                    Range = 1;
                }
            }
        }

        private async void LastSeenTimerCallbackAsync(object state)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    lock (_lockerObject)
                    {
                        TimeSpan timeElapsedSinceLastSeen = DateTime.Now - Timestamp;
                        SecondsElapsedSinceLastSeen = (int)timeElapsedSinceLastSeen.TotalSeconds;
                    }
                });
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
