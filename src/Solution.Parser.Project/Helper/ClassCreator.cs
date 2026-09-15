namespace Solution.Parser.Project
{
    public static class ClassCreator
    {
        public static AssemblyFileInfo CreateAssemblyInfo(string path)
        {
            return new AssemblyFileInfo(path);
        }
    }
}
