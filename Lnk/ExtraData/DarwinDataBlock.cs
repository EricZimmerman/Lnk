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
        Signature = ExtraDataTypes.DarwinDataBlock;

        Size = BitConverter.ToUInt32(rawBytes, 0);

        //TODO can these be decoded further?
        ApplicationIdentifierAscii = CodePagesEncodingProvider.Instance.GetEncoding(codepage)?.GetString(rawBytes, 8, 260).Split('\0').First();
        ApplicationIdentifierUnicode = Encoding.Unicode.GetString(rawBytes, 268, 520).Split('\0').First();

        var sepChar = '>';

        if (ApplicationIdentifierAscii.Contains("<"))
        {
            sepChar = '<';
        }

        var segs = ApplicationIdentifierAscii.Split(sepChar);

        var left = segs[0];

        var rawDarwin = left.Substring(0, 20);

        ProductCode = DecodeDarwinToGuid(rawDarwin);

        FeatureName = "(None)";
        if (left.Length > 20)
        {
            FeatureName = left.Substring(20);
        }

        ComponentId = "(None)";
        if (segs.Length > 1)
        {
            rawDarwin = segs[1];

            ComponentId = DecodeDarwinToGuid(rawDarwin);
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