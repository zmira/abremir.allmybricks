﻿using System.IO;
using abremir.AllMyBricks.AssetManagement.Implementations;
using abremir.AllMyBricks.Platform.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NFluent;
using NSubstitute;
using NSubstituteAutoMocker.Standard;
using SharpCompress.Readers;

namespace abremir.AllMyBricks.AssetManagement.Tests.Implementations
{
    [TestClass]
    public class AssetExpansionTests
    {
        private NSubstituteAutoMocker<AssetExpansion> _assetExpansion;

        [TestInitialize]
        public void TestInitialize()
        {
            _assetExpansion = new NSubstituteAutoMocker<AssetExpansion>();
        }

        [DataTestMethod]
        [DataRow(false, true)]
        [DataRow(true, false)]
        public void ExpandAsset_ForStreamAndInvalidParameters_ReturnsFalse(bool validStream, bool validTargetFolder)
        {
            _assetExpansion.Get<IFile>()
                .GetAttributes(Arg.Any<string>())
                .Returns(validTargetFolder ? FileAttributes.Directory : FileAttributes.Archive);
            _assetExpansion.Get<IDirectory>()
                .Exists(Arg.Any<string>())
                .Returns(!validTargetFolder);

            var result = _assetExpansion.ClassUnderTest.ExpandAsset(validStream ? new MemoryStream() : null, ".");

            Check.That(result).IsFalse();
        }

        [DataTestMethod]
        [DataRow(null, true, true)]
        [DataRow("", true, true)]
        [DataRow("test_file.txt", false, true)]
        [DataRow("test_file.txt", true, false)]
        public void ExpandAsset_ForFileAndInvalidParameters_ReturnsFalse(string sourceFilePath, bool sourceFileExists, bool sourceFileIsFile)
        {
            _assetExpansion.Get<IFile>()
                .Exists(Arg.Any<string>())
                .Returns(sourceFileExists);
            _assetExpansion.Get<IFile>()
                .GetAttributes(Arg.Any<string>())
                .Returns(sourceFileIsFile ? FileAttributes.Archive : FileAttributes.Directory);

            var result = _assetExpansion.ClassUnderTest.ExpandAsset(sourceFilePath, string.Empty);

            Check.That(result).IsFalse();
        }

        [TestMethod]
        public void ExpandAsset_ValidParameters_ReturnsTrue()
        {
            _assetExpansion.Get<IFile>()
                .GetAttributes(Arg.Any<string>())
                .Returns(FileAttributes.Directory);
            _assetExpansion.Get<IDirectory>()
                .Exists(Arg.Any<string>())
                .Returns(true);

            var reader = Substitute.For<IReader>();
            reader
                .MoveToNextEntry()
                .Returns(true, false);
            reader.Entry
                .IsDirectory
                .Returns(false);
            reader.Entry
                .Key
                .Returns("test_file.txt");

            _assetExpansion.Get<Interfaces.IReaderFactory>()
                .Open(Arg.Any<Stream>())
                .Returns(reader);

            var result = _assetExpansion.ClassUnderTest.ExpandAsset(new MemoryStream(), string.Empty);

            Check.That(result).IsTrue();
            reader.Received()
                .WriteEntryTo(Arg.Any<Stream>());
        }
    }
}
