using System.Net.Sockets;
using EchoTspServer.Interfaces;

namespace EchoTspServer.Server
{
    /// <summary>
    /// TCP Echo Server with dependency injection support
    /// </summary>
    public class EchoServer
    {
        private readonly ITcpListenerWrapper _listener;
        private readonly IClientHandler _clientHandler;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public EchoServer(ITcpListenerWrapper listener, IClientHandler clientHandler)
        {
            _listener = listener ?? throw new ArgumentNullException(nameof(listener));
            _clientHandler = clientHandler ?? throw new ArgumentNullException(nameof(clientHandler));
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            _listener.Start();
            Console.WriteLine("Server started.");

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    Console.WriteLine("Client connected.");

                    _ = Task.Run(async () => await HandleClientConnectionAsync(client, _cancellationTokenSource.Token));
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }

            Console.WriteLine("Server shutdown.");
        }

        private async Task HandleClientConnectionAsync(TcpClient client, CancellationToken token)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    await _clientHandler.HandleClientAsync(stream, token);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                client.Close();
                Console.WriteLine("Client disconnected.");
            }
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _listener.Stop();
            _cancellationTokenSource.Dispose();
            Console.WriteLine("Server stopped.");
        }
    }
}