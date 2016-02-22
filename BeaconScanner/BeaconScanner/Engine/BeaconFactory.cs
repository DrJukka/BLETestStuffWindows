using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;


namespace BeaconScanner.Engine
{
    public class BeaconFactory
    {
        private const char HexStringSeparator = '-';
        private const byte FirstBeaconDataSectionDataType = 0x01;
        private const byte SecondBeaconDataSectionDataType = 0xFF;
        private const int SecondBeaconDataSectionMinimumLengthInBytes = 25;

        /// <summary>
        /// Constructs a Beacon instance and sets the properties based on the given data.
        /// </summary>
        /// <param name="args"></param>
        /// <returns>A newly created Beacon instance or null in case of a failure.</returns>
        public static Beacon BeaconFromBluetoothLEAdvertisementReceivedEventArgs(BluetoothLEAdvertisementReceivedEventArgs args)
        {
            Beacon beacon = null;

            if (args != null && args.Advertisement != null)
            {
                beacon = BeaconFromDataSectionList(args.Advertisement.DataSections);

                if (beacon != null)
                {
                    beacon.Timestamp = args.Timestamp;
                    beacon.RawSignalStrengthInDBm = args.RawSignalStrengthInDBm;
                    beacon.BluetoothAddress = args.BluetoothAddress;
                    beacon.DiscoveryTestData = new bool[100];
                }
            }

            return beacon;
        }

        /// <summary>
        /// Constructs a Beacon instance and sets the properties based on the given data.
        /// </summary>
        /// <param name="dataSection">A data section containing beacon data.</param>
        /// <returns>A newly created Beacon instance or null in case of a failure.</returns>
        public static Beacon BeaconFromDataSectionList(IList<BluetoothLEAdvertisementDataSection> dataSections)
        {
            Beacon beacon = null;

            if (dataSections != null && dataSections.Count > 0)
            {
                foreach (BluetoothLEAdvertisementDataSection dataSection in dataSections)
                {
                    if (dataSection != null)
                    {
                        if (dataSection.DataType == SecondBeaconDataSectionDataType)
                        {
                            beacon = BeaconFromDataSection(dataSection);
                        }
                    }
                }
            }

            return beacon;
        }

        /// <summary>
        /// Constructs a Beacon instance and sets the properties based on the given data.
        /// </summary>
        /// <param name="dataSection">A data section containing beacon data.</param>
        /// <returns>A newly created Beacon instance or null in case of a failure.</returns>
        public static Beacon BeaconFromDataSection(BluetoothLEAdvertisementDataSection dataSection)
        {
            Beacon beacon = null;

            if (dataSection != null && dataSection.Data != null)
            {
                beacon = BeaconFromByteArray(dataSection.Data.ToArray());
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("BeaconFactory.BeaconFromDataSection(): The given data (section) is null");
            }

            return beacon;
        }

        /// <summary>
        /// Constructs a Beacon instance and sets the properties based on the given data.
        /// 
        /// The expected specification of the data is as follows:
        /// 
        /// Byte(s)     Name
        /// --------------------------
        /// 0-1         Manufacturer ID (16-bit unsigned integer, big endian)
        /// 2-3         Beacon code (two 8-bit unsigned integers, but can be considered as one 16-bit unsigned integer in little endian)
        /// 4-19        ID1 (UUID)
        /// 20-21       ID2 (16-bit unsigned integer, big endian)
        /// 22-23       ID3 (16-bit unsigned integer, big endian)
        /// 24          Measured Power (signed 8-bit integer)
        /// 25          Reserved for use by the manufacturer to implement special features (optional)
        /// 
        /// For more details on the beacon specifications see https://github.com/AltBeacon/spec
        /// 
        /// The minimum length of the given byte array is 25. If it is longer than 26 bits,
        /// everything after the 26th bit is ignored.
        /// </summary>
        /// <param name="data">The data to populate the Beacon instance properties with.</param>
        /// <returns>A newly created Beacon instance or null in case of a failure.</returns>
        public static Beacon BeaconFromByteArray([ReadOnlyArray] byte[] data)
        {
            if (data == null || data.Length < SecondBeaconDataSectionMinimumLengthInBytes)
            {
                // The given data is null or too short
                return null;
            }

            Beacon beacon = new Beacon();
            beacon.Code = BitConverter.ToUInt16(data, 2); // Bytes 2-3
            beacon.Id1 = FormatUuid(BitConverter.ToString(data, 4, 16)); // Bytes 4-19
            beacon.MeasuredPower = Convert.ToSByte(BitConverter.ToString(data, 24, 1), 16); // Byte 24

            if (data.Length >= SecondBeaconDataSectionMinimumLengthInBytes + 1)
            {
                beacon.MfgReserved = data[25]; // Byte 25
            }

            // Data is expected to be big endian. Thus, if we are running on a little endian,
            // we need to switch the bytes
            if (BitConverter.IsLittleEndian)
            {
                data = ChangeInt16ArrayEndianess(data);
            }

            beacon.ManufacturerId = BitConverter.ToUInt16(data, 0); // Bytes 0-1
            beacon.Id2 = BitConverter.ToUInt16(data, 20); // Bytes 20-21
            beacon.Id3 = BitConverter.ToUInt16(data, 22); // Bytes 22-23

            return beacon;
        }

