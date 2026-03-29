using System;
using System.Buffers;
using System.Text;

namespace MiniToolBoxCross.Common.Helper;

public static class CommandLinePackageEncoder
{
    public static ReadOnlyMemory<byte> Encode(string key, string body)
    {
        var bufferSize = Encoding.UTF8.GetByteCount(key) + 1 + Encoding.UTF8.GetByteCount(body) + 2;

        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            var span = buffer.AsSpan();
            var offset = 0;

            offset += Encoding.UTF8.GetBytes(key, span[offset..]);

            span[offset++] = (byte)' ';

            offset += Encoding.UTF8.GetBytes(body, span[offset..]);

            span[offset++] = (byte)'\r';
            span[offset++] = (byte)'\n';

            var result = new byte[offset];
            span[..offset].CopyTo(result);
            return result.AsMemory();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
