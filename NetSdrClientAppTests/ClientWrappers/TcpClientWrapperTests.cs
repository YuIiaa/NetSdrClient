using NUnit.Framework;
using NetSdrClientApp.Networking;
using System.Diagnostics.CodeAnalysis;

namespace NetSdrClientApp.Tests
{
    [TestFixture]
    public class TcpClientWrapperTests
    {
        private string _host = "127.0.0.1";

        [Test]
        public void Constructor_SetsHostAndPort()
        {
            var client = new TcpClientWrapper(_host, 1234);
            Assert.That(client, Is.Not.Null);
        }

        [Test]
        public void Connected_ReturnsFalse_WhenNotConnected()
        {
            var client = new TcpClientWrapper(_host, 1234);
            Assert.That(client.Connected, Is.False);
        }
    }
}
