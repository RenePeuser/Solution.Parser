using Solution.Parser.Core;

namespace Solution.Parser.CSharp
{
    public class CSharpFileInfo(string path) : SpecificFileInfo(path, ".cs");
}
