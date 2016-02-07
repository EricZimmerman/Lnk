using System.IO;

namespace Lnk
{
    public static class Lnk
    {
        public static LnkFile LoadFile(string lnkFile)
        {
            var raw = File.ReadAllBytes(lnkFile);

            return new LnkFile(raw, lnkFile);
        }
    }
}