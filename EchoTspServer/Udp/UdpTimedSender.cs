using System.Net;
using System.Net.Sockets;
using EchoTspServer.Interfaces;

namespace EchoTspServer.Udp
{
    /// <summary>
    /// Sends UDP messages at timed intervals
    /// </summary>
    public class UdpTimedSender : IUdpSender
    {
        private readonly string _host;
        private readonly int _port;
        private readonly UdpClient _udpClient;
        private readonly UdpMessageBuilder _messageBuilder;
        private Timer? _timer;

        public UdpTimedSender(string host, int port, UdpMessageBuilder messageBuilder)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _port = port;
            _messageBuilder = messageBuilder ?? throw new ArgumentNullException(nameof(messageBuilder));
            _udpClient = new UdpClient();
        }

        public UdpTimedSender(string host, int port)
            : this(host, port, new UdpMessageBuilder())
        {
        }

        public void StartSending(int intervalMilliseconds)
        {
            if (_timer != null)
                throw new InvalidOperationException("Sender is already running.");

            _timer = new Timer(SendMessageCallback, null, 0, intervalMilliseconds);
        }

        private void SendMessageCallback(object? state)
        {
            try
            {
                byte[] msg = _messageBuilder.BuildMessage();
                var endpoint = new IPEndPoint(IPAddress.Parse(_host), _port);

                _udpClient.Send(msg, msg.Length, endpoint);
                Console.WriteLine($"Message sent to {_host}:{_port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        }

        public void StopSending()
        {
            _timer?.Dispose();
            _timer = null;
        }

        public void Dispose()
        {
            StopSending();
            _udpClient.Dispose();
        }
    }
}