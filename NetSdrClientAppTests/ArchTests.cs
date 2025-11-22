using NetArchTest.Rules;
using NUnit.Framework;
using System.Linq;
using System.Reflection;
using System.Text;
namespace NetSdrClientAppTests
{
    public class ArchitectureTests
    {
        // 🔹 UI шар не повинен напряму залежати від серверної логіки
        [Test]
        public void App_Should_Not_Depend_On_EchoServer()
        {
            var result = Types.InAssembly(typeof(NetSdrClientApp.NetSdrClient).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp")
                .ShouldNot()
                .HaveDependencyOn("EchoServer")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True,
                "UI шар (NetSdrClientApp) не повинен напряму залежати від EchoServer!");
        }

        // 🔹 Повідомлення не мають залежати від мережевого шару
        [Test]
        public void Messages_Should_Not_Depend_On_Networking()
        {
            var result = Types.InAssembly(typeof(NetSdrClientApp.Messages.NetSdrMessageHelper).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp.Messages")
                .ShouldNot()
                .HaveDependencyOn("NetSdrClientApp.Networking")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True,
                "Шар Messages не повинен залежати від Networking!");
        }

        // 🔹 Мережевий шар не повинен залежати від повідомлень
        [Test]
        public void Networking_Should_Not_Depend_On_Messages()
        {
            var result = Types.InAssembly(typeof(NetSdrClientApp.Networking.ITcpClient).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp.Networking")
                .ShouldNot()
                .HaveDependencyOn("NetSdrClientApp.Messages")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True,
                "Шар Networking не повинен залежати від Messages!");
        }

        
        // 🔹 UI може залежати лише від внутрішніх просторів (Messages, Networking)
        [Test]
        public void App_Should_Only_Depend_On_Internal_Layers()
        {
            var result = Types.InAssembly(typeof(NetSdrClientApp.NetSdrClient).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp")
                .Should()
                .OnlyHaveDependenciesOn(
                    "System",
                    "NetSdrClientApp.Messages",
                    "NetSdrClientApp.Networking"
                )
                .GetResult();

            
        }
    }
}