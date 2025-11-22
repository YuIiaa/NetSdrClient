using EchoTspServer.Udp;
using NUnit.Framework;

namespace EchoTspServer.Tests.Udp
{
    [TestFixture]
    public class UdpMessageBuilderTests
    {
        private UdpMessageBuilder _builder;

        [SetUp]
        public void SetUp()
        {
            _builder = new UdpMessageBuilder();
        }

        [Test]
        public void BuildMessage_ReturnsCorrectMessageLength()
        {
            // Act
            var message = _builder.BuildMessage();

            // Assert
            // Header (2) + Sequence (2) + Samples (1024) = 1028
            Assert.That(message.Length, Is.EqualTo(1028));
        }

        [Test]
        public void BuildMessage_StartsWithCorrectHeader()
        {
            // Act
            var message = _builder.BuildMessage();

            // Assert
            Assert.That(message[0], Is.EqualTo(0x04));
            Assert.That(message[1], Is.EqualTo(0x84));
        }

        [Test]
        public void BuildMessage_GeneratesDifferentPayloads()
        {
            // Act
            var message1 = _builder.BuildMessage();
            var message2 = _builder.BuildMessage();

            // Extract payloads (skip header + sequence = 4 bytes)
            var payload1 = message1.Skip(4).ToArray();
            var payload2 = message2.Skip(4).ToArray();

            // Assert - payloads should be different (random data)
            Assert.That(payload1, Is.Not.EqualTo(payload2));
        }

        [Test]
        public void GetCurrentSequenceNumber_ReturnsCorrectValue()
        {
            // Act
            _builder.BuildMessage();
            _builder.BuildMessage();
            var currentSeq = _builder.GetCurrentSequenceNumber();

            // Assert
            Assert.That(currentSeq, Is.EqualTo(2));
        }

        [Test]
        public void GetCurrentSequenceNumber_InitiallyZero()
        {
            // Act
            var currentSeq = _builder.GetCurrentSequenceNumber();

            // Assert
            Assert.That(currentSeq, Is.EqualTo(0));
        }
    }
}