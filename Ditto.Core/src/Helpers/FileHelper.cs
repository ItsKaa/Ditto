using System;
using System.IO;

namespace Ditto.Helpers
{
    public static class FileHelper
    {
        public static string GetFileFromPathEnv(string fileName)
        {
            var values = Environment.GetEnvironmentVariable("PATH");
            if (values != null)
            {
                foreach (var path in values.Split(Path.PathSeparator))
                {
                    var fullPath = Path.Combine(path, fileName);
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
            }
            return null;
        }
    }
}
