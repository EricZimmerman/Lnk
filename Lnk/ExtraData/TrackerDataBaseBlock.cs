using System;
using System.Linq;
using System.Text;

namespace Lnk.ExtraData
{
    public class TrackerDataBaseBlock : ExtraDataBase
    {

        public TrackerDataBaseBlock(byte[] rawBytes)
        {
            Size = BitConverter.ToUInt32(rawBytes, 0);

            Signature = ExtraDataTypes.TrackerDataBlock;
            Version = BitConverter.ToInt32(rawBytes, 8);
            MachineId = Encoding.GetEncoding(1252).GetString(rawBytes, 16, 16).Split('\0').First();

            var guidRaw0 = new byte[16];
            var guidRaw1 = new byte[16];
            var guidRaw2 = new byte[16];
            var guidRaw3 = new byte[16];
            Buffer.BlockCopy(rawBytes, 0x20, guidRaw0, 0, 16);
            Buffer.BlockCopy(rawBytes, 0x30, guidRaw1, 0, 16);
            Buffer.BlockCopy(rawBytes, 0x40, guidRaw2, 0, 16);
            Buffer.BlockCopy(rawBytes, 0x50, guidRaw3, 0, 16);

            VolumeDroid = new Guid(guidRaw0);
            FileDroid = new Guid(guidRaw1);
            VolumeDroidBirth = new Guid(guidRaw2);
            FileDroidBirth = new Guid(guidRaw3);
        }

        public int Version { get; }
        public string MachineId { get; }
        public Guid VolumeDroid { get; }
        public Guid FileDroid { get; }
        public Guid VolumeDroidBirth { get; }
        public Guid FileDroidBirth { get; }

        public override string ToString()
        {
            return
                $"Sig: {Signature}, Ver: {Version}, MachineId: {MachineId}, Vol Droid: {VolumeDroid}, File droid: {FileDroid}, Vol Droid Birth: {VolumeDroidBirth}, File Droid birth: {FileDroidBirth}";
        }
    }
}