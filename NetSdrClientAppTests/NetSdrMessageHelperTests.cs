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
        [Test]
        public void TranslateMessage_ControlItem_Success()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverFrequency;
            byte[] parameters = { 1, 2, 3, 4 };

            // Act
            var msg = NetSdrMessageHelper.GetControlItemMessage(type, code, parameters);
            bool success = NetSdrMessageHelper.TranslateMessage(msg, out var parsedType, out var parsedCode, out var seq, out var body);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(parsedType, Is.EqualTo(type));
            Assert.That(parsedCode, Is.EqualTo(code));
            Assert.That(seq, Is.EqualTo(0));
            Assert.That(body, Is.EqualTo(parameters));
        }

        [Test]
        public void TranslateMessage_DataItem_Success()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem1;
            byte[] parameters = { 9, 8, 7, 6 };

            // Act
            var msg = NetSdrMessageHelper.GetDataItemMessage(type, parameters);
            bool success = NetSdrMessageHelper.TranslateMessage(msg, out var parsedType, out var parsedCode, out var seq, out var body);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(parsedType, Is.EqualTo(type));
            Assert.That(parsedCode, Is.EqualTo(NetSdrMessageHelper.ControlItemCodes.None));
            Assert.That(body.Length, Is.EqualTo(parameters.Length - 2)); // minus sequence bytes
        }

        [Test]
        public void TranslateMessage_InvalidControlItemCode_ShouldFail()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, (NetSdrMessageHelper.ControlItemCodes)9999, new byte[] { 1, 2 });

            // corrupt item code bytes to an undefined value
            msg[2] = 0xFF;
            msg[3] = 0xFF;

            // Act
            bool success = NetSdrMessageHelper.TranslateMessage(msg, out _, out _, out _, out _);

            // Assert
            Assert.That(success, Is.False);
        }

        [Test]
        public void TranslateMessage_WrongBodyLength_ShouldFail()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var msg = NetSdrMessageHelper.GetControlItemMessage(type, NetSdrMessageHelper.ControlItemCodes.RFFilter, new byte[] { 1, 2 });

            // Cut last byte to mismatch length
            msg = msg.Take(msg.Length - 1).ToArray();

            // Act
            bool success = NetSdrMessageHelper.TranslateMessage(msg, out _, out _, out _, out _);

            // Assert
            Assert.That(success, Is.False);
        }

        [Test]
        public void GetSamples_ValidSamples_ShouldReturnIntegers()
        {
            // Arrange
            ushort sampleSize = 16;
            byte[] body = { 1, 0, 2, 0, 3, 0, 4, 0 };

            // Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

            // Assert
            Assert.That(samples.Count, Is.EqualTo(4));
            Assert.That(samples[0], Is.EqualTo(BitConverter.ToInt32(new byte[] { 1, 0, 0, 0 })));
        }

        [Test]
        public void GetSamples_InvalidSampleSize_ShouldThrow()
        {
            // Arrange + Act + Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                NetSdrMessageHelper.GetSamples(64, new byte[] { 1, 2, 3 }).ToList();
            });
        }

        [Test]
        public void GetControlItemMessage_TooLong_ShouldThrow()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.SetControlItem;
            var code = NetSdrMessageHelper.ControlItemCodes.RFFilter;
            var longData = new byte[8200];

            // Act + Assert
            Assert.Throws<ArgumentException>(() =>
            {
                NetSdrMessageHelper.GetControlItemMessage(type, code, longData);
            });
        }

        [Test]
        public void GetHeader_EdgeCase_DataItem_MaxLength_ShouldWrapToZero()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem0;
            var msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[8192]);

            // Act
            bool success = NetSdrMessageHelper.TranslateMessage(msg, out var parsedType, out _, out _, out var body);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(parsedType, Is.EqualTo(type));
            Assert.That(body.Length, Is.EqualTo(8192 - 2));
        }
    }
}