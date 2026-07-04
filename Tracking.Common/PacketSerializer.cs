using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace KinectBridge.Tracking
{
    public static class ArmTrackingPacketSerializer
    {
        public static string SerializeToJson(ArmTrackingPacket packet)
        {
            byte[] payload = SerializeToUtf8Bytes(packet);
            return Encoding.UTF8.GetString(payload);
        }

        public static byte[] SerializeToUtf8Bytes(ArmTrackingPacket packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ArmTrackingPacket));
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, packet);
                return stream.ToArray();
            }
        }

        public static ArmTrackingPacket DeserializeFromJson(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ArmTrackingPacket));
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return (ArmTrackingPacket)serializer.ReadObject(stream);
            }
        }
    }
}
