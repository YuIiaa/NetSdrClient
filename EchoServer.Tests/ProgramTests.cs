using NUnit.Framework;
using Moq;
using EchoTspServer.Server;
using EchoTspServer.Handlers;
using EchoTspServer.Udp;
using System.Threading.Tasks;

namespace EchoTspServer.Tests
{
    [TestFixture]
    public class ProgramTests
    {
        [Test]
        public async Task Main_ShouldRunThroughAllLines()
        {
            using var reader = new StringReader("Q\n");
            await Program.MainInternal(new string[0], reader);

            Assert.Pass();
        }

        [Test]
        public async Task WaitForQuitKey_ShouldExitOnQ()
        {
            // підготовка StringReader з рядками, Q завершує цикл
            using var reader = new StringReader("hello\nworld\nQ\n");

            // виклик тестованого методу
            await Program.WaitForQuitKey(reader);

            // якщо метод завершився без виключень — тест успішний
            Assert.Pass();
        }
    }
}
