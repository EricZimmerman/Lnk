using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Lnk;

internal static class Helpers
{
    private static readonly ConcurrentDictionary<int, Encoding> Encodings = new();

    /// <summary>
    ///     Signature of the BEEF0004 shell item extension block as stored on disk (0xBEEF0004, little endian).
    /// </summary>
    public static readonly byte[] Beef0004Signature = { 0x04, 0x00, 0xEF, 0xBE };

    public static string GetDescriptionFromEnumValue(Enum value)
    {
        var attribute = value.GetType()
            .GetField(value.ToString())
            ?.GetCustomAttributes(typeof(DescriptionAttribute), false)
            .SingleOrDefault() as DescriptionAttribute;
        return attribute == null ? value.ToString() : attribute.Description;
    }

    /// <summary>
    ///     Resolves a code page once and caches it. Falls back to Windows-1252 if the code page is unknown.
    /// </summary>
    public static Encoding GetEncoding(int codepage)
    {
        return Encodings.GetOrAdd(codepage, cp =>
        {
            try
            {
                return CodePagesEncodingProvider.Instance.GetEncoding(cp) ?? Encoding.GetEncoding(cp);
            }
            catch (Exception)
            {
                return CodePagesEncodingProvider.Instance.GetEncoding(1252);
            }
        });
    }

    /// <summary>
    ///     Throws <see cref="LnkParseException" /> unless [offset, offset + count) lies inside the first
    ///     <paramref name="limit" /> bytes of <paramref name="buffer" />.
    /// </summary>
    public static void EnsureRange(byte[] buffer, long offset, long count, string field, int limit = -1)
    {
        var end = limit < 0 ? buffer.Length : Math.Min(limit, buffer.Length);
        if (offset < 0 || count < 0 || offset + count > end)
        {
            throw new LnkParseException(
                $"Invalid {field} data (offset {offset}, length {count}, available {end}) - is this file corrupt?",
                (int)Math.Min(Math.Max(offset, -1), int.MaxValue), field);
        }
    }

    public static ushort ReadUInt16(byte[] buffer, long offset, string field, int limit = -1)
    {
        EnsureRange(buffer, offset, 2, field, limit);
        return BitConverter.ToUInt16(buffer, (int)offset);
    }

    public static uint ReadUInt32(byte[] buffer, long offset, string field, int limit = -1)
    {
        EnsureRange(buffer, offset, 4, field, limit);
        return BitConverter.ToUInt32(buffer, (int)offset);
    }

    public static int ReadInt32(byte[] buffer, long offset, string field, int limit = -1)
    {
        EnsureRange(buffer, offset, 4, field, limit);
        return BitConverter.ToInt32(buffer, (int)offset);
    }

    public static long ReadInt64(byte[] buffer, long offset, string field, int limit = -1)
    {
        EnsureRange(buffer, offset, 8, field, limit);
        return BitConverter.ToInt64(buffer, (int)offset);
    }

    public static Guid ReadGuid(byte[] buffer, long offset, string field, int limit = -1)
    {
        EnsureRange(buffer, offset, 16, field, limit);
        var g = new byte[16];
        Buffer.BlockCopy(buffer, (int)offset, g, 0, 16);
        return new Guid(g);
    }

    public static byte[] Slice(byte[] buffer, long offset, long count, string field, int limit = -1)
    {
        EnsureRange(buffer, offset, count, field, limit);
        var b = new byte[count];
        Buffer.BlockCopy(buffer, (int)offset, b, 0, (int)count);
        return b;
    }

    /// <summary>
    ///     Decodes a NUL-terminated single/multi-byte string that starts at <paramref name="offset" /> and must end
    ///     before <paramref name="limit" />. A missing terminator yields everything up to the limit.
    /// </summary>
    public static string ReadAnsiZ(byte[] buffer, long offset, Encoding encoding, string field, int limit = -1)
    {
        var end = limit < 0 ? buffer.Length : Math.Min(limit, buffer.Length);
        EnsureRange(buffer, offset, 0, field, end);
        var start = (int)offset;
        var nul = Array.IndexOf(buffer, (byte)0, start, end - start);
        var len = (nul < 0 ? end : nul) - start;
        return len == 0 ? string.Empty : encoding.GetString(buffer, start, len);
    }

