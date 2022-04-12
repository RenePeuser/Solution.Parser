using System;
using System.IO;
using System.Linq;
using Argument.Check;
using Extensions.Pack;

namespace SolutionParser.Solution
{
    public static class SolutionFileHelper
    {
        public static SolutionFileInfo FindSolutionFileReverseFrom(this SolutionFileName solutionFileName,
            DirectoryInfo startUpDirectory)
        {
            Throw.IfNull(() => solutionFileName);
            Throw.IfNull(() => startUpDirectory);

            if (!startUpDirectory.Exists)
            {
                throw new ArgumentException("The start up directory have to be a existing directory");
            }

            var result = FindSolutionFileFrom(solutionFileName, startUpDirectory);
            return result;
        }

        public static DirectoryInfo FindDirectoryFrom(this DirectoryInfo startUpDirectory, string nameOfDirectory)
        {
            while (startUpDirectory != null)
            {
                var expectedDirectoryInfo = startUpDirectory.EnumerateDirectories()
                    .FirstOrDefault(item => item.Name.EqualsToIgnoringCase(nameOfDirectory));
                if (expectedDirectoryInfo != null)
                {
                    return expectedDirectoryInfo;
                }

                startUpDirectory = startUpDirectory.Parent;
            }

            return null;
        }

        private static SolutionFileInfo FindSolutionFileFrom(this SolutionFileName solutionFileName,
            DirectoryInfo startUpDirectory)
        {
            while (startUpDirectory != null)
            {
                var solutionFile = startUpDirectory.EnumerateFiles()
                    .FirstOrDefault(item => item.Name.EqualsTo(solutionFileName.Value));
                if (solutionFile != null)
                {
                    return new SolutionFileInfo(solutionFile.FullName);
                }

                startUpDirectory = startUpDirectory.Parent;
            }

            return null;
        }
    }
}
