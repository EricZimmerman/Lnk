using System;

namespace Lnk.ExtraData
{
    public class ConsoleFEDataBlock : ExtraDataBase
    {
        public ConsoleFEDataBlock(byte[] rawBytes)
        {
            Signature = ExtraDataTypes.ConsoleFEDataBlock;

            Size = BitConverter.ToUInt32(rawBytes, 0);

            CodePage = BitConverter.ToUInt32(rawBytes, 8);
        }

        public uint CodePage { get; }

        public override string ToString()
        {
            return $"Console FE data block" +
                   $"\r\nCodePage: {CodePage}";            
        }
    }
}