using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using abremir.AllMyBricks.AssetManagement.Implementations;
using abremir.AllMyBricks.AssetManagement.Interfaces;
using abremir.AllMyBricks.Platform.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NFluent;
using NSubstitute;
using NSubstituteAutoMocker.Standard;

namespace abremir.AllMyBricks.AssetManagement.Tests.Implementations
{
    [TestClass]
    public class AssetCompressionTests
    {
        private NSubstituteAutoMocker<AssetCompression> _assetCompression;

        [TestInitialize]
        public void TestInitialize()
        {
            _assetCompression = new NSubstituteAutoMocker<AssetCompression>();
        }

        [DataTestMethod]
        [DataRow("", true, null, false)]
        [DataRow(null, true, null, false)]
        [DataRow("source_file_path.txt", false, null, false)]
        [DataRow("source_file_path.txt", true, ".", false)]
        public void CompressAsset_InvalidParameters_ReturnsFalse(string sourceFilePath, bool sourceFileExists, string targetFolderPath, bool targetFolderPathIsFolder)
        {
            _assetCompression.Get<IFile>().Exists(Arg.Any<string>()).Returns(sourceFileExists);
            _assetCompression.Get<IDirectory>().Exists(Arg.Any<string>()).Returns(!targetFolderPathIsFolder);
            _assetCompression.Get<IFile>().GetAttributes(Arg.Any<string>()).Returns(targetFolderPathIsFolder ? FileAttributes.Directory : FileAttributes.Archive);

            var result = _assetCompression.ClassUnderTest.CompressAsset(sourceFilePath, targetFolderPath);

            Check.That(result).IsFalse();
        }

        [TestMethod]
        public void CompressAsset_InvalidTargetFilePath_ReturnsFalse()
        {
            const string sourceFilePath = "test_file.txt";
            _assetCompression.Get<IFile>().Exists(sourceFilePath).Returns(true);
            _assetCompression.Get<IFile>().Exists(AssetCompression.GetCompressedAssetFileName(sourceFilePath, false)).Returns(true);

            var result = _assetCompression.ClassUnderTest.CompressAsset(sourceFilePath, string.Empty, overwrite: false);

            Check.That(result).IsFalse();
        }

        [TestMethod]
        public void CompressAsset_ValidData_ReturnsTrue()
        {
            const string sourceFilePath = "test_file.txt";
            _assetCompression.Get<IFile>().Exists(sourceFilePath).Returns(true);

            var compressedTarHandler = _assetCompression.Get<ICompressedTarHandler>();

            var result = _assetCompression.ClassUnderTest.CompressAsset(sourceFilePath, string.Empty);

            Check.That(result).IsTrue();
            compressedTarHandler.Received().CreateCompressedTarFromDirectory(Arg.Any<string>(), Arg.Any<Stream>());
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void GetCompressedAssetFileName_InvalidFilePath_ReturnsNull(string fileName)
        {
            var result = AssetCompression.GetCompressedAssetFileName(fileName, false);

            Check.That(result).IsNull();
        }

        [DataTestMethod]
        [DynamicData(nameof(GetTestFilePaths), DynamicDataSourceType.Method)]
        public void GetCompressedAssetFileName_ValidFilePath_ReturnsNewFilename(string fileName)
        {
            var result = AssetCompression.GetCompressedAssetFileName(fileName, false);

            Check.That(result).IsNotNull().And.IsEqualTo("this_is_a_file.lz");
        }

        [DataTestMethod]
        [DynamicData(nameof(GetTestFilePaths), DynamicDataSourceType.Method)]
        public void GetCompressedAssetFileName_ValidFilePathAndEncrypted_ReturnsNewFilename(string fileName)
        {
            var result = AssetCompression.GetCompressedAssetFileName(fileName, true);

            Check.That(result).IsNotNull().And.IsEqualTo("this_is_a_file.lzc");
        }

        public static IEnumerable<object[]> GetTestFilePaths()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                yield return new object[] { @"F:\test\this_is_a_file.txt" };
                yield return new object[] { @"C:\this_is_a_file" };
            }
            else
            {
                yield return new object[] { @"~/test/this_is_a_file.txt" };
                yield return new object[] { @"~/this_is_a_file" };
            }
        }
    }
}
