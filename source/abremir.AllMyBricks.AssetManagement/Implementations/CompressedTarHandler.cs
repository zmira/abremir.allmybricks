using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using abremir.AllMyBricks.AssetManagement.Interfaces;

namespace abremir.AllMyBricks.AssetManagement.Implementations
{
    public class CompressedTarHandler : ICompressedTarHandler
    {
        public void CreateCompressedTarFromDirectory(string sourceDirectory, Stream outputStream)
        {
            using BrotliStream compressedStream = new(outputStream, CompressionLevel.SmallestSize);

            TarFile.CreateFromDirectory(sourceDirectory, compressedStream, includeBaseDirectory: false);
        }

        public void ExtractCompressedTarToDirectory(Stream inputStream, string targetDirectory, bool overwrite = true)
        {
            using BrotliStream compressedStream = new(inputStream, CompressionMode.Decompress);

            TarFile.ExtractToDirectory(compressedStream, targetDirectory, overwriteFiles: overwrite);
        }
    }
}
