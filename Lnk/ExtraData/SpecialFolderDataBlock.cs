﻿using System;

namespace Lnk.ExtraData
{
    public class SpecialFolderDataBlock : ExtraDataBase
    {
        public SpecialFolderDataBlock(byte[] rawBytes)
        {
            Signature = ExtraDataTypes.SpecialFolderDataBlock;

            Size = BitConverter.ToUInt32(rawBytes, 0);

            SpecialFolderID = BitConverter.ToUInt32(rawBytes, 8);
            Offset = BitConverter.ToUInt32(rawBytes, 12);
        }

        public uint SpecialFolderID { get; }
        public uint Offset { get; }

        public override string ToString()
        {
            return $"Special folder data block" +
                   $"\r\nSpecialFolderID: {SpecialFolderID}";
        }
    }
}