        public static Beacon DublicateBeacon(Beacon sourceBeacon)
        {
            if (sourceBeacon == null )
            {
                return null;
            }

            Beacon beacon = new Beacon();
            beacon.Code = sourceBeacon.Code;
            beacon.Id1 = sourceBeacon.Id1;
            beacon.MeasuredPower = sourceBeacon.MeasuredPower;
            beacon.MfgReserved = sourceBeacon.MfgReserved;
            beacon.ManufacturerId = sourceBeacon.ManufacturerId;
            beacon.Id2 = sourceBeacon.Id2;
            beacon.Id3 = sourceBeacon.Id3;
            beacon.ListeningCycle = sourceBeacon.ListeningCycle;
            beacon.BluetoothAddress = sourceBeacon.BluetoothAddress;

            beacon.DiscoveryTestData = new bool[sourceBeacon.DiscoveryTestData.Count()];
            for(int i = 0; i < sourceBeacon.DiscoveryTestData.Count(); i++)
            {
                beacon.DiscoveryTestData[i] = sourceBeacon.DiscoveryTestData[i];
            }
            
            return beacon;
        }

        /// <summary>
        /// Creates a BluetoothLEManufacturerData instance based on the given manufacturer ID and
        /// beacon code. The returned instance can be used as a filter for a BLE advertisement
        /// watcher.
        /// </summary>
        /// <param name="manufacturerId">The manufacturer ID.</param>
        /// <param name="beaconCode">The beacon code.</param>
        /// <returns>BluetoothLEManufacturerData instance based on given arguments.</returns>
        public static BluetoothLEManufacturerData BeaconManufacturerData(UInt16 manufacturerId, UInt16 beaconCode)
        {
            BluetoothLEManufacturerData manufacturerData = new BluetoothLEManufacturerData();
            manufacturerData.CompanyId = manufacturerId;
            DataWriter writer = new DataWriter();
            writer.WriteUInt16(beaconCode);
            manufacturerData.Data = writer.DetachBuffer();
            return manufacturerData;
        }

        /// <summary>
        /// Creates the second part of the beacon advertizing packet.
        /// Uses the beacon IDs 1, 2, 3 and measured power to create the data section.
        /// </summary>
        /// <param name="beacon">A beacon instance.</param>
        /// <param name="includeMfgReservedByte">Defines whether we should add the additional, manufacturer reserved byte or not.</param>
        /// <returns>A newly created data section.</returns>
        public static BluetoothLEAdvertisementDataSection BeaconToSecondDataSection(
            Beacon beacon, bool includeMfgReservedByte = false)
        {
            string[] temp = beacon.Id1.Split(HexStringSeparator);
            string beaconId1 = string.Join("", temp);

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(ManufacturerIdToString(beacon.ManufacturerId));
            stringBuilder.Append(BeaconCodeToString(beacon.Code));
            stringBuilder.Append(beaconId1.ToUpper());

            byte[] beginning = HexStringToByteArray(stringBuilder.ToString());

            byte[] data = includeMfgReservedByte
                ? new byte[SecondBeaconDataSectionMinimumLengthInBytes + 1]
                : new byte[SecondBeaconDataSectionMinimumLengthInBytes];

            beginning.CopyTo(data, 0);
            ChangeInt16ArrayEndianess(BitConverter.GetBytes(beacon.Id2)).CopyTo(data, 20);
            ChangeInt16ArrayEndianess(BitConverter.GetBytes(beacon.Id3)).CopyTo(data, 22);
            data[24] = (byte)Convert.ToSByte(beacon.MeasuredPower);

            if (includeMfgReservedByte)
            {
                data[25] = beacon.MfgReserved;
            }

            BluetoothLEAdvertisementDataSection dataSection = new BluetoothLEAdvertisementDataSection();
            dataSection.DataType = SecondBeaconDataSectionDataType;
            dataSection.Data = data.AsBuffer();

            return dataSection;
        }

