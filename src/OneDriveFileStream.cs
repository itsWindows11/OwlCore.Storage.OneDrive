using Microsoft.Graph.Models;
using OwlCore.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http.Headers;
using System.Net.Http;
using System;
using CommunityToolkit.Diagnostics;
using OwlCore.Storage.OneDrive.Internal;
using System.Collections.Concurrent;
using System.Linq;

namespace OwlCore.Storage.OneDrive;

/// <summary>
/// A lazy stream that reads and writes data to a OneDrive file.
/// </summary>
/// <remarks>
/// If you are trying to use this from your app, you should
/// open the file using <see cref="OneDriveFile"/> instead.
/// </remarks>
internal partial class OneDriveFileStream : LazySeekStream, IAsyncDisposable
{
    private readonly HttpClient? _httpClient;
    private readonly FileAccess _accessMode;

    private readonly ConcurrentQueue<RangeData> _rangesToWrite = new();

    private bool _disposed;

    /// <summary>
    /// The upload session used for uploading data.
    /// </summary>
    public UploadSession? UploadSession { get; }

    /// <inheritdoc />
    public override bool CanRead => _accessMode.HasFlag(FileAccess.Read) && !_disposed;

    /// <inheritdoc />
    public override bool CanWrite => _accessMode.HasFlag(FileAccess.Write) && !_disposed;

    /// <summary>
    /// Initializes a stream over a OneDrive file.
    /// </summary>
    /// <remarks>
    /// If you are trying to use this from your app, you should
    /// open the file using <see cref="OneDriveFile"/> instead.
    /// </remarks>
    /// <param name="drive">The drive to use to locate the file.</param>
    /// <param name="httpClient">The HTTP client to use for uploading.</param>
    /// <param name="item">The drive item.</param>
    /// <param name="uploadSession">The upload session to use for writing.</param>
    /// <param name="readStream">The backing stream to use for reading the file.</param>
    /// <param name="accessMode">The file access mode to use.</param>
    internal OneDriveFileStream(HttpClient? httpClient, UploadSession? uploadSession, Stream readStream, long length, FileAccess accessMode) : base(new LengthOverrideStream(readStream, length))
    {
        _accessMode = accessMode;
        _httpClient = httpClient;

        UploadSession = uploadSession;
    }
}

// Operations.
internal partial class OneDriveFileStream
{
    /// <inheritdoc />
    public override void Flush()
    {
        if (CanWrite)
        {
            if (Position != Length)
                Seek(0, SeekOrigin.End);

            var tasks = _rangesToWrite.Select(x => FlushRangeAsync(x.Offset, x.Count, default));

            // Upload ranges in chunks.
            foreach (var chunk in tasks.Chunk(Environment.ProcessorCount))
                Task.WaitAll(chunk);

            _rangesToWrite.Clear();
        }

        base.Flush();
    }

    /// <inheritdoc />
    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        if (CanWrite)
        {
            if (Position != Length)
                Seek(0, SeekOrigin.End);

            var tasks = _rangesToWrite.Select(x => FlushRangeAsync(x.Offset, x.Count, default));

            // Upload ranges in chunks.
            foreach (var chunk in tasks.Chunk(Environment.ProcessorCount))
                await Task.WhenAll(chunk);

            _rangesToWrite.Clear();
        }

        await base.FlushAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task FlushRangeAsync(int offset, int count, CancellationToken cancellationToken)
    {
        if (!CanWrite)
            throw new NotSupportedException("The stream does not support writing.");

        return UploadRangeAsync(MemoryStream, offset, count, cancellationToken);
    }

    /// <inheritdoc />
    public override void WriteByte(byte value)
    {
        MemoryStream.WriteByte(value);
        _rangesToWrite.Enqueue(new RangeData((int)Position, 1, 1));
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        MemoryStream.Write(buffer, offset, count);
        _rangesToWrite.Enqueue(new RangeData(offset, count, buffer.Length));
    }

    /// <inheritdoc />
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await MemoryStream.WriteAsync(buffer, offset, count, cancellationToken);
        _rangesToWrite.Enqueue(new RangeData(offset, count, buffer.Length));
    }

    private async Task UploadRangeAsync(MemoryStream stream, int offset, int count, CancellationToken cancellationToken)
    {
        if (UploadSession == null && _httpClient == null)
            ThrowHelper.ThrowInvalidOperationException("Cannot write to this stream. The upload session is not available.");
        if (stream == null)
            ThrowHelper.ThrowArgumentNullException(nameof(stream));
        if (offset < 0 || offset >= stream.Length)
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(offset));
        if (count <= 0 || offset + count > stream.Length)
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(count));

        // TODO: Find a way to optimize this.
        var buffer = stream.ToArray().Skip(offset).Take(count).ToArray();

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, UploadSession!.UploadUrl)
        {
            Content = new ByteArrayContent(buffer, offset, count)
        };

        httpRequestMessage.Content.Headers.ContentRange = new ContentRangeHeaderValue(offset, offset + count - 1, stream.Length);

        try
        {
            var response = await _httpClient!.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
        } catch (Exception ex)
        {
            throw new Exception("Failed to upload data to the OneDrive item.", ex);
        }
    }
}

// Dispose.
internal partial class OneDriveFileStream
{
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            Flush();

            _httpClient?.Dispose();
            _rangesToWrite.Clear();

            _disposed = true;
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await FlushAsync();

            _httpClient?.Dispose();
            _rangesToWrite.Clear();

            _disposed = true;
        }

        base.Dispose();
    }
}