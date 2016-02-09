using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lnk.ExtraData
{
    public enum ExtraDataTypes:uint
    {
        ConsoleDataBlock = 0xA0000002,
        ConsoleFEDataBlock = 0xA0000004,
        DarwinDataBlock = 0xA0000006,
        EnvironmentVariableDataBlock = 0xA0000001,
        IconEnvironmentDataBlock = 0xA0000007,
        KnownFolderDataBlock = 0xA000000B,
        PropertyStoreDataBlock = 0xA0000009,
        ShimDataBlock = 0xA0000008,
        SpecialFolderDataBlock = 0xA0000005,
        TrackerDataBlock = 0xA0000003,
        VistaAndAboveIDListDataBlock = 0xA000000C,
    }

   public abstract class ExtraDataBase
   {
       private ExtraDataTypes Signature;
   }
}