        /// <summary>
        /// Converts the given manufacturer ID to string.
        /// </summary>
        /// <param name="manufacturerId">The manufacturer ID as Uint16.</param>
        /// <returns>The manufacturer ID as string.</returns>        
        public static string ManufacturerIdToString(UInt16 manufacturerId)
        {
            byte[] manufacturerIdAsByteArray = BitConverter.GetBytes(manufacturerId);
            string manufacturerIdAsString = BitConverter.ToString(manufacturerIdAsByteArray);
            return manufacturerIdAsString.Replace(HexStringSeparator.ToString(), string.Empty);
        }

        /// <summary>
        /// Converts the given beacon code to string.
        /// </summary>
        /// <param name="beaconCode">The beacon code as Uint16.</param>
        /// <returns>The beacon code as string.</returns>        
        public static string BeaconCodeToString(UInt16 beaconCode)
        {
            byte[] beaconCodeAsByteArray = ChangeInt16ArrayEndianess(BitConverter.GetBytes(beaconCode));
            string beaconCodeAsString = BitConverter.ToString(beaconCodeAsByteArray);
            return beaconCodeAsString.Replace(HexStringSeparator.ToString(), string.Empty);
        }

        /// <summary>
        /// Calculates the beacon distance based on the given values.
        /// </summary>
        /// <param name="rawSignalStrengthInDBm">The detected signal strength.</param>
        /// <param name="measuredPower">The device specific measured power as reported by the beacon.</param>
        /// <returns>The distance to the beacon in meters.</returns>
        public static double CalculateDistanceFromRssi(double rawSignalStrengthInDBm, int measuredPower)
        {
            double distance = 0d;
            double near = rawSignalStrengthInDBm / measuredPower;

            if (near < 1.0f)
            {
                distance = Math.Pow(near, 10);
            }
            else
            {
                distance = ((0.89976f) * Math.Pow(near, 7.7095f) + 0.111f);
            }

            return distance;
        }

        /// <summary>
        /// Formats the given UUID. The method also accepts strings, which do
        /// not have the full UUID (are shorter than expected). Too long
        /// strings are truncated.
        /// 
        /// An example of a formatted UUID: de305d54-75b4-431b-adb2-eb6b9e546014
        /// </summary>
        /// <param name="uuid">A UUID to format.</param>
        /// <returns>The formatted UUID.</returns>
        public static string FormatUuid(string uuid)
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (uuid.Length > 0 && uuid.Trim().Length > 0)
            {
                uuid = uuid.Trim();
                uuid = uuid.Replace(HexStringSeparator.ToString(), string.Empty);

                if (uuid.Length > 8)
                {
                    stringBuilder.Append(uuid.Substring(0, 8));
                    stringBuilder.Append(HexStringSeparator);

                    if (uuid.Length > 12)
                    {
                        stringBuilder.Append(uuid.Substring(8, 4));
                        stringBuilder.Append(HexStringSeparator);

                        if (uuid.Length > 16)
                        {
                            stringBuilder.Append(uuid.Substring(12, 4));
                            stringBuilder.Append(HexStringSeparator);

                            if (uuid.Length > 20)
                            {
                                stringBuilder.Append(uuid.Substring(16, 4));
                                stringBuilder.Append(HexStringSeparator);

                                if (uuid.Length > 32)
                                {
                                    stringBuilder.Append(uuid.Substring(20, 12));
                                }
                                else
                                {
                                    stringBuilder.Append(uuid.Substring(20));
                                }
                            }
                            else
                            {
                                stringBuilder.Append(uuid.Substring(16));
                            }
                        }
                        else
                        {
                            stringBuilder.Append(uuid.Substring(12));
                        }
                    }
                    else
                    {
                        stringBuilder.Append(uuid.Substring(8));
                    }
                }
                else
                {
                    stringBuilder.Append(uuid);
                }
            }

            return stringBuilder.ToString().ToLower();
        }

        /// <summary>
        /// Converts the given hex string to byte array.
        /// </summary>
        /// <param name="hexString">The hex string to convert.</param>
        /// <returns>The given hex string as a byte array.</returns>
        private static byte[] HexStringToByteArray(string hexString)
        {
            return Enumerable.Range(0, hexString.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hexString.Substring(x, 2), 16))
                .ToArray();
        }

        /// <summary>
        /// Switches the endianess of the given byte array so that every two bytes
        /// are switched.
        /// </summary>
        /// <param name="byteArray">A byte array, whose endianess needs to be changed.</param>
        /// <returns>The modified byte array.</returns>
        private static byte[] ChangeInt16ArrayEndianess(byte[] byteArray)
        {
            byte[] convertedArray = new byte[byteArray.Length];

            for (int i = 0; i < byteArray.Length; i += 2)
            {
                if (i + 1 < byteArray.Length)
                {
                    convertedArray[i] = byteArray[i + 1];
                    convertedArray[i + 1] = byteArray[i];
                }
                else
                {
                    convertedArray[i] = byteArray[i];
                }
            }

            return convertedArray;
        }
    }
}

