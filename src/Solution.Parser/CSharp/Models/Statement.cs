using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solution.Parser.CSharp.Models
{
    public record Statement(string SyntaxTree, string FilePath);
}
