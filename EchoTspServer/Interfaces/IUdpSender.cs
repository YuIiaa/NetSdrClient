namespace EchoTspServer.Interfaces
{
    /// <summary>
    /// Interface for UDP message sending
    /// </summary>
    public interface IUdpSender : IDisposable
    {
        void StartSending(int intervalMilliseconds);
        void StopSending();
    }
}