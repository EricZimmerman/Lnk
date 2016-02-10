using System;
using System.Linq;
using System.Text;

namespace Lnk.ExtraData
{
    public class DarwinDataBlock : ExtraDataBase
    {
        public DarwinDataBlock(byte[] rawBytes)
        {
            Signature = ExtraDataTypes.DarwinDataBlock;

            Size = BitConverter.ToUInt32(rawBytes, 0);

            //TODO can these be decoded further?
            ApplicationIdentifierAscii = Encoding.GetEncoding(1252).GetString(rawBytes, 8, 260).Split('\0').First();
            ApplicationIdentifierUnicode = Encoding.Unicode.GetString(rawBytes, 268, 520).Split('\0').First();
        }

        public string ApplicationIdentifierAscii { get; }
        public string ApplicationIdentifierUnicode { get; }

        public override string ToString()
        {
            return $"Darwin data block" +
                   $"\r\nApplication ID: {ApplicationIdentifierAscii}";
        }
    }
}