using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lnk.ExtraData
{
public    class VistaAndAboveIDListDataBlock:ExtraDataBase
    {

    public VistaAndAboveIDListDataBlock(byte[] rawBytes)
    {
            Signature = ExtraDataTypes.VistaAndAboveIDListDataBlock;

            Size = BitConverter.ToUInt32(rawBytes, 0);


        var index = 8;
            //process shell items
            var shellItemSize = BitConverter.ToInt16(rawBytes, index);
            index += 2;

            var shellItemBytes = new byte[shellItemSize];
            Buffer.BlockCopy(rawBytes, index, shellItemBytes, 0, shellItemSize);

            //TODO process shell items



        }
        public override string ToString()
        {
            return $"Size: {Size}, SpecialFolderID: {"FINISH ME"}";
        }

    }
}
