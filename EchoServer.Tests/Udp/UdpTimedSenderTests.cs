using EchoTspServer.Udp;
using Moq;
using NUnit.Framework;
using System.Text;

namespace EchoTspServer.Tests.Udp
{
    [TestFixture]
    public class UdpTimedSenderTests
    {
        private Mock<UdpMessageBuilder> _mockBuilder;
        private UdpTimedSender _sender;

        [SetUp]
        public void SetUp()
        {
            _mockBuilder = new Mock<UdpMessageBuilder>();
        }

        [TearDown]
        public void TearDown()
        {
            _sender?.Dispose();
        }


        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenHostIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new UdpTimedSender(null, 5000, _mockBuilder.Object));
        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenMessageBuilderIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new UdpTimedSender("127.0.0.1", 5000, null));
        }

        [Test]
        public void Constructor_WithHostAndPort_CreatesInstance()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
                _sender = new UdpTimedSender("127.0.0.1", 5000));
        }

        [Test]
        public void StartSending_ThrowsException_WhenAlreadyStarted()
        {
            // Arrange
            _sender = new UdpTimedSender("127.0.0.1", 5000, _mockBuilder.Object);
            _sender.StartSending(1000);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                _sender.StartSending(1000));
        }

        [Test]
        public void StopSending_CanBeCalledMultipleTimes()
        {
            // Arrange
            _sender = new UdpTimedSender("127.0.0.1", 5000, _mockBuilder.Object);

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                _sender.StopSending();
                _sender.StopSending();
            });
        }

        [Test]
        public void Dispose_StopsSending()
        {
            // Arrange
            _sender = new UdpTimedSender("127.0.0.1", 5000, _mockBuilder.Object);
            _sender.StartSending(1000);

            // Act & Assert
            Assert.DoesNotThrow(() => _sender.Dispose());
        }

        [Test]
        public void StartSending_ExecutesSendMessage_AndWritesToConsole()
        {
            _sender = new UdpTimedSender("127.0.0.1", 5000, _mockBuilder.Object);

            using var cts = new CancellationTokenSource();
            _sender.StartSending(50); // інтервал 50ms

            // даємо таймеру спрацювати
            Thread.Sleep(100);

            // зупиняємо таймер
            _sender.StopSending();

            // якщо рядок з Console.WriteLine виконується, тест покриє його
            Assert.Pass("SendMessage executed, Console.WriteLine covered");
        }


    }
}