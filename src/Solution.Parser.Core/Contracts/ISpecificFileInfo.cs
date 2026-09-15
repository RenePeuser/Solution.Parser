using FileSystem.Abstraction;

namespace Solution.Parser.Core
{
    public interface ISpecificFileInfo
    {
        string FileNameWithoutExtension { get; }

        IFileInfo Value { get; }
    }
}
