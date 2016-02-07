using System;
using System.ComponentModel;

namespace Lnk
{
    public class Header
    {
        [Flags]
        public enum DataFlag
        {
            [Description("The LNK file contains a link target identifier")] HasTargetIDList = 0x00000001,
            [Description("The LNK file contains location information")] HasLinkInfo = 0x00000002,
            [Description("The LNK file contains a [Description data string")] HasName = 0x00000004,
            [Description("The LNK file contains a relative path data string")] HasRelativePath = 0x00000008,
            [Description("The LNK file contains a working directory data string")] HasWorkingDir = 0x00000010,
            [Description("The LNK file contains a command line arguments data string")] HasArguments = 0x00000020,
            [Description("The LNK file contains a custom icon location")] HasIconLocation = 0x00000040,

            [Description(
                "The data strings in the LNK file are stored in Unicode (UTF-16 little-endian) instead of ASCII")] IsUnicode = 0x00000080,
            [Description("The location information is ignored")] ForceNoLinkInfo = 0x00000100,
            [Description("The LNK file contains environment variables location data block")] HasExpString = 0x00000200,

            [Description("A 16-bit target application is run in a separate virtual machine.")] RunInSeparateProcess =
                0x00000400,
            Reserved0 = 0x00000800,
            [Description("The LNK file contains a Darwin (Mac OS-X) properties data block")] HasDarwinID = 0x00001000,
            [Description("The target application is run as a different user.")] RunAsUser = 0x00002000,
            [Description("The LNK file contains an icon location data block")] HasExpIcon = 0x00004000,

            [Description(
                "The file system location is represented in the shell namespace when the path to an item is parsed into the link target identifiers"
                )] NoPidlAlias0x00008000,
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
            KeepLocalIDListForUNCTarget = 0x04000000
        }

        public enum FileAttribute
        {
            [Description("Is read-only")] FILE_ATTRIBUTE_READONLY = 0x00000001,
            [Description("Is hidden")] FILE_ATTRIBUTE_HIDDEN = 0x00000002,
            [Description("Is a system file or directory")] FILE_ATTRIBUTE_SYSTEM = 0x00000004,
            [Description("Is a volume label/Reserved, not used by the LNK format")] RES_VOLUME_LABEL = 0x00000008,
            [Description("Is a directory")] FILE_ATTRIBUTE_DIRECTORY = 0x00000010,
            [Description("Should be archived")] FILE_ATTRIBUTE_ARCHIVE = 0x00000020,
            [Description("Is a device/Reserved, not used by the LNK format")] FILE_ATTRIBUTE_DEVICE = 0x00000040,
            [Description("Is normal (None of the other flags should be set)")] FILE_ATTRIBUTE_NORMAL = 0x00000080,
            [Description("Is temporary")] FILE_ATTRIBUTE_TEMPORARY = 0x00000100,
            [Description("Is a sparse file")] FILE_ATTRIBUTE_SPARSE_FILE = 0x00000200,
            [Description("Is a reparse point or symbolic link")] FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400,
            [Description("Is compressed")] FILE_ATTRIBUTE_COMPRESSED = 0x00000800,

            [Description("Is offline. The data of the file is stored on an offline storage.")] FILE_ATTRIBUTE_OFFLINE =
                0x00001000,

            [Description(
                "Do not index content. The content of the file or directory should not be indexed by the indexing service."
                )] FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000,
            [Description("Is encrypted")] FILE_ATTRIBUTE_ENCRYPTED = 0x00004000,
            [Description("Unknown (seen on Windows 95 FAT)")] UNK_WIN95_FAT = 0x00008000,
            [Description("Is virtual/Currently reserved for future use, not used by the LNK format")] FILE_ATTRIBUTE_VIRTUAL = 0x00010000
        }

        public enum ShowWindowOption
        {
            [Description("Hides the window and activates another window.")] SW_HIDE = 0,

            [Description(
                "Activates and displays the window. The window is restored to its original size and position if the window is minimized or maximized."
                )] SW_NORMAL = 1,
            [Description("Activates and minimizes the window.")] SW_SHOWMINIMIZED = 2,
            [Description("Activates and maximizes the window.")] SW_MAXIMIZE = 3,
            [Description("Display the window in its most recent position and size without activating it.")] SW_SHOWNOACTIVATE = 4,
            [Description("Activates the window and displays it in its current size and position.")] SW_SHOW = 5,
            [Description("Minimizes the window and activates the next top-level windows (in order of depth (Z order))")] SW_MINIMIZE = 6,
            [Description("Display the window as minimized without activating it.")] SW_SHOWMINNOACTIVE = 7,
            [Description("Display the window in its current size and position without activating it.")] SW_SHOWNA = 8,

            [Description(
                "Activates and displays the window. The window is restored to its original size and position if the window is minimized or maximized."
                )] SW_RESTORE = 9,

            [Description(
                "Set the show state based on the ShowWindow values specified during the creation of the process.")] SW_SHOWDEFAULT = 10,
            [Description("Minimizes a window, even if the thread that owns the window is not responding.")] SW_FORCEMINIMIZE = 11,
            [Description("Undocumented according to wine project.")] SW_NORMALNA = 0xcc
        }

        public Header(byte[] rawBytes)
        {
            //TODO The LNK class identifier


            DataFlags = (DataFlag) BitConverter.ToInt32(rawBytes, 20);
            FileAttributes = (FileAttribute) BitConverter.ToInt32(rawBytes, 24);

            CreationDate = DateTimeOffset.FromFileTime(BitConverter.ToInt64(rawBytes, 28)).ToUniversalTime();
            ModificationDate = DateTimeOffset.FromFileTime(BitConverter.ToInt64(rawBytes, 36)).ToUniversalTime();
            LastAccessedDate = DateTimeOffset.FromFileTime(BitConverter.ToInt64(rawBytes, 44)).ToUniversalTime();

            FileSize = BitConverter.ToInt32(rawBytes, 52);
            IconIndex = BitConverter.ToInt32(rawBytes, 56);

            ShowWindow = (ShowWindowOption) BitConverter.ToInt32(rawBytes, 60);
            HotKey = GetHotkey(rawBytes[64], rawBytes[65]);

            Reserved0 = BitConverter.ToInt16(rawBytes, 66);
            Reserved1 = BitConverter.ToInt32(rawBytes, 68);
            Reserved1 = BitConverter.ToInt32(rawBytes, 72);
        }

        public DataFlag DataFlags { get; }
        public FileAttribute FileAttributes { get; }

        public DateTimeOffset CreationDate { get; }
        public DateTimeOffset ModificationDate { get; }
        public DateTimeOffset LastAccessedDate { get; }

        public int FileSize { get; }
        public int IconIndex { get; }

        public string HotKey { get; }
        public ShowWindowOption ShowWindow { get; }
        public short Reserved0 { get; }
        public int Reserved1 { get; }
        public int Reserved2 { get; }

        public override string ToString()
        {
            return
                $"C:{CreationDate} M:{ModificationDate} L:{LastAccessedDate}, DataFlags: {DataFlags}, FileAttr:{FileAttributes}, Size: {FileSize:N0}, IconIndex: {IconIndex}, Hotkey: {HotKey}, ShowWindow: {ShowWindow}, Res0: {Reserved0}, Res1: {Reserved1}, Res2: {Reserved2}";
        }

        private string GetHotkey(byte low, byte high)
        {
            //TODO set up test cases for all manner of shortcuts
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


            if ((0x30 <= lowNum && lowNum <= 0x39) || 0x41 <= lowNum && lowNum <= 0x5a)
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
    }
}