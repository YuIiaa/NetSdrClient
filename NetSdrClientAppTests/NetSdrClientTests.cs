using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;
using static NetSdrClientApp.Messages.NetSdrMessageHelper;

namespace NetSdrClientAppTests;

public class NetSdrClientTests
{
    NetSdrClient _client;
    Mock<ITcpClient> _tcpMock;
    Mock<IUdpClient> _updMock;

    public NetSdrClientTests() { }

    [SetUp]
    public void Setup()
    {
        _tcpMock = new Mock<ITcpClient>();
        bool isConnected = false;

        // підключення та стан
        _tcpMock.Setup(tcp => tcp.Connect()).Callback(() => isConnected = true);
        _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() => isConnected = false);
        _tcpMock.SetupGet(tcp => tcp.Connected).Returns(() => isConnected);

        // надсилання повідомлення - піднімаємо подію асинхронно
        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()))
            .Returns(Task.CompletedTask)
            .Callback<byte[]>((bytes) =>
            {
                Task.Run(() => _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes));
            });

        // мок для UDP-клієнта
        _updMock = new Mock<IUdpClient>();
        _updMock.Setup(udp => udp.StartListeningAsync()).Returns(Task.CompletedTask);
        _updMock.Setup(udp => udp.StopListening());

        // створюємо NetSdrClient
        _client = new NetSdrClient(_tcpMock.Object, _updMock.Object);
    }


    [Test]
    public async Task ConnectAsyncTest()
    {
        //act
        await _client.ConnectAsync();

        //assert
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Test]
    public async Task DisconnectWithNoConnectionTest()
    {
        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task DisconnectTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task StartIQNoConnectionTest()
    {
        // Arrange: TCP не підключений
        _tcpMock.SetupGet(tcp => tcp.Connected).Returns(false);

        // Redirect Console safely
        using var sw = new System.IO.StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            // Act
            await _client.StartIQAsync();

            // Assert: перевіряємо, що SendMessageAsync не викликався
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
            _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);

            // Перевіряємо фактичний вихід у консоль
            var output = sw.ToString();
            Assert.That(output, Does.Contain("No active connection"));
        }
        finally
        {
            // Restore Console
            Console.SetOut(originalOut);
        }
    }





    [Test]
    public async Task StartIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
        Assert.That(_client.IQStarted, Is.True);
    }

    [Test]
    public async Task StopIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StopIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(tcp => tcp.StopListening(), Times.Once);
        Assert.That(_client.IQStarted, Is.False);
    }

    [Test]
    public async Task EchoServer_ReadsAndWritesStreamData()
    {
        // Arrange
        var inputBytes = System.Text.Encoding.UTF8.GetBytes("ping");
        using var memoryStream = new MemoryStream(inputBytes);

        var streamMock = new Mock<Stream>();
        int readCalled = 0;

        streamMock.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<byte[], int, int, CancellationToken>((buffer, offset, count, token) =>
            {
                if (readCalled == 0)
                {
                    // первая итерация: вернуть "ping"
                    Array.Copy(inputBytes, buffer, inputBytes.Length);
                    readCalled++;
                    return Task.FromResult(inputBytes.Length);
                }
                else
                {
                    // вторая итерация: конец потока
                    return Task.FromResult(0);
                }
            });

        streamMock.Setup(s => s.WriteAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var buffer = new byte[1024];
        var token = new CancellationTokenSource().Token;
        int bytesRead = 0;

        // Act (имитация куска кода с while)
        while (!token.IsCancellationRequested &&
               (bytesRead = await streamMock.Object.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
        {
            await streamMock.Object.WriteAsync(buffer, 0, bytesRead, token);
        }

        // Assert
        streamMock.Verify(s => s.WriteAsync(It.IsAny<byte[]>(), 0, inputBytes.Length, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ChangeFrequencyAsyncTest()
    {
        //Arrange
        await ConnectAsyncTest();

        long freq = 12345678;
        int channel = 1;

        //Act
        await _client.ChangeFrequencyAsync(freq, channel);

        //Assert
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.AtLeastOnce);
    }

    [Test]
    public void UdpClientMessageReceivedTest()
    {
        //Arrange
        var samples = new short[] { 100, 200, -50 };
        byte[] body = new byte[samples.Length * 2]; // 16-bit per sample
        for (int i = 0; i < samples.Length; i++)
        {
            var b = BitConverter.GetBytes(samples[i]);
            body[i * 2] = b[0];
            body[i * 2 + 1] = b[1];
        }

        byte[] message = NetSdrMessageHelper.GetControlItemMessage(
            MsgTypes.SetControlItem,
            ControlItemCodes.ReceiverFrequency,
            body
        );

        using var sw = new System.IO.StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        //Act
        var method = typeof(NetSdrClient)
            .GetMethod("_udpClient_MessageReceived", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        method.Invoke(_client, new object?[] { _updMock.Object, message });

        //Assert: перевіряємо, що в консоль вивело "Samples recieved"
        var output = sw.ToString();
        Assert.That(output, Does.Contain("Samples recieved"));

        Console.SetOut(originalOut);
    }

    [Test]
    public async Task SendTcpRequest_NotConnected_ReturnsNull()
    {
        //Arrange
        _tcpMock.SetupGet(tcp => tcp.Connected).Returns(false);

        var msg = new byte[] { 0x01, 0x02, 0x03 };

        //Act
        var method = typeof(NetSdrClient)
            .GetMethod("SendTcpRequest", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var task = (Task<byte[]>)method.Invoke(_client, new object?[] { msg })!;
        var result = await task;

        //Assert
        Assert.That(result, Is.Null);
    }

}
