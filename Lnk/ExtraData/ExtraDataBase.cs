namespace Lnk.ExtraData;

public enum ExtraDataTypes : uint
{
    DamagedDataBlock = 0x0,
    ConsoleDataBlock = 0xA0000002,
    ConsoleFeDataBlock = 0xA0000004,
    DarwinDataBlock = 0xA0000006,
    EnvironmentVariableDataBlock = 0xA0000001,
    IconEnvironmentDataBlock = 0xA0000007,
    KnownFolderDataBlock = 0xA000000B,
    PropertyStoreDataBlock = 0xA0000009,
    ShimDataBlock = 0xA0000008,
    SpecialFolderDataBlock = 0xA0000005,
    TrackerDataBlock = 0xA0000003,
    VistaAndAboveIdListDataBlock = 0xA000000C
}

public abstract class ExtraDataBase
{
    protected ExtraDataTypes Signature { get; set; }
    protected uint Size { get; set; }

    /// <summary>
    ///     Throws <see cref="LnkParseException" /> when a block is smaller than the size MS-SHLLINK requires for it.
    /// </summary>
    protected static void RequireSize(byte[] rawBytes, int minimumSize, string blockName)
    {
        if (rawBytes == null || rawBytes.Length < minimumSize)
        {
            throw new LnkParseException(
                $"{blockName} must be at least 0x{minimumSize:X} bytes but is 0x{rawBytes?.Length ?? 0:X} - is this file corrupt?",
                0, blockName);
        }
    }
}