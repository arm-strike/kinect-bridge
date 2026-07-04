using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace KinectBridge
{
    internal sealed class UdpPacketSender : IDisposable
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);

        private readonly UdpClient _client;
        private readonly IPEndPoint _endpoint;

        public UdpPacketSender(string address, int port)
        {
            _client = new UdpClient();
            _endpoint = new IPEndPoint(IPAddress.Parse(address), port);
        }

        public string TargetDescription
        {
            get { return _endpoint.Address + ":" + _endpoint.Port; }
        }

        public void SendJson(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            byte[] payload = Utf8WithoutBom.GetBytes(json);
            _client.Send(payload, payload.Length, _endpoint);
        }

        public void Dispose()
        {
            _client.Close();
            _client.Dispose();
        }
    }
}
