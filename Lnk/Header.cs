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

        [Description("The LNK does not contain a distributed link tracking data block")] ForceNoLinkTrack =
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

    private readonly Guid _goodSignature = new Guid("{00021401-0000-0000-c000-000000000046}");

    public Header(byte[] rawBytes)
    {
        var sigBytes = new byte[16];
        Buffer.BlockCopy(rawBytes, 4, sigBytes, 0, 16);
        Signature = new Guid(sigBytes);

        if (Signature != _goodSignature)
        {
            throw new Exception("Invalid Signature!");
        }

        DataFlags = (DataFlag) BitConverter.ToInt32(rawBytes, 20);
        FileAttributes = (FileAttribute) BitConverter.ToInt32(rawBytes, 24);

        try
        {
            TargetCreationDate = DateTimeOffset.FromFileTime(BitConverter.ToInt64(rawBytes, 28)).ToUniversalTime();
            TargetLastAccessedDate = DateTimeOffset.FromFileTime(BitConverter.ToInt64(rawBytes, 36)).ToUniversalTime();
            TargetModificationDate = DateTimeOffset.FromFileTime(BitConverter.ToInt64(rawBytes, 44)).ToUniversalTime();
        }
        catch (ArgumentOutOfRangeException)
        {
            // Second chance
            TargetCreationDate = DateTimeOffset.FromFileTime(ConvertLongToDateTime(BitConverter.ToInt64(rawBytes, 28)).ToFileTime());
            TargetLastAccessedDate = DateTimeOffset.FromFileTime(ConvertLongToDateTime(BitConverter.ToInt64(rawBytes, 36)).ToFileTime());
            TargetModificationDate = DateTimeOffset.FromFileTime(ConvertLongToDateTime(BitConverter.ToInt64(rawBytes, 44)).ToFileTime());
        }

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

    public DateTimeOffset TargetCreationDate { get; }
    public DateTimeOffset TargetModificationDate { get; }
    public DateTimeOffset TargetLastAccessedDate { get; }

    public uint FileSize { get; }
    public int IconIndex { get; }

    public string HotKey { get; }
    public ShowWindowOption ShowWindow { get; }
    public short Reserved0 { get; }
    public int Reserved1 { get; }
    public int Reserved2 { get; }

    public override string ToString()
    {
        return
            $"C:{TargetCreationDate} M:{TargetModificationDate} L:{TargetLastAccessedDate}, DataFlags: {DataFlags}, FileAttr:{FileAttributes}, Size: {FileSize:N0}, IconIndex: {IconIndex}, Hotkey: {HotKey}, ShowWindow: {ShowWindow}, Res0: {Reserved0}, Res1: {Reserved1}, Res2: {Reserved2}";
    }

    private string GetHotkey(byte low, byte high)
    {
        var hk = string.Empty;

        if (low == 0 && high == 0)
        {
            return hk;
        }

        int highNum = high;

        switch (highNum)
        {
            case 1:
                hk = "SHIFT+";
                break;
            case 2:
                hk = "CONTROL+";
                break;
            case 4:
                hk = "ALT+";
                break;
            case 6:
                hk = "CONTROL+ALT+";
                break;
        }

        int lowNum = low;


        if (0x30 <= lowNum && lowNum <= 0x39 || 0x41 <= lowNum && lowNum <= 0x5a)
        {
            hk += Convert.ToChar(lowNum);
            return hk;
        }

        if (0x70 <= lowNum && lowNum <= 0x87)
        {
            var keyVal = lowNum - 111;
            hk += $"F{keyVal}";
            return hk;
        }

        if (lowNum == 0x90)
        {
            hk += "NUMLOCK";
        }

        if (lowNum == 0x91)
        {
            hk += "SCROLLLOCK";
        }

        return hk;
    }

    private DateTime ConvertLongToDateTime(long value)
    {
        // Minimum and maximum ticks for DateTime
        const long minTicks = 0;
        const long maxTicks = 3155378975999999999;

        // Check if the value is within the range of DateTime
        if (value < minTicks || value > maxTicks)
        {
            // If the value is out of range, return the minimum or maximum DateTime value
            return value < minTicks ? DateTime.MinValue : DateTime.MaxValue;
        }
        else
        {
            // If the value is within range, create a DateTime object with the given ticks
            return new DateTime(value);
        }
    }

}
