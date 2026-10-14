using System;
using System.ComponentModel;

namespace Lnk;

public class Header
{
    [Flags]
    public enum DataFlag
    {
        [Description("The LNK file contains a link target identifier")] HasTargetIdList = 0x00000001,
        [Description("The LNK file contains location information")] HasLinkInfo = 0x00000002,
        [Description("The LNK file contains a Description data string")] HasName = 0x00000004,
        [Description("The LNK file contains a relative path data string")] HasRelativePath = 0x00000008,
        [Description("The LNK file contains a working directory data string")] HasWorkingDir = 0x00000010,
        [Description("The LNK file contains a command line arguments data string")] HasArguments = 0x00000020,
        [Description("The LNK file contains a custom icon location")] HasIconLocation = 0x00000040,

        [Description(
            "The data strings in the LNK file are stored in Unicode (UTF-16 little-endian) instead of ASCII")]
        IsUnicode = 0x00000080,
        [Description("The location information is ignored")] ForceNoLinkInfo = 0x00000100,
        [Description("The LNK file contains environment variables location data block")] HasExpString = 0x00000200,

        [Description("A 16-bit target application is run in a separate virtual machine.")] RunInSeparateProcess =
            0x00000400,
        Reserved0 = 0x00000800,
        [Description("The LNK file contains a Darwin (Mac OS-X) properties data block")] HasDarwinId = 0x00001000,
        [Description("The target application is run as a different user.")] RunAsUser = 0x00002000,
        [Description("The LNK file contains an icon location data block")] HasExpIcon = 0x00004000,

        [Description(
            "The file system location is represented in the shell namespace when the path to an item is parsed into the link target identifiers"
        )] NoPidlAlias = 0x00008000,
        Reserved1 = 0x00010000,

        [Description(
            "The target application is run with the shim layer. The LNK file contains shim layer properties data block."
        )] RunWithShimLayer = 0x00020000,

        [Description("The TrackerDataBlock is ignored")] ForceNoLinkTrack =
            0x00040000,

        [Description("The LNK file contains a metadata property store data block")] EnableTargetMetadata =
            0x00080000,

        [Description("The environment variables location block should be ignored")] DisableLinkPathTracking =
            0x00100000,
        DisableKnownFolderTracking = 0x00200000,
        DisableKnownFolderAlias = 0x00400000,
        AllowLinkToLink = 0x00800000,
        UnaliasOnSave = 0x01000000,
        PreferEnvironmentPath = 0x02000000,
        KeepLocalIdListForUncTarget = 0x04000000
    }

    [Flags]
    public enum FileAttribute
    {
        [Description("Is read-only")] FileAttributeReadonly = 0x00000001,
        [Description("Is hidden")] FileAttributeHidden = 0x00000002,
        [Description("Is a system file or directory")] FileAttributeSystem = 0x00000004,
        [Description("Is a volume label/Reserved, not used by the LNK format")] ResVolumeLabel = 0x00000008,
        [Description("Is a directory")] FileAttributeDirectory = 0x00000010,
        [Description("Should be archived")] FileAttributeArchive = 0x00000020,
        [Description("Is a device/Reserved, not used by the LNK format")] FileAttributeDevice = 0x00000040,
        [Description("Is normal (None of the other flags should be set)")] FileAttributeNormal = 0x00000080,
        [Description("Is temporary")] FileAttributeTemporary = 0x00000100,
        [Description("Is a sparse file")] FileAttributeSparseFile = 0x00000200,
        [Description("Is a reparse point or symbolic link")] FileAttributeReparsePoint = 0x00000400,
        [Description("Is compressed")] FileAttributeCompressed = 0x00000800,

        [Description("Is offline. The data of the file is stored on an offline storage.")] FileAttributeOffline =
            0x00001000,

        [Description(
            "Do not index content. The content of the file or directory should not be indexed by the indexing service."
        )] FileAttributeNotContentIndexed = 0x00002000,
        [Description("Is encrypted")] FileAttributeEncrypted = 0x00004000,
        [Description("Unknown (seen on Windows 95 FAT)")] UnkWin95Fat = 0x00008000,

        [Description("Is virtual/Currently reserved for future use, not used by the LNK format")]
        FileAttributeVirtual = 0x00010000
    }

    public enum ShowWindowOption
    {
        [Description("Hides the window and activates another window.")] SwHide = 0,

        [Description(
            "Activates and displays the window. The window is restored to its original size and position if the window is minimized or maximized."
        )] SwNormal = 1,
        [Description("Activates and minimizes the window.")] SwShowminimized = 2,
        [Description("Activates and maximizes the window.")] SwMaximize = 3,

        [Description("Display the window in its most recent position and size without activating it.")]
        SwShownoactivate = 4,
        [Description("Activates the window and displays it in its current size and position.")] SwShow = 5,

        [Description("Minimizes the window and activates the next top-level windows (in order of depth (Z order))")]
        SwMinimize = 6,
        [Description("Display the window as minimized without activating it.")] SwShowminnoactive = 7,
        [Description("Display the window in its current size and position without activating it.")] SwShowna = 8,

        [Description(
            "Activates and displays the window. The window is restored to its original size and position if the window is minimized or maximized."
        )] SwRestore = 9,

        [Description(
            "Set the show state based on the ShowWindow values specified during the creation of the process.")]
        SwShowdefault = 10,

        [Description("Minimizes a window, even if the thread that owns the window is not responding.")]
        SwForceminimize = 11,
        [Description("Undocumented according to wine project.")] SwNormalna = 0xcc
    }

    public const int HeaderSize = 0x4C;

