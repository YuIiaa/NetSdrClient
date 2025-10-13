using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using NUnit.Framework;

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
        _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        });

        _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        });

        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Callback<byte[]>((bytes) =>
        {
            // Simulate a response for the request-response pattern
            byte[] response = new byte[bytes.Length]; // Simple echo for testing completion
            Array.Copy(bytes, response, bytes.Length);
            _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, response);
        });

        _updMock = new Mock<IUdpClient>();

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
        await _client.ConnectAsync();

        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task StartIQNoConnectionTest()
    {

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
    }

    [Test]
    public async Task StartIQTest()
    {
        //Arrange 
        await _client.ConnectAsync();

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
        Assert.That(_client.IQStarted, Is.True);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(4)); // 3 from connect + 1
    }

    [Test]
    public async Task StopIQTest()
    {
        //Arrange 
        await _client.ConnectAsync();
        await _client.StartIQAsync();  // Add this to logically test stop after start

        //act
        await _client.StopIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(udp => udp.StopListening(), Times.Once);
        Assert.That(_client.IQStarted, Is.False);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(5)); // 3 connect +1 start +1 stop
    }

    [Test]
    public async Task StopIQNoConnectionTest()
    {
        //act
        await _client.StopIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(udp => udp.StopListening(), Times.Never);
        _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
        Assert.That(_client.IQStarted, Is.False);
    }

    [Test]
    public async Task StartIQAlreadyStartedTest()
    {
        //Arrange 
        await _client.ConnectAsync();
        await _client.StartIQAsync();

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(udp => udp.StartListeningAsync(), Times.Once);  // Guard prevents extra
        Assert.That(_client.IQStarted, Is.True);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(5)); // Extra send on second call (always sent)
    }

    [Test]
    public async Task StopIQAlreadyStoppedTest()
    {
        //Arrange 
        await _client.ConnectAsync();
        await _client.StartIQAsync();  // Start first, then stop to make "already stopped"
        await _client.StopIQAsync();

        //act
        await _client.StopIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(udp => udp.StopListening(), Times.Once);  // Guard prevents extra
        Assert.That(_client.IQStarted, Is.False);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(6)); // 3 +1 start +1 first stop +1 second (always sent)
    }

    [Test]
    public async Task ConnectAfterDisconnectTest()
    {
        //Arrange 
        await _client.ConnectAsync();
        _client.Disconect();

        //act
        await _client.ConnectAsync();

        //assert
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Exactly(2));
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(6)); // 3 + 3
    }

    [Test]
    public async Task DisconnectWhileIQStartedTest()
    {
        //Arrange 
        await _client.ConnectAsync();
        await _client.StartIQAsync();

        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _updMock.Verify(udp => udp.StopListening(), Times.Once);
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
        Assert.That(_client.IQStarted, Is.False);
    }

    [Test]
    public async Task ChangeFrequencyNoConnectionTest()
    {
        //act
        await _client.ChangeFrequencyAsync(1000000L, 0);

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
    }

    [Test]
    public async Task ChangeFrequencyTest()
    {
        //Arrange 
        await _client.ConnectAsync();

        //act
        await _client.ChangeFrequencyAsync(1420000000L, 1);

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(4)); // 3 from connect + 1
    }

    [Test]
    public async Task ChangeFrequencyMultipleCallsTest()
    {
        //Arrange 
        await _client.ConnectAsync();
        await _client.ChangeFrequencyAsync(1000000L, 0);

        //act
        await _client.ChangeFrequencyAsync(2000000L, 0);

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(5)); // 3 + 1 + 1
    }

    [Test]
    public async Task ChangeFrequencyWithIQStartedTest()
    {
        //Arrange 
        await _client.ConnectAsync();
        await _client.StartIQAsync();

        //act
        await _client.ChangeFrequencyAsync(1420000000L, 1);

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(5)); // 3 connect + 1 start + 1 change
        Assert.That(_client.IQStarted, Is.True);
    }
}
