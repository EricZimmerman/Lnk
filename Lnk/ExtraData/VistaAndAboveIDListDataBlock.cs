using System;
using System.Collections.Generic;
using System.Text;
using Lnk.ShellItems;

namespace Lnk.ExtraData;

public class VistaAndAboveIdListDataBlock : ExtraDataBase
{
    public VistaAndAboveIdListDataBlock(byte[] rawBytes, int codepage=1252)
    {
        RequireSize(rawBytes, 0x0A, "VistaAndAboveIDListDataBlock");

        Signature = ExtraDataTypes.VistaAndAboveIdListDataBlock;

        Size = BitConverter.ToUInt32(rawBytes, 0);

        // The IDList starts directly at offset 8 (there is no IDListSize field in this block) and runs to the
        // end of the block, terminated by a 2 byte TerminalID.
        ParseErrors = new List<string>();
        TargetIDs = ShellItemFactory.ParseIdList(rawBytes, 8, rawBytes.Length, codepage, ParseErrors);
    }

    /// <summary>
    ///     Shell items in this block that could not be parsed (they appear in <see cref="TargetIDs" /> as
    ///     <see cref="ShellBagDamaged" />).
    /// </summary>
    public List<string> ParseErrors { get; }

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