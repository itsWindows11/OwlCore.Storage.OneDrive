using Microsoft.Graph.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage.OneDrive;

/// <summary>
/// A stream for writing to OneDrive upload sessions.
/// </summary>
public sealed class OneDriveWriteStream : Stream
{
    private readonly HttpClient _httpClient;
    private bool _isDisposed = false;

    /// <summary>
    /// Creates a new instance of <see cref="OneDriveWriteStream" />.
    /// </summary>
    /// <param name="uploadSession">The upload session to use for writing.</param>
    public OneDriveWriteStream(UploadSession uploadSession)
    {
        UploadSession = uploadSession;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// The upload session this stream was created with.
    /// </summary>
    public UploadSession UploadSession { get; }

    /// <inheritdoc />
    public override bool CanRead => !_isDisposed;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => !_isDisposed;

    /// <inheritdoc />
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc />
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override void Flush()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        WriteAsync(buffer, offset, count).Wait();
    }

    /// <inheritdoc />
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream(buffer);

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, UploadSession.UploadUrl)
        {
            Content = new StreamContent(memoryStream)
        };

        httpRequestMessage.Content.Headers.ContentLength = buffer.LongLength;
        httpRequestMessage.Content.Headers.ContentRange = new ContentRangeHeaderValue(offset, offset + count - 1);

        var response = await _httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        _httpClient.Dispose();
        _isDisposed = true;
    }
}