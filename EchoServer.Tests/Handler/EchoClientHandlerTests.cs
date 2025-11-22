using EchoTspServer.Handlers;
using NUnit.Framework;
using System.Text;

namespace EchoTspServer.Tests.Handlers
{
    [TestFixture]
    public class EchoClientHandlerTests
    {
        private EchoClientHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new EchoClientHandler();
        }

        [Test]
        public async Task HandleClientAsync_EchoesDataBack()
        {
            // Arrange
            var inputData = Encoding.UTF8.GetBytes("Hello, World!");
            var inputStream = new MemoryStream(inputData);
            var outputStream = new MemoryStream();
            var combinedStream = new CombinedStream(inputStream, outputStream);
            var cts = new CancellationTokenSource();

            // Act
            await _handler.HandleClientAsync(combinedStream, cts.Token);

            // Assert
            var outputData = outputStream.ToArray();
            Assert.That(outputData, Is.EqualTo(inputData));
        }

        [Test]
        public async Task HandleClientAsync_HandlesEmptyStream()
        {
            // Arrange
            var emptyStream = new MemoryStream();
            var cts = new CancellationTokenSource();

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
                await _handler.HandleClientAsync(emptyStream, cts.Token));
        }

        [Test]
        public async Task HandleClientAsync_EchoesMultipleChunks()
        {
            // Arrange
            var chunk1 = Encoding.UTF8.GetBytes("First");
            var chunk2 = Encoding.UTF8.GetBytes("Second");
            var combined = chunk1.Concat(chunk2).ToArray();

            var inputStream = new MemoryStream(combined);
            var outputStream = new MemoryStream();
            var combinedStream = new CombinedStream(inputStream, outputStream);
            var cts = new CancellationTokenSource();

            // Act
            await _handler.HandleClientAsync(combinedStream, cts.Token);

            // Assert
            var result = outputStream.ToArray();
            Assert.That(result, Is.EqualTo(combined));
        }
    }

    /// <summary>
    /// Helper class for testing - combines read and write streams
    /// </summary>
    public class CombinedStream : Stream
    {
        private readonly Stream _readStream;
        private readonly Stream _writeStream;

        public CombinedStream(Stream readStream, Stream writeStream)
        {
            _readStream = readStream;
            _writeStream = writeStream;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _readStream.Length;
        public override long Position
        {
            get => _readStream.Position;
            set => _readStream.Position = value;
        }

        public override void Flush() => _writeStream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
            => _readStream.Read(buffer, offset, count);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _readStream.ReadAsync(buffer, offset, count, cancellationToken);

        public override void Write(byte[] buffer, int offset, int count)
            => _writeStream.Write(buffer, offset, count);

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _writeStream.WriteAsync(buffer, offset, count, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Helper stream that simulates slow reading for cancellation tests
    /// </summary>
    public class SlowMemoryStream : MemoryStream
    {
        public SlowMemoryStream(byte[] buffer) : base(buffer) { }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await Task.Delay(1000, cancellationToken);
            return await base.ReadAsync(buffer, offset, count, cancellationToken);
        }
    }
}