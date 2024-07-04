using System.IO;
using System.Threading.Tasks;

namespace OwlCore.Storage.OneDrive.Internal;

internal static class StreamExtensions
{
    public static async Task<byte[]> ReadRangeAsync(this Stream stream, int offset, int count)
    {
        var originalPosition = stream.Position;

        var buffer = new byte[count];

        stream.Position = offset;

        await stream.ReadAsync(buffer, 0, count);

        stream.Position = originalPosition;

        return buffer;
    }
}