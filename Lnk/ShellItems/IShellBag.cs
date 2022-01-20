using System.Collections.Generic;
using ExtensionBlocks;

namespace Lnk.ShellItems;

public interface IShellBag
{
    /// <summary>
    ///     A nice looking name vs the technical representation of the ShellBag item
    /// </summary>
    string FriendlyName { get; }


    /// <summary>
    ///     The name of the ShellBag. Can be based on file name, directory name, or GUID
    /// </summary>
    string Value { get; }


    List<IExtensionBlock> ExtensionBlocks { get; set; }
}