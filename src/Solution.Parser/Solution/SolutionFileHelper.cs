using System;
using System.Collections.Immutable;
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

            var candidateFileNames = CandidateFileNames(solutionFileName);

            var result = FindSolutionFileFrom(candidateFileNames, startUpDirectory);
            if (result.IsNull())
            {
                throw new FileNotFoundException($"The solution file: {string.Join(" or ", candidateFileNames)} could not be found from your given directory: {startUpDirectory.FullName} backwards until root. Please check your file location or solution name");
            }

            return result;
        }

        private static ImmutableList<string> CandidateFileNames(SolutionFileName solutionFileName)
        {
            if (solutionFileName.HasExplicitFormat)
            {
                return [solutionFileName.Value];
            }

            return SolutionFileFormat.All.Select(extension => $"{solutionFileName.Value}{extension}").ToImmutableList();
        }

        private static SolutionFileInfo? FindSolutionFileFrom(ImmutableList<string> candidateFileNames, DirectoryInfo? startUpDirectory)
        {
            while (startUpDirectory != null)
            {
                var filesInDirectory = startUpDirectory.EnumerateFiles().ToImmutableList();

                var solutionFile = candidateFileNames.Select(candidate => filesInDirectory.FirstOrDefault(file => file.Name.EqualsToIgnoringCase(candidate)))
                                                     .FirstOrDefault(file => file.IsNotNull());

                if (solutionFile.IsNotNull())
                {
                    return new SolutionFileInfo(solutionFile!.FullName);
                }

                startUpDirectory = startUpDirectory.Parent;
            }

            return null;
        }
    }
}
