namespace EchoTspServer.Interfaces
{
    /// <summary>
    /// Interface for handling client connections
    /// </summary>
    public interface IClientHandler
    {
        Task HandleClientAsync(Stream stream, CancellationToken token);
    }
}