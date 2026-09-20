using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using abremir.AllMyBricks.AssetManagement.Interfaces;
using abremir.AllMyBricks.Platform.Interfaces;
using Easy.MessageHub;

namespace abremir.AllMyBricks.AssetManagement.Implementations
{
    public class AssetExpansion(
        IFile file,
        IDirectory directory,
        ICompressedTarHandler compressedTarHandler,
        IMessageHub messageHub)
        : IAssetExpansion
    {
        private readonly IFile _file = file;
        private readonly IDirectory _directory = directory;
        private readonly ICompressedTarHandler compressedTarHandler = compressedTarHandler;
        private readonly IMessageHub _messageHub = messageHub;

        public bool ExpandAsset(string sourceFilePath, string targetFolderPath, bool overwrite = true, string encryptionKey = null)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath)
                || !_file.Exists(sourceFilePath)
                || _file.GetAttributes(sourceFilePath).HasFlag(FileAttributes.Directory))
            {
                return false;
            }

            using var sourceFileStream = _file.OpenRead(sourceFilePath);

            return ExpandAsset(sourceFileStream, targetFolderPath, overwrite, encryptionKey);
        }

        public bool ExpandAsset(Stream sourceStream, string targetFolderPath, bool overwrite = true, string encryptionKey = null)
        {
            if (sourceStream is null
                || (!string.IsNullOrWhiteSpace(targetFolderPath)
                    && _directory.Exists(targetFolderPath)
                    && !_file.GetAttributes(targetFolderPath).HasFlag(FileAttributes.Directory)))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(targetFolderPath)
                && !_directory.Exists(targetFolderPath))
            {
                _directory.CreateDirectory(targetFolderPath);
            }

            using var workingStream = GetDecryptedStream(sourceStream, encryptionKey);

            compressedTarHandler.ExtractCompressedTarToDirectory(workingStream, targetFolderPath, overwrite);

            return true;
        }

        private static Stream GetDecryptedStream(Stream inputStream, string encryptionKey)
        {
            inputStream.Position = 0;

            if (encryptionKey is null)
            {
                return inputStream;
            }

            var hash = SHA256.HashData(Encoding.ASCII.GetBytes(encryptionKey));

            using var aes = Aes.Create();
            aes.Key = hash.Take(32).ToArray();
            aes.IV = hash.Take(16).ToArray();

            using var outputStream = new MemoryStream();
            using var cryptoStreamDecryptor = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read);

            cryptoStreamDecryptor.CopyTo(outputStream);

            outputStream.Flush();
            outputStream.Position = 0;

            return new MemoryStream(outputStream.ToArray());
        }
    }
}
