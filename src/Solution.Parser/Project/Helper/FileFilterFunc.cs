using System.IO;
using Argument.Check;
using Extensions.Pack;

namespace Solution.Parser.Project
{
    internal static class FileFilterFunc
    {
        internal static bool CSharpFileInfoFilterFunc(FileInfo fileInfo)
        {
            Throw.IfNull(() => fileInfo);

            return fileInfo.Name.EndWith(".cs") && !fileInfo.Name.Contains(".g.");
        }

        internal static bool XamlFileInfoFilterFunc(FileInfo fileInfo)
        {
            Throw.IfNull(() => fileInfo);

            return fileInfo.Name.EndWith(".xaml") && !fileInfo.Name.Contains(".g.");
        }
    }
}
