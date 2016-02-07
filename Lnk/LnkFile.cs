using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Lnk
{
    public class LnkFile
    {
        public string SourceFile { get; }
        public Header Header { get; }

        public string Name { get; }
        public string RelativePath { get; }
        public string WorkingDirectory { get; }
        public string Arguments { get; }
        public string IconLocation { get; }

        public enum LocationFlag
        {
            [Description("The linked file is on a volume")]
            VolumeIDAndLocalBasePath = 0x0001,

            [Description("The linked file is on a network share")]
            CommonNetworkRelativeLinkAndPathSuffix = 0x0002
        }

        public LocationFlag LocationFlags { get; }

        public LnkFile(byte[] rawBytes, string sourceFile)
        {
            SourceFile = sourceFile;
            var headerBytes = new byte[76];
            Buffer.BlockCopy(rawBytes, 0, headerBytes, 0, 76);

            Header = new Header(headerBytes);

            var index = 76;

            if ((Header.DataFlags & Header.DataFlag.HasTargetIDList) == Header.DataFlag.HasTargetIDList)
            {
                //process shell items
                var shellItemSize = BitConverter.ToInt16(rawBytes, index);
                index += 2;

                var shellItemBytes = new byte[shellItemSize];
                Buffer.BlockCopy(rawBytes, index, shellItemBytes, 0, shellItemSize);

                //TODO process shell items

                index += shellItemSize;
            }

            if ((Header.DataFlags & Header.DataFlag.HasLinkInfo) == Header.DataFlag.HasLinkInfo)
            {
                var locationItemSize = BitConverter.ToInt32(rawBytes, index);
                var locationBytes = new byte[locationItemSize];
                Buffer.BlockCopy(rawBytes, index, locationBytes, 0, locationItemSize);

                //TODO Process Location info

                LocationFlags = (LocationFlag) BitConverter.ToInt32(locationBytes, 8);

                var volOffset = BitConverter.ToInt32(locationBytes, 12);
                var localPathOffset = BitConverter.ToInt32(locationBytes, 16);
                var networkShareOffset = BitConverter.ToInt32(locationBytes, 20);

                if ((LocationFlags & LocationFlag.VolumeIDAndLocalBasePath) == LocationFlag.VolumeIDAndLocalBasePath)
                {
                    var localpath = Encoding.GetEncoding(1252)
                 .GetString(locationBytes, localPathOffset, locationBytes.Length - localPathOffset).Split('\0').First();
                }
                else if ((LocationFlags & LocationFlag.CommonNetworkRelativeLinkAndPathSuffix) == LocationFlag.CommonNetworkRelativeLinkAndPathSuffix)
                {
                    var networkShareSize = BitConverter.ToInt32(locationBytes, networkShareOffset);
                    var networkBytes = new byte[networkShareSize];
                    Buffer.BlockCopy(locationBytes,networkShareOffset,networkBytes,0,networkShareSize);

                    var nw = new NetworkShareInfo(networkBytes);
                }

                var commonPathOffset = BitConverter.ToInt32(locationBytes, 24);

                var commonPath = Encoding.GetEncoding(1252)
                    .GetString(locationBytes, commonPathOffset, locationBytes.Length - commonPathOffset).Split('\0').First();

                if (locationBytes.Length > 28)
                {
                    var uniLocalOffset = BitConverter.ToInt32(locationBytes, 28);
                    //TODO var unicodeLocalPath = Encoding.Unicode.GetString(locationBytes, uniLocalOffset,5);
                }

                if (locationBytes.Length > 32)
                {
                    var uniCommonOffset = BitConverter.ToInt32(locationBytes, 32);
                    //TODO var unicodeCommonPath = Encoding.Unicode.GetString(locationBytes, uniCommonOffset, 5);
                }

                //                The full filename can be determined by:
                //                combining the local path and the common path
                //                combining the network share name(in the network share information) with the common path

                //VolumeIDAndLocalBasePath
                //
                //The linked file is on a volume
                //If set the volume information and the local path contain data
                //
                //CommonNetworkRelativeLinkAndPathSuffix
                //
                //The linked file is on a network share
                //If set the network share information and common path contain data

                index += locationItemSize;
            }

            if ((Header.DataFlags & Header.DataFlag.HasName) == Header.DataFlag.HasName)
            {
                var nameLen = BitConverter.ToInt16(rawBytes, index);
                index += 2;
                if ((Header.DataFlags & Header.DataFlag.IsUnicode) == Header.DataFlag.IsUnicode)
                {
                    Name = Encoding.Unicode.GetString(rawBytes, index, nameLen * 2);
                    index += nameLen ;
                }
                else
                {
                    Name = Encoding.GetEncoding(1252).GetString(rawBytes, index, nameLen);
                }
                index += nameLen;
            }

            if ((Header.DataFlags & Header.DataFlag.HasRelativePath) == Header.DataFlag.HasRelativePath)
            {
                var relLen = BitConverter.ToInt16(rawBytes, index);
                index += 2;
                if ((Header.DataFlags & Header.DataFlag.IsUnicode) == Header.DataFlag.IsUnicode)
                {
                    RelativePath = Encoding.Unicode.GetString(rawBytes, index, relLen * 2);
                    index += relLen;
                }
                else
                {
                    RelativePath = Encoding.GetEncoding(1252).GetString(rawBytes, index, relLen);
                }
                index += relLen;
            }

            if ((Header.DataFlags & Header.DataFlag.HasWorkingDir) == Header.DataFlag.HasWorkingDir)
            {
                var workLen = BitConverter.ToInt16(rawBytes, index);
                index += 2;
                if ((Header.DataFlags & Header.DataFlag.IsUnicode) == Header.DataFlag.IsUnicode)
                {
                    WorkingDirectory = Encoding.Unicode.GetString(rawBytes, index, workLen * 2);
                    index += workLen;
                }
                else
                {
                    WorkingDirectory = Encoding.GetEncoding(1252).GetString(rawBytes, index, workLen);
                }
                index += workLen;
            }

            if ((Header.DataFlags & Header.DataFlag.HasArguments) == Header.DataFlag.HasArguments)
            {
                var argLen = BitConverter.ToInt16(rawBytes, index);
                index += 2;
                if ((Header.DataFlags & Header.DataFlag.IsUnicode) == Header.DataFlag.IsUnicode)
                {
                    Arguments = Encoding.Unicode.GetString(rawBytes, index, argLen * 2);
                    index += argLen;
                }
                else
                {
                    Arguments = Encoding.GetEncoding(1252).GetString(rawBytes, index, argLen);
                }
                index += argLen;
            }

            if ((Header.DataFlags & Header.DataFlag.HasIconLocation) == Header.DataFlag.HasIconLocation)
            {
                var icoLen = BitConverter.ToInt16(rawBytes, index);
                index += 2;
                if ((Header.DataFlags & Header.DataFlag.IsUnicode) == Header.DataFlag.IsUnicode)
                {
                    IconLocation = Encoding.Unicode.GetString(rawBytes, index, icoLen * 2);
                    index += icoLen;
                }
                else
                {
                    IconLocation = Encoding.GetEncoding(1252).GetString(rawBytes, index, icoLen);
                }
                index += icoLen;
            }

        


        }
    }
}
