using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using abremir.AllMyBricks.AssetManagement.Interfaces;
using abremir.AllMyBricks.Platform.Interfaces;

namespace abremir.AllMyBricks.AssetManagement.Implementations
{
    public class AssetCompression(
        IFile file,
        IDirectory directory,
        IFileStream fileStream,
        ICompressedTarHandler compressedTarHandler)
        : IAssetCompression
    {
        private readonly IFile _file = file;
        private readonly IDirectory _directory = directory;
        private readonly IFileStream _fileStream = fileStream;
        private readonly ICompressedTarHandler _compressedTarHandler = compressedTarHandler;

        public bool CompressAsset(string sourceFilePath, string targetFolderPath, bool overwrite = true, string encryptionKey = null)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath)
                || !_file.Exists(sourceFilePath)
                || (!string.IsNullOrWhiteSpace(targetFolderPath)
                    && _directory.Exists(targetFolderPath)
                    && (_file.GetAttributes(targetFolderPath) & FileAttributes.Directory) is 0))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(targetFolderPath)
                && !_directory.Exists(targetFolderPath))
            {
                _directory.CreateDirectory(targetFolderPath);
            }

            var targetCompressedFilePath = Path.Combine(targetFolderPath ?? string.Empty, GetCompressedAssetFileName(sourceFilePath, false));
            var targetEncryptedFilePath = Path.Combine(targetFolderPath ?? string.Empty, GetCompressedAssetFileName(sourceFilePath, true));
            var encrypted = !string.IsNullOrWhiteSpace(encryptionKey);

            if (!overwrite
                && ((_file.Exists(targetCompressedFilePath) && !encrypted)
                    || (_file.Exists(targetEncryptedFilePath) && encrypted)))
            {
                return false;
            }

            _file.DeleteFileIfExists(targetCompressedFilePath);

            using var targetFileStream = _file.OpenWrite(targetCompressedFilePath);

            var tempDirectoryPath = Path.Combine(Path.GetDirectoryName(sourceFilePath), Path.GetRandomFileName());
            _directory.CreateDirectory(tempDirectoryPath);
            _file.Copy(sourceFilePath, Path.Combine(tempDirectoryPath, Path.GetFileName(sourceFilePath)), true);

            _compressedTarHandler.CreateCompressedTarFromDirectory(tempDirectoryPath, targetFileStream);

            EncryptCompressedFileIfRequired(encrypted, targetEncryptedFilePath, targetCompressedFilePath, encryptionKey);

            _directory.DeleteDirectoryIfExists(tempDirectoryPath, true);

            return true;
        }

        private void EncryptCompressedFileIfRequired(bool encrypted, string encryptedFilePath, string compressedFilePath, string encryptionKey)
        {
            if (encrypted)
            {
                _file.DeleteFileIfExists(encryptedFilePath);

                using var compressedFileStream = GetEncryptedStream(compressedFilePath, encryptionKey);
                using var targetEncryptedFileStream = _file.OpenWrite(encryptedFilePath);

                compressedFileStream.CopyTo(targetEncryptedFileStream);
                compressedFileStream.Flush();
                compressedFileStream.Close();
                targetEncryptedFileStream.Flush();
                targetEncryptedFileStream.Close();

                _file.DeleteFileIfExists(compressedFilePath);
            }
        }

        private MemoryStream GetEncryptedStream(string sourceFilePath, string encryptionKey)
        {
            var inputStream = _fileStream.CreateFileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            using var outputStream = new MemoryStream();

            var hash = SHA256.HashData(Encoding.ASCII.GetBytes(encryptionKey));

            using var aes = Aes.Create();
            aes.Key = [.. hash.Take(32)];
            aes.IV = [.. hash.Take(16)];

            using var cryptoStreamEncryptor = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write);

            inputStream.CopyTo(cryptoStreamEncryptor);

            inputStream.Close();
            cryptoStreamEncryptor.FlushFinalBlock();

            outputStream.Flush();
            outputStream.Position = 0;

            return new MemoryStream(outputStream.ToArray());
        }

        public static string GetCompressedAssetFileName(string expandedFilePath, bool encrypted)
        {
            if (string.IsNullOrWhiteSpace(expandedFilePath))
            {
                return null;
            }

            return $"{Path.GetFileNameWithoutExtension(expandedFilePath)}.lz{(encrypted ? "c" : string.Empty)}";
        }
    }
}
