using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Lnk.ExtraData
{
   public class TrackerDataBaseBlock:ExtraDataBase
   {
       public ExtraDataTypes Signature;
       public int Version { get; }
        public string MachineId { get; }
        public List<Guid> Droid { get; }
        public List<Guid> DroidBirth { get; }

       public TrackerDataBaseBlock (byte[] rawBytes)
       {
            Signature = ExtraDataTypes.TrackerDataBlock;
               Version = BitConverter.ToInt32(rawBytes, 8);
           MachineId = Encoding.GetEncoding(1252).GetString(rawBytes, 16, 16).Split('\0').First();

           var guidRaw0 = new byte[16];
           var guidRaw1 = new byte[16];
           var guidRaw2 = new byte[16];
           var guidRaw3 = new byte[16];
            Buffer.BlockCopy(rawBytes,0x20,guidRaw0,0,16);
            Buffer.BlockCopy(rawBytes,0x30,guidRaw1,0,16);
            Buffer.BlockCopy(rawBytes,0x40,guidRaw2,0,16);
            Buffer.BlockCopy(rawBytes,0x50,guidRaw3,0,16);

            Droid = new List<Guid> { new Guid(guidRaw0), new Guid(guidRaw1) };
            DroidBirth = new List<Guid> { new Guid(guidRaw2), new Guid(guidRaw3) };
        }

       public override string ToString()
       {
           return $"Sig: {Signature}, Ver: {Version}, MachineId: {MachineId}, Droid: {string.Join(", ", Droid)}, DroidBirth: {string.Join(", ", DroidBirth)}";
       }
    }
}
