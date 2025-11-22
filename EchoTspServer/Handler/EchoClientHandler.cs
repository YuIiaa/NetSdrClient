using EchoTspServer.Interfaces;

namespace EchoTspServer.Handlers
{
    /// <summary>
    /// Handles echo logic for connected clients
    /// </summary>
    public class EchoClientHandler : IClientHandler
    {
        private const int BufferSize = 8192;

        public async Task HandleClientAsync(Stream stream, CancellationToken token)
        {
            byte[] buffer = new byte[BufferSize];
            int bytesRead;

            while (!token.IsCancellationRequested &&
                   (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
            {
                await stream.WriteAsync(buffer, 0, bytesRead, token);
                Console.WriteLine($"Echoed {bytesRead} bytes to the client.");
            }
        }
    }
}