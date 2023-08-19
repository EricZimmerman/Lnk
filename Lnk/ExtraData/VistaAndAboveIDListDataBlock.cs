using System;
using System.Collections.Generic;
using System.Text;
using Lnk.ShellItems;

namespace Lnk.ExtraData;

public class VistaAndAboveIdListDataBlock : ExtraDataBase
{
    public VistaAndAboveIdListDataBlock(byte[] rawBytes, int codepage=1252)
    {
        Signature = ExtraDataTypes.VistaAndAboveIdListDataBlock;

        Size = BitConverter.ToUInt32(rawBytes, 0);

        var index = 8;
        //process shell items
        var shellItemSize = BitConverter.ToInt16(rawBytes, index);

        var shellItemBytes = new byte[shellItemSize];
        Buffer.BlockCopy(rawBytes, index, shellItemBytes, 0, shellItemSize);

        var shellItemsRaw = new List<byte[]>();
        var shellItemIndex = 0;

        while (shellItemIndex < shellItemBytes.Length)
        {
            var shellSize = BitConverter.ToUInt16(shellItemBytes, shellItemIndex);

            if (shellSize == 0)
            {
                break;
            }
            var itemBytes = new byte[shellSize];
            Buffer.BlockCopy(shellItemBytes, shellItemIndex, itemBytes, 0, shellSize);

            shellItemsRaw.Add(itemBytes);
            shellItemIndex += shellSize;
        }


        TargetIDs = new List<ShellBag>();

        foreach (var bytese in shellItemsRaw)
        {
            switch (bytese[2])
            {
                case 0x1f:
                    var f = new ShellBag0X1F(bytese, codepage);
                    TargetIDs.Add(f);
                    break;

                case 0x2f:
                    var ff = new ShellBag0X2F(bytese, codepage);
                    TargetIDs.Add(ff);
                    break;
                case 0x2e:
                    var ee = new ShellBag0X2E(bytese);
                    TargetIDs.Add(ee);
                    break;
                case 0xb1:
                case 0x31:
                case 0x35:
                    var d = new ShellBag0X31(bytese, codepage);
                    TargetIDs.Add(d);
                    break;
                case 0x32:
                    var d2 = new ShellBag0X32(bytese, codepage);
                    TargetIDs.Add(d2);
                    break;
                case 0x00:
                    var v0 = new ShellBag0X00(bytese);
                    TargetIDs.Add(v0);
                    break;
                case 0x01:
                    var one = new ShellBag0X01(bytese);
                    TargetIDs.Add(one);
                    break;
                case 0x71:
                    var sevenone = new ShellBag0X71(bytese);
                    TargetIDs.Add(sevenone);
                    break;
                case 0x61:
                    var sixone = new ShellBag0X61(bytese, codepage);
                    TargetIDs.Add(sixone);
                    break;

                case 0xC3:
                    var c3 = new ShellBag0Xc3(bytese, codepage);
                    TargetIDs.Add(c3);
                    break;

                case 0x74:
                case 0x77:
                    var sev = new ShellBag0X74(bytese, codepage);
                    TargetIDs.Add(sev);
                    break;

                case 0x41:
                case 0x42:
                case 0x43:
                case 0x46:
                case 0x47:
                    var forty = new ShellBag0X40(bytese, codepage);
                    TargetIDs.Add(forty);
                    break;
                default:
                    throw new Exception($"Unknown item ID: 0x{bytese[2]:X}");
            }
        }
    }

    public List<ShellBag> TargetIDs { get; }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine("Vista and above ID List data block");

        foreach (var shellBag in TargetIDs)
        {
            sb.AppendLine(shellBag.ToString());
        }


        return sb.ToString();
    }
}