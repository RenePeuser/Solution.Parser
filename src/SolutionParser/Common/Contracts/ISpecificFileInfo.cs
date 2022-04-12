using FileSystem.Abstraction;

namespace SolutionParser.Common
{
    public interface ISpecificFileInfo
    {
        string FileNameWithoutExtension { get; }

        IFileInfo Value { get; }
    }
}
