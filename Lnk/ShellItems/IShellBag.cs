using System;
using System.Collections.Generic;
using ExtensionBlocks;

namespace Lnk.ShellItems
{
    public interface IShellBag
    {
        /// <summary>
        ///     ID used for uniqueness. can be used to find a shellbag among a collection of shellbags
        /// </summary>
        string InternalId { get; }

        /// <summary>
        ///     A nice looking name vs the technical representation of the ShellBag item
        /// </summary>
        string FriendlyName { get; }

        /// <summary>
        ///     ShellBag data in its unparsed format as a string of hex characters separated by -
        /// </summary>
        byte[] HexValue { get; }

        /// <summary>
        ///     BagPath is the *root* path to the ShellBag.
        /// </summary>
        string BagPath { get; }

        /// <summary>
        ///     AbsolutePath is the path to the ShellBag.
        /// </summary>
        string AbsolutePath { get; set; }

        /// <summary>
        ///     Slot is the value name in BagPath
        /// </summary>
        int Slot { get; }

        /// <summary>
        /// True if the ShellBag is from a deleted Registry key
        /// </summary>
        bool IsDeleted { get; set; }
        /// <summary>
        ///     The position this ShellBag item was opened
        /// </summary>
        int MruPosition { get; }

        /// <summary>
        /// Gets the node slot.
        /// </summary>
        /// <value>The node slot.</value>
        int NodeSlot { get; set; }

        /// <summary>
        ///     Child ShellBag items for this ShellBag
        /// </summary>
        List<IShellBag> ChildShellBags { get; set; }

        /// <summary>
        ///     The name of the ShellBag. Can be based on file name, directory name, or GUID
        /// </summary>
        string Value { get; }

        /// <summary>
        ///     last write time of BagPath key
        /// </summary>
        DateTimeOffset? LastWriteTime { get; set; }

        /// <summary>
        ///     First explored time
        /// </summary>
        DateTimeOffset? FirstExplored { get; set; }

        /// <summary>
        ///     First explored time
        /// </summary>
        DateTimeOffset? LastExplored { get; set; }


        List<IExtensionBlock> ExtensionBlocks { get; set; }
    }
}