    private static readonly Guid GoodSignature = new("{00021401-0000-0000-c000-000000000046}");

    /// <summary>
    ///     Parses the 76 byte ShellLinkHeader (MS-SHLLINK 2.1). <paramref name="rawBytes" /> may be the whole file;
    ///     only the first 76 bytes are read.
    /// </summary>
    public Header(byte[] rawBytes)
    {
        if (rawBytes == null || rawBytes.Length < HeaderSize)
        {
            throw new LnkParseException($"Header is smaller than {HeaderSize} bytes - is this file corrupt?", 0, "ShellLinkHeader");
        }

        var headerSize = BitConverter.ToUInt32(rawBytes, 0);
        Signature = Helpers.ReadGuid(rawBytes, 4, "LinkCLSID");

        if (headerSize != HeaderSize || Signature != GoodSignature)
        {
            throw new LnkParseException("Invalid Signature!", 0, "HeaderSize/LinkCLSID");
        }

        DataFlags = (DataFlag) BitConverter.ToInt32(rawBytes, 20);
        FileAttributes = (FileAttribute) BitConverter.ToInt32(rawBytes, 24);

        // Each FILETIME is converted on its own so one corrupt value cannot affect the others.
        TargetCreationDateRaw = BitConverter.ToInt64(rawBytes, 28);
        TargetLastAccessedDateRaw = BitConverter.ToInt64(rawBytes, 36);
        TargetModificationDateRaw = BitConverter.ToInt64(rawBytes, 44);

        TargetCreationDate = Helpers.FromFileTime(TargetCreationDateRaw) ?? DateTimeOffset.MinValue;
        TargetLastAccessedDate = Helpers.FromFileTime(TargetLastAccessedDateRaw) ?? DateTimeOffset.MinValue;
        TargetModificationDate = Helpers.FromFileTime(TargetModificationDateRaw) ?? DateTimeOffset.MinValue;

        FileSize = BitConverter.ToUInt32(rawBytes, 52);
        IconIndex = BitConverter.ToInt32(rawBytes, 56);

        ShowWindow = (ShowWindowOption) BitConverter.ToInt32(rawBytes, 60);
        HotKey = GetHotkey(rawBytes[64], rawBytes[65]);

        Reserved0 = BitConverter.ToInt16(rawBytes, 66);
        Reserved1 = BitConverter.ToInt32(rawBytes, 68);
        Reserved2 = BitConverter.ToInt32(rawBytes, 72);
    }

    public Guid Signature { get; }

    public DataFlag DataFlags { get; }
    public FileAttribute FileAttributes { get; }

    /// <summary>
    ///     Target creation time (UTC). <see cref="DateTimeOffset.MinValue" /> if the stored FILETIME is out of range;
    ///     see <see cref="TargetCreationDateRaw" /> for the stored value.
    /// </summary>
    public DateTimeOffset TargetCreationDate { get; }

    /// <summary>
    ///     Target last write (modification) time (UTC).
    /// </summary>
    public DateTimeOffset TargetModificationDate { get; }

    /// <summary>
    ///     Target last access time (UTC).
    /// </summary>
    public DateTimeOffset TargetLastAccessedDate { get; }

    public long TargetCreationDateRaw { get; }
    public long TargetModificationDateRaw { get; }
    public long TargetLastAccessedDateRaw { get; }

    public uint FileSize { get; }
    public int IconIndex { get; }

    public string HotKey { get; }

    /// <summary>
    ///     Raw ShowCommand value. Per MS-SHLLINK only SW_SHOWNORMAL, SW_SHOWMAXIMIZED and SW_SHOWMINNOACTIVE are
    ///     meaningful; Windows treats every other value as SW_SHOWNORMAL.
    /// </summary>
    public ShowWindowOption ShowWindow { get; }
    public short Reserved0 { get; }
    public int Reserved1 { get; }
    public int Reserved2 { get; }

    public override string ToString()
    {
        return
            $"C:{TargetCreationDate} M:{TargetModificationDate} L:{TargetLastAccessedDate}, DataFlags: {DataFlags}, FileAttr:{FileAttributes}, Size: {FileSize:N0}, IconIndex: {IconIndex}, Hotkey: {HotKey}, ShowWindow: {ShowWindow}, Res0: {Reserved0}, Res1: {Reserved1}, Res2: {Reserved2}";
    }

    // HotKeyFlags (MS-SHLLINK 2.1.3): low byte is the virtual key, high byte a bitmask of
    // HOTKEYF_SHIFT (1), HOTKEYF_CONTROL (2) and HOTKEYF_ALT (4); modifiers may be combined.
    private static string GetHotkey(byte low, byte high)
    {
        if (low == 0 && high == 0)
        {
            return string.Empty;
        }

        var hk = string.Empty;

        if ((high & 0x01) != 0)
        {
            hk += "SHIFT+";
        }

        if ((high & 0x02) != 0)
        {
            hk += "CONTROL+";
        }

        if ((high & 0x04) != 0)
        {
            hk += "ALT+";
        }

        if (low is >= 0x30 and <= 0x39 or >= 0x41 and <= 0x5a)
        {
            return hk + Convert.ToChar(low);
        }

        if (low is >= 0x70 and <= 0x87)
        {
            return hk + $"F{low - 0x6F}";
        }

        switch (low)
        {
            case 0x90:
                return hk + "NUMLOCK";
            case 0x91:
                return hk + "SCROLLLOCK";
            case 0:
                return hk.TrimEnd('+');
            default:
                return hk + $"0x{low:X2}";
        }
    }
}
