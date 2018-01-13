using System.Collections;
using System.IO;
using System.Runtime.InteropServices;

namespace Ditto.Data
{
    public struct DirectoryData
    {

    }

    public abstract class BaseClass
    {
        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool IsOSX() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static string GetProperPath(string path)
        {
            var fileName = Path.GetFileName(path);
            return Path.GetDirectoryName(path) + fileName;
        }

        public static string GetProperPathAndCreate(string path)
        {
            var properPath = GetProperPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            return properPath;
        }
    }
}
