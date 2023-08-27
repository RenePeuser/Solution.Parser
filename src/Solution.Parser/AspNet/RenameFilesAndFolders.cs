using System.Collections.Generic;
using System.IO;
using System.Linq;
using Extensions.Pack;
using FileSystem.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using DirectoryInfo = FileSystem.Abstraction.DirectoryInfo;

namespace Solution.Parser.AspNet
{
    internal static class AddRenameFilesAndFoldersExtension
    {
        internal static void AddRenameFilesAndFolders(this IServiceCollection services)
        {
            services.AddSingletonIfNotExists<IRenameFilesAndFolders, RenameFilesAndFolders>();
        }
    }

    internal interface IRenameFilesAndFolders
    {
        void Rename(IDirectoryInfo directoryInfo, string originalName, string newName);
        void Rename2(IDirectoryInfo directoryInfo, string originalName, string newName);
    }

    internal sealed class RenameFilesAndFolders : IRenameFilesAndFolders
    {
        public void Rename(IDirectoryInfo directoryInfo, string originalName, string newName)
        {
            var folders = directoryInfo.EnumerateDirectories("*.*", SearchOption.AllDirectories);
            foreach (var folder in folders)
            {
                if (folder.Name.Contains(originalName))
                {
                    Directory.Move(folder.FullName, folder.FullName.Replace(originalName, newName));
                }
            }

            var allFiles = directoryInfo.EnumerateFiles("*.*", SearchOption.AllDirectories);
            foreach (var fileInfo in allFiles)
            {
                var content = File.ReadAllText(fileInfo.FullName);
                if (content.Contains(originalName))
                {
                    var newContent = content.Replace(originalName, newName);
                    File.WriteAllText(fileInfo.FullName, newContent);
                }

                if (fileInfo.Name.Contains(originalName))
                {
                    File.Move(fileInfo.FullName, fileInfo.FullName.Replace(originalName, newName));
                }
            }
        }

        public void Rename2(IDirectoryInfo directoryInfo, string originalName, string newName)
        {
            var newDirectories = GetNewDirectories();

            IEnumerable<DirectoryInfo> GetNewDirectories()
            {
                var folders = directoryInfo.EnumerateDirectories($"{originalName}*", SearchOption.AllDirectories);
                foreach (var folder in folders)
                {
                    if (folder.Name.Contains(originalName))
                    {
                        var destDirName = folder.FullName.Replace(originalName, newName);
                        if (Directory.Exists(destDirName).IsFalse())
                        {
                            Directory.Move(folder.FullName, destDirName);
                            yield return new DirectoryInfo(new System.IO.DirectoryInfo(destDirName));
                        }
                    }
                }
            }


            var allFiles = newDirectories.SelectMany(f => f.EnumerateFiles("*.*", SearchOption.AllDirectories));
            foreach (var fileInfo in allFiles)
            {
                var content = File.ReadAllText(fileInfo.FullName);
                if (content.Contains(originalName))
                {
                    var newContent = content.Replace(originalName, newName);
                    File.WriteAllText(fileInfo.FullName, newContent);
                }

                if (fileInfo.Name.Contains(originalName))
                {
                    var destFileName = fileInfo.FullName.Replace(originalName, newName);
                    File.Move(fileInfo.FullName, destFileName);
                }
            }
        }
    }
}
