using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtensionBlocks;

namespace Lnk.ShellItems
{
    internal class ShellBag0X2F : ShellBag
    {
        public ShellBag0X2F(int slot, int mruPosition, byte[] rawBytes, string bagPath)
        {
            Slot = slot;
            MruPosition = mruPosition;

            FriendlyName = "Drive letter";

            ChildShellBags = new List<IShellBag>();

            InternalId = Guid.NewGuid().ToString();

            HexValue = rawBytes;

            ExtensionBlocks = new List<IExtensionBlock>();

            BagPath = bagPath;

            var driveLetter = Encoding.GetEncoding(1252).GetString(rawBytes, 3, 2);

            Value = driveLetter;

            if (rawBytes.Length > 0x30)
            {
                var index = 0x19;

                var signature1 = BitConverter.ToUInt32(rawBytes, index + 4);

                //Debug.WriteLine(" 0x1f bag sig: " + signature1.ToString("X8"));

                var block1 = Utils.GetExtensionBlockFromBytes(signature1, rawBytes.Skip(index).ToArray());

                ExtensionBlocks.Add(block1);
            }

//                if (bagPath.Contains(@"BagMRU\2") && slot == 0x11)
//                {
//                    Debug.WriteLine("At trap for certain bag in 0x2f bag");
//                }

            //    SiAuto.Main.LogMessage("Got drive letter: {0}", Value);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(base.ToString());

            return sb.ToString();
        }
    }
}