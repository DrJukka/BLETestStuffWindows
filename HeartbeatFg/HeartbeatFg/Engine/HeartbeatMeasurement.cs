using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeartbeatFg.Engine
{
    public class HeartbeatMeasurement
    {
        public ushort HeartbeatValue { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public override string ToString()
        {
            return HeartbeatValue.ToString() + " bpm @ " + Timestamp.ToString();
        }

        public static HeartbeatMeasurement GetHeartbeatMeasurementFromData(byte[] data, DateTimeOffset timeStamp)
        {
            // Heart Rate profile defined flag values
            const byte HEART_RATE_VALUE_FORMAT = 0x01;

            byte currentOffset = 0;
            byte flags = data[currentOffset];
            bool isHeartbeatValueSizeLong = ((flags & HEART_RATE_VALUE_FORMAT) != 0);

            currentOffset++;

            ushort HeartbeatMeasurementValue = 0;

            if (isHeartbeatValueSizeLong)
            {
                HeartbeatMeasurementValue = (ushort)((data[currentOffset + 1] << 8) + data[currentOffset]);
                currentOffset += 2;
            }
            else
            {
                HeartbeatMeasurementValue = data[currentOffset];
                currentOffset++;
            }

            DateTimeOffset tmpVal = timeStamp;
            if(tmpVal == null)
            {
                tmpVal = DateTimeOffset.Now;
            }
            return new HeartbeatMeasurement
            {
                HeartbeatValue = HeartbeatMeasurementValue,
                Timestamp = tmpVal
            };
        }
    }
}
