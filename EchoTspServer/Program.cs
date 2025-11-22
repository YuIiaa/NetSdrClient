using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using EchoTspServer.Handlers;
using EchoTspServer.Infrastructure;
using EchoTspServer.Server;
using EchoTspServer.Udp;
using System.Diagnostics.CodeAnalysis;

namespace EchoTspServer
{
    /// <summary>
    /// Entry point for the Echo Server application
    /// </summary>

    public class Program
    {
        [ExcludeFromCodeCoverage]
        public static void Main(string[] args)
        {
            // викликаємо асинхронний метод синхронно
            MainInternal(args, Console.In).GetAwaiter().GetResult();
        }

        // тестований метод з параметром input
        public static async Task MainInternal(string[] args, TextReader input)
        {
            input ??= Console.In;

            var listener = new TcpListenerWrapper(IPAddress.Any, 5000);
            var clientHandler = new EchoClientHandler();
            var server = new EchoServer(listener, clientHandler);

            _ = Task.Run(() => server.StartAsync());

            string host = "127.0.0.1";
            int port = 60000;
            int intervalMilliseconds = 5000;

            using var sender = new UdpTimedSender(host, port);
            sender.StartSending(intervalMilliseconds);

            await WaitForQuitKey(input);

            sender.StopSending();
            server.Stop();
            Console.WriteLine("Sender stopped.");
        }

        // метод для очікування натискання Q
        public static async Task WaitForQuitKey(TextReader input)
        {
            while (true)
            {
                var line = await input.ReadLineAsync();
                if (line?.ToUpper() == "Q") break;
                await Task.Delay(50);
            }
        }
    }
}
