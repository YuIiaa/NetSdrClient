using System;
using System.Linq;
using NUnit.Framework;
using NetSdrClientApp.Messages;

namespace NetSdrClientAppTests
{
    public class NetSdrMessageHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void GetControlItemMessageTest()
        {
            
            // Навмисне порушення — створюємо залежність Messages → Networking
            var _ = typeof(NetSdrClientApp.Networking.ITcpClient);
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            int parametersLength = 7500;

            //Act
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var codeBytes = msg.Skip(2).Take(2);
            var parametersBytes = msg.Skip(4);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);
            var actualCode = BitConverter.ToInt16(codeBytes.ToArray());

            //Assert
            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));
            Assert.That(actualCode, Is.EqualTo((short)code));
            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void GetDataItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem2;
            int parametersLength = 7500;

            //Act
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var parametersBytes = msg.Skip(2);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);

            //Assert
            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));
            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        // ------------------- Тести для GetSamples -------------------
        [Test]
        public void GetSamples_ReturnsCorrectValues_For8bitSamples()
        {
            byte[] body = { 1, 2, 3, 4 };
            ushort sampleSize = 8;

            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToArray();

            Assert.That(samples.Length, Is.EqualTo(4));
            Assert.That(samples[0], Is.EqualTo(1));
            Assert.That(samples[1], Is.EqualTo(2));
            Assert.That(samples[2], Is.EqualTo(3));
            Assert.That(samples[3], Is.EqualTo(4));
        }

        [Test]
        public void GetSamples_ReturnsCorrectValues_For16bitSamples()
        {
            byte[] body = { 1, 0, 2, 0 };
            ushort sampleSize = 16;

            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToArray();

            Assert.That(samples.Length, Is.EqualTo(2));
            Assert.That(samples[0], Is.EqualTo(1));
            Assert.That(samples[1], Is.EqualTo(2));
        }

        [Test]
        public void GetSamples_ReturnsCorrectValues_For24bitSamples()
        {
            byte[] body = { 1, 2, 3, 4, 5, 6 };
            ushort sampleSize = 24;

            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToArray();

            Assert.That(samples.Length, Is.EqualTo(2));
            Assert.That(samples[0], Is.EqualTo(BitConverter.ToInt32(new byte[] { 1, 2, 3, 0 }, 0)));
            Assert.That(samples[1], Is.EqualTo(BitConverter.ToInt32(new byte[] { 4, 5, 6, 0 }, 0)));
        }

        [Test]
        public void GetSamples_ThrowsArgumentOutOfRangeException_WhenSampleSizeTooBig()
        {
            byte[] body = { 1, 2, 3, 4 };
            ushort sampleSize = 40;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                NetSdrMessageHelper.GetSamples(sampleSize, body).ToArray();
            });
        }
    }
}