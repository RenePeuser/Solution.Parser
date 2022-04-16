using FileSystem.Abstraction;

namespace Solution.Parser.Common
{
    public interface ISpecificFileInfo
    {
        string FileNameWithoutExtension { get; }

        IFileInfo Value { get; }
    }
}
