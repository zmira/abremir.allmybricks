using System;
using System.IO;
using System.Threading.Tasks;
using abremir.AllMyBricks.Platform.Interfaces;
using abremir.AllMyBricks.Platform.Models;

namespace abremir.AllMyBricks.Platform.Implementations
{
    public class FileImplementation : IFile
    {
        public void Copy(string sourceFileName, string destFileName, bool overwrite)
        {
            File.Copy(sourceFileName, destFileName, overwrite);
        }

        public void Delete(string path)
        {
            File.Delete(path);
        }

        public void DeleteFileIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        public FileAttributes GetAttributes(string path)
        {
            return File.GetAttributes(path);
        }

        public FileSize GetFileSize(string path)
        {
            var fileInfo = new FileInfo(path);

            if (fileInfo.Exists)
            {
                return new FileSize(fileInfo.Length);
            }

            return new FileSize();
        }

        public FileStream OpenRead(string path)
        {
            return File.OpenRead(path);
        }

        public FileStream OpenWrite(string path)
        {
            return File.OpenWrite(path);
        }

        public async Task WriteAllBytes(string path, byte[] bytes)
        {
            await using var stream = new FileStream(path, FileMode.Create);

            await stream.WriteAsync(bytes.AsMemory(0, bytes.Length)).ConfigureAwait(false);
        }
    }
}
