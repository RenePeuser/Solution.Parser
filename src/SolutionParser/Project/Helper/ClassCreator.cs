using System.IO;
using SolutionParser.CSharp;
using SolutionParser.XAML;

namespace SolutionParser.Project
{
    public static class ClassCreator
    {
        public static AssemblyFileInfo CreateAssemblyInfo(string path)
        {
            return new AssemblyFileInfo(path);
        }

        internal static CSharpFileInfo CreateCSharpFile(string path)
        {
            return new CSharpFileInfo(path);
        }

        internal static XAMLFileInfo CreateXAMLFile(string path)
        {
            return new XAMLFileInfo(path);
        }

        internal static CSharpFileInfo CreateCSharpFile(FileInfo fileInfo)
        {
            return new CSharpFileInfo(fileInfo.FullName);
        }

        internal static XAMLFileInfo CreateXAMLFile(FileInfo fileInfo)
        {
            return new XAMLFileInfo(fileInfo.FullName);
        }
    }
}
