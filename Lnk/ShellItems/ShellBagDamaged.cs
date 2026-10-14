using System.Collections.Generic;
using ExtensionBlocks;

namespace Lnk.ShellItems;

/// <summary>
///     Placeholder for a shell item that could not be parsed (unknown type or malformed data).
///     Parsing continues with the next item so the rest of the LNK file is still available.
/// </summary>
public class ShellBagDamaged : ShellBag
{
    public ShellBagDamaged(byte[] rawBytes, string errorMessage)
    {
        ExtensionBlocks = new List<IExtensionBlock>();
        RawBytes = rawBytes;
        ErrorMessage = errorMessage;
        ItemType = rawBytes != null && rawBytes.Length > 2 ? rawBytes[2] : (byte)0;
        FriendlyName = "Damaged or unsupported shell item";
        Value = $"Type 0x{ItemType:X2}, {rawBytes?.Length ?? 0} bytes: {errorMessage}";
    }

    public byte ItemType { get; }
    public byte[] RawBytes { get; }
    public string ErrorMessage { get; }
}
