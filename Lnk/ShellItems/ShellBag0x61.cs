using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ExtensionBlocks;

namespace Lnk.ShellItems
{
    internal class ShellBag0X61 : ShellBag
    {
        // Fields...


        public ShellBag0X61(int slot, int mruPosition, byte[] rawBytes, string bagPath)
        {
            Slot = slot;
            MruPosition = mruPosition;

            FriendlyName = "URI";

            ChildShellBags = new List<IShellBag>();

            InternalId = Guid.NewGuid().ToString();

            //if (bagPath.Contains(@"BagMRU\1\1\2") && slot == 0)
            //{
            //    Debug.WriteLine("At trap for certain bag in 0x71 bag");
            //}

            HexValue = rawBytes;

            ExtensionBlocks = new List<IExtensionBlock>();

            BagPath = bagPath;

            var index = 2; //past size

            index += 1; // move past type and a single unknown value

            var flags = rawBytes[index];
            index += 1;

            var dataSize = BitConverter.ToUInt16(rawBytes, index);

            UserName = string.Empty;

            if (dataSize > 0)
            {
                index += 2; // past size
                index += 4; // skip unknown
                index += 4; // skip unknown

                var ft1 = DateTimeOffset.FromFileTime((long) BitConverter.ToUInt64(rawBytes, index));

                FileTime1 = ft1.ToUniversalTime();


                index += 8;
                index += 4; // skip unknown FF FF FF FF
                index += 12; // skip unknown 12 0's
                index += 4; // skip unknown

                var strSize = BitConverter.ToUInt32(rawBytes, index);
                index += 4;

                var str = Encoding.GetEncoding(1252).GetString(rawBytes, index, (int) strSize);

                Value = str.Replace("\0", "");

                index += (int) strSize;

                strSize = BitConverter.ToUInt32(rawBytes, index);
                index += 4;

                if (strSize > 0)
                {
                    str = Encoding.GetEncoding(1252).GetString(rawBytes, index, (int) strSize);

                    UserName = str.Replace("\0", "");
                    ;
                    index += (int) strSize;
                }

                strSize = BitConverter.ToUInt32(rawBytes, index);
                index += 4;


                if (strSize > 0)
                {
                    str = Encoding.GetEncoding(1252).GetString(rawBytes, index, (int) strSize);
                    index += (int) strSize;
                }

                var len1 = 0;

                while (rawBytes[index + len1] != 0x00)
                {
                    len1 += 1;
                }

                URI = Encoding.GetEncoding(1252).GetString(rawBytes, index, len1);


                index += len1 + 1;
            }

            dataSize = BitConverter.ToUInt16(rawBytes, index);
            index += 2;


            Trace.Assert(dataSize == 0, "extra data in ftp case");
        }

        //   public PropertyStore PropertyStore { get; private set; }

        public DateTimeOffset? FileTime1 { get; }

        public string URI { get; set; }


        public string UserName { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"URI: {URI}");
            sb.AppendLine();

            sb.AppendLine($"Connect time: {FileTime1}");
            sb.AppendLine();

            if (UserName.Length > 0)
            {
                sb.AppendLine($"Username: {UserName}");
                sb.AppendLine();
            }


            sb.AppendLine(base.ToString());

            return sb.ToString();
        }
    }
}