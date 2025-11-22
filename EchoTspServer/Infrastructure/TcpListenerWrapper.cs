using EchoTspServer.Interfaces;
using System.Net;
using System.Net.Sockets;

namespace EchoTspServer.Infrastructure
{
    /// <summary>
    /// Wrapper for TcpListener to enable dependency injection and testing
    /// </summary>
    public class TcpListenerWrapper : ITcpListenerWrapper
    {
        private readonly TcpListener _listener;

        public TcpListenerWrapper(IPAddress address, int port)
        {
            _listener = new TcpListener(address, port);
        }

        public void Start()
        {
            _listener.Start();
        }

        public void Stop()
        {
            _listener.Stop();
        }

        public Task<TcpClient> AcceptTcpClientAsync()
        {
            return _listener.AcceptTcpClientAsync();
        }
    }
}