    /// <summary>
    ///     Decodes a NUL-terminated UTF-16LE string that starts at <paramref name="offset" /> and must end before
    ///     <paramref name="limit" />. A missing terminator yields everything up to the limit.
    /// </summary>
    public static string ReadUnicodeZ(byte[] buffer, long offset, string field, int limit = -1)
    {
        var end = limit < 0 ? buffer.Length : Math.Min(limit, buffer.Length);
        EnsureRange(buffer, offset, 0, field, end);
        var start = (int)offset;
        var i = start;
        while (i + 1 < end && (buffer[i] != 0 || buffer[i + 1] != 0))
        {
            i += 2;
        }

        var len = Math.Min(i, end) - start;
        len -= len % 2;
        return len <= 0 ? string.Empty : Encoding.Unicode.GetString(buffer, start, len);
    }

    /// <summary>
    ///     Returns the index of the first occurrence of <paramref name="pattern" /> at or after
    ///     <paramref name="start" />, or -1. Replaces the BitConverter.ToString(...).IndexOf("..") idiom.
    /// </summary>
    public static int IndexOf(byte[] buffer, byte[] pattern, int start = 0)
    {
        if (start < 0)
        {
            start = 0;
        }

        var last = buffer.Length - pattern.Length;
        var first = pattern[0];
        for (var i = Array.IndexOf(buffer, first, start); i >= 0 && i <= last; i = Array.IndexOf(buffer, first, i + 1))
        {
            var match = true;
            for (var j = 1; j < pattern.Length; j++)
            {
                if (buffer[i + j] != pattern[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }

            if (i + 1 > last)
            {
                break;
            }
        }

        return -1;
    }

    /// <summary>
    ///     Same result as <c>buffer.Skip(offset).Take(count).ToArray()</c> (clamped to the buffer, never throws)
    ///     without enumerating the array byte by byte.
    /// </summary>
    public static byte[] SliceClamped(byte[] buffer, int offset, int count)
    {
        if (offset < 0)
        {
            offset = 0;
        }

        if (offset >= buffer.Length || count <= 0)
        {
            return Array.Empty<byte>();
        }

        var len = Math.Min(count, buffer.Length - offset);
        var b = new byte[len];
        Buffer.BlockCopy(buffer, offset, b, 0, len);
        return b;
    }

    /// <summary>
    ///     Finds shell item extension block signatures (0xBEEFxxxx, stored as xx 00 EF BE) and returns the offset of
    ///     each signature, scanning without overlap. Replaces a regex over <c>BitConverter.ToString(buffer)</c>.
    /// </summary>
    public static List<int> FindExtensionBlockSignatures(byte[] buffer)
    {
        var result = new List<int>();
        for (var i = 0; i + 3 < buffer.Length;)
        {
            var e = Array.IndexOf(buffer, (byte) 0xEF, i + 2);
            if (e < 0 || e + 1 >= buffer.Length)
            {
                break;
            }

            var p = e - 2;
            if (buffer[e + 1] == 0xBE && buffer[e - 1] == 0x00)
            {
                result.Add(p);
                i = p + 4;
            }
            else
            {
                i = p + 1;
            }
        }

        return result;
    }

    /// <summary>
    ///     Converts a FILETIME to a UTC <see cref="DateTimeOffset" />. Returns null when the value cannot be represented.
    /// </summary>
    public static DateTimeOffset? FromFileTime(long fileTime)
    {
        if (fileTime < 0 || fileTime > DateTime.MaxValue.ToFileTimeUtc())
        {
            return null;
        }

        return new DateTimeOffset(DateTime.FromFileTimeUtc(fileTime), TimeSpan.Zero);
    }
}
