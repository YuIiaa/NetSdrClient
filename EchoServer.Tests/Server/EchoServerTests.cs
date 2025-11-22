using EchoTspServer.Interfaces;
using EchoTspServer.Server;
using Moq;
using NUnit.Framework;
using System.Net.Sockets;

namespace EchoTspServer.Tests.Server
{
    [TestFixture]
    public class EchoServerTests
    {
        private Mock<ITcpListenerWrapper> _mockListener;
        private Mock<IClientHandler> _mockHandler;
        private EchoServer _server;

        [SetUp]
        public void SetUp()
        {
            _mockListener = new Mock<ITcpListenerWrapper>();
            _mockHandler = new Mock<IClientHandler>();
            _server = new EchoServer(_mockListener.Object, _mockHandler.Object);
        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenListenerIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new EchoServer(null, _mockHandler.Object));
        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenHandlerIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new EchoServer(_mockListener.Object, null));
        }

        [Test]
        public async Task StartAsync_StartsListener()
        {
            // Arrange
            _mockListener.Setup(l => l.AcceptTcpClientAsync())
                .ThrowsAsync(new ObjectDisposedException("test"));

            // Act
            await _server.StartAsync();

            // Assert
            _mockListener.Verify(l => l.Start(), Times.Once);
        }

        [Test]
        public void Stop_StopsListener()
        {
            // Act
            _server.Stop();

            // Assert
            _mockListener.Verify(l => l.Stop(), Times.Once);
        }

        [Test]
        public async Task StartAsync_HandlesObjectDisposedException()
        {
            // Arrange
            _mockListener.Setup(l => l.AcceptTcpClientAsync())
                .ThrowsAsync(new ObjectDisposedException("listener"));

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _server.StartAsync());
        }
    }
}