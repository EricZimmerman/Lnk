using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lnk.ExtraData;

public class DarwinDataBlock : ExtraDataBase
{
    private const string Base85 =
        "!$%&'()*+,-.0123456789=?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{}~";

    public DarwinDataBlock(byte[] rawBytes, int codepage=1252)
    {
        RequireSize(rawBytes, 0x314, "DarwinDataBlock");

        Signature = ExtraDataTypes.DarwinDataBlock;

        Size = BitConverter.ToUInt32(rawBytes, 0);

        // DarwinDataAnsi (260 bytes) at offset 8, DarwinDataUnicode (520 bytes) at offset 268
        ApplicationIdentifierAscii = Helpers.ReadAnsiZ(rawBytes, 8, Helpers.GetEncoding(codepage), "DarwinDataAnsi", 8 + 260);
        ApplicationIdentifierUnicode = Helpers.ReadUnicodeZ(rawBytes, 268, "DarwinDataUnicode", 268 + 520);

        ProductCode = "(None)";
        FeatureName = "(None)";
        ComponentId = "(None)";

        // The descriptor is normally identical in both fields; fall back to the Unicode copy if the ANSI one is empty.
        var descriptor = ApplicationIdentifierAscii.Length > 0 ? ApplicationIdentifierAscii : ApplicationIdentifierUnicode;

        // Compressed MSI descriptor: 20 char product code, feature name, '>' or '<', 20 char component id
        var sepChar = descriptor.Contains("<") ? '<' : '>';

        var segs = descriptor.Split(sepChar);

        var left = segs[0];

        if (left.Length >= 20)
        {
            ProductCode = DecodeDarwinToGuid(left.Substring(0, 20));

            if (left.Length > 20)
            {
                FeatureName = left.Substring(20);
            }
        }

        if (segs.Length > 1 && segs[1].Length >= 20)
        {
            ComponentId = DecodeDarwinToGuid(segs[1].Substring(0, 20));
        }
    }

    public string FeatureName { get; }
    public string ComponentId { get; }
    public string ProductCode { get; }

    public string ApplicationIdentifierAscii { get; }
    public string ApplicationIdentifierUnicode { get; }

    private string DecodeDarwinToGuid(string rawDarwin)
    {
        var chunks = new string[4];

        if (rawDarwin.Length == 0)
        {
            return "";
        }

        for (var i = 0; i < 4; i++)
        {
            var offset = i * 5;

            var chunk = rawDarwin.Substring(offset, 5);
            chunks[i] = chunk;
        }

        var hexChunks = new List<int>();

        foreach (var chunk in chunks)
        {
            var f = QuadToHex(chunk);

            hexChunks.Add(f);
        }

        var block1 = hexChunks[0].ToString("X8");
        var block2 = hexChunks[1].ToString("X8");

        var block2left = block2.Substring(0, 4);
        var block2right = block2.Substring(4);

        var block3 = hexChunks[2].ToString("X8");

        var b3 = FlipIt(block3);
        var block3left = b3.Substring(0, 4);
        var block3right = b3.Substring(4);

        var block4 = hexChunks[3].ToString("X8");

        var b4 = FlipIt(block4);

        return "{" + $"{block1}-{block2right}-{block2left}-{block3left}-{block3right}{b4}" + "}";
    }

    private string FlipIt(string start)
    {
        var subs = new List<string>();

        for (var i = 0; i < 4; i++)
        {
            var index = 2 * i;

            var sub = start.Substring(index, 2);
            subs.Add(sub);
        }

        subs.Reverse();
        return string.Join("", subs);
    }

    private int QuadToHex(string quad)
    {
        var dd = 0;

        for (var i = 4; i > -1; i--)
        {
            var foo = Base85.IndexOf(quad[i]);
            if (foo < 0)
            {
                throw new LnkParseException($"Invalid character '{quad[i]}' in Darwin descriptor", -1, "DarwinData");
            }

            dd += foo;

            if (i > 0)
            {
                dd *= 85;
            }
        }

        return dd;
    }

    public override string ToString()
    {
        return $"Darwin data block" +
               $"\r\nRaw data: {ApplicationIdentifierAscii}, Product code: {ProductCode}, Feature name: {FeatureName}, ComponentId: {ComponentId}";
    }
}