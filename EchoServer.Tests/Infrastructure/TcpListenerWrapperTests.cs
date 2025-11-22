using EchoTspServer.Infrastructure;
using NUnit.Framework;
using System.Net;
using System.Net.Sockets;

namespace EchoTspServer.Tests.Infrastructure
{
    [TestFixture]
    public class TcpListenerWrapperTests
    {
        private TcpListenerWrapper _wrapper;
        private int _testPort;

        [SetUp]
        public void SetUp()
        {
            // Use dynamic port to avoid conflicts
            _testPort = GetAvailablePort();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                _wrapper?.Stop();
            }
            catch { }
        }

        [Test]
        public void Constructor_CreatesInstance()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
                _wrapper = new TcpListenerWrapper(IPAddress.Loopback, _testPort));
        }

        [Test]
        public void Start_StartsListener()
        {
            // Arrange
            _wrapper = new TcpListenerWrapper(IPAddress.Loopback, _testPort);

            // Act & Assert
            Assert.DoesNotThrow(() => _wrapper.Start());
        }

        [Test]
        public void Stop_StopsListener()
        {
            // Arrange
            _wrapper = new TcpListenerWrapper(IPAddress.Loopback, _testPort);
            _wrapper.Start();

            // Act & Assert
            Assert.DoesNotThrow(() => _wrapper.Stop());
        }

        [Test]
        public async Task AcceptTcpClientAsync_AcceptsConnection()
        {
            // Arrange
            _wrapper = new TcpListenerWrapper(IPAddress.Loopback, _testPort);
            _wrapper.Start();

            // Act
            var clientTask = _wrapper.AcceptTcpClientAsync();

            // Create a client to connect
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, _testPort);

            var acceptedClient = await clientTask;

            // Assert
            Assert.That(acceptedClient, Is.Not.Null);
            Assert.That(acceptedClient.Connected, Is.True);

            acceptedClient.Close();
        }

        [Test]
        public void Stop_ThrowsWhenAcceptingClient()
        {
            // Arrange
            _wrapper = new TcpListenerWrapper(IPAddress.Loopback, _testPort);
            _wrapper.Start();

            // Act
            var acceptTask = _wrapper.AcceptTcpClientAsync();
            _wrapper.Stop();

            // Assert
            Assert.ThrowsAsync<SocketException>(async () => await acceptTask);
        }

        private int GetAvailablePort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}