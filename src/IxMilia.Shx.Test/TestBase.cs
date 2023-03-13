using System.IO;
using System.Runtime.CompilerServices;

namespace IxMilia.Shx.Test
{
    public abstract class TestBase
    {
        protected static string GetPathToSampleFile(string fontName, [CallerFilePath] string sourcePath = null)
        {
            var directory = Path.GetDirectoryName(sourcePath);
            var fullPath = Path.Combine(directory, "Samples", fontName);
            return fullPath;
        }
    }
}
