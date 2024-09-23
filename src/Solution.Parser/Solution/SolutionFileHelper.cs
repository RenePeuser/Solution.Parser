using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Argument.Check;
using Extensions.Pack;

namespace Solution.Parser.Solution
{
    public static class SolutionFileHelper
    {
        public static SolutionFileInfo FindSolutionFileReverseFrom(this SolutionFileName solutionFileName, DirectoryInfo startUpDirectory)
        {
            Throw.IfNull(solutionFileName);
            Throw.IfNull(startUpDirectory);

            if (!startUpDirectory.Exists)
            {
                throw new ArgumentException("The start up directory have to be a existing directory");
            }

            var result = FindSolutionFileFrom(solutionFileName, startUpDirectory);
            if (result.IsNull())
            {
                throw new FileNotFoundException($"The solution file: {solutionFileName} could not be found from your given directory: {startUpDirectory.FullName} backwards until root. Please check your file location or solution name");
            }
            
            return result;
        }

        private static SolutionFileInfo? FindSolutionFileFrom(this SolutionFileName solutionFileName, DirectoryInfo? startUpDirectory)
        {
            while (startUpDirectory != null)
            {
                var solutionFile = startUpDirectory.EnumerateFiles().FirstOrDefault(item => item.Name.ToLower(CultureInfo.InvariantCulture).EqualsTo(solutionFileName.Value.ToLower(CultureInfo.InvariantCulture)));
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
