using System;

namespace Lnk.ExtraData;

public class DamagedDataBlock : ExtraDataBase
{
    public DamagedDataBlock(byte[] rawBytes, string errMsg)
    {
        Signature = ExtraDataTypes.DamagedDataBlock;
            
        Size = (uint)rawBytes.Length;
        ErrorMessage = errMsg;

        OriginalData = rawBytes;

        // The signature lives at offset 4; blocks shorter than 8 bytes do not have one.
        if (rawBytes.Length >= 8)
        {
            OriginalSignature = (ExtraDataTypes)BitConverter.ToUInt32(rawBytes, 4);
        }
    }

    public ExtraDataTypes OriginalSignature { get;  }
    public byte[] OriginalData { get; }
    public string ErrorMessage { get; }

    public override string ToString()
    {
        return
            $"Damaged data block" +
            $"\r\nOriginal Signature: {OriginalSignature}" +
            $"\r\nErrorMessage: {ErrorMessage}" ;
    }
}