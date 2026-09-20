using System.IO;

namespace abremir.AllMyBricks.AssetManagement.Interfaces
{
    public interface ICompressedTarHandler
    {
        public void CreateCompressedTarFromDirectory(string sourceDirectory, Stream outputStream);
        public void ExtractCompressedTarToDirectory(Stream inputStream, string targetDirectory, bool overwrite = true);
    }
}
