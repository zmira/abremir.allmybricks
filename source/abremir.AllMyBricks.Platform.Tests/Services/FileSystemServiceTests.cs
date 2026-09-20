using System.Text.RegularExpressions;
using System.Threading.Tasks;
using abremir.AllMyBricks.Platform.Configuration;
using abremir.AllMyBricks.Platform.Interfaces;
using abremir.AllMyBricks.Platform.Services;
using Microsoft.Maui.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NFluent;
using NSubstitute;
using NSubstituteAutoMocker.Standard;

namespace abremir.AllMyBricks.Platform.Tests.Services
{
    [TestClass]
    public partial class FileSystemServiceTest
    {
        private NSubstituteAutoMocker<FileSystemService> _fileSystemService;

        [TestInitialize]
        public void TestInitialize()
        {
            _fileSystemService = new NSubstituteAutoMocker<FileSystemService>();

            _fileSystemService.Get<IFileSystem>().AppDataDirectory.Returns("./");
        }

        [TestMethod]
        [DataRow(null, null)]
        [DataRow("", null)]
        [DataRow(" ", null)]
        [DataRow(null, "")]
        [DataRow(null, " ")]
        [DataRow("", "")]
        [DataRow(" ", " ")]
        [DataRow("FILENAME", null)]
        [DataRow("FILENAME", "")]
        [DataRow("FILENAME", " ")]
        [DataRow(null, "FOLDERNAME")]
        [DataRow("", "FOLDERNAME")]
        [DataRow(" ", "FOLDERNAME")]
        [DataRow("FILENAME", "FOLDERNAME")]
        public void GetLocalPathToFile_ReturnsValidPathWithFilename(string filename, string folder)
        {
            var localPathToFile = _fileSystemService.ClassUnderTest.GetLocalPathToFile(filename, folder);

            Check.That(localPathToFile).Not.IsNullOrWhiteSpace().And.Contains(Constants.AllMyBricksDataFolder);

            if (!string.IsNullOrWhiteSpace(filename))
            {
                Check.That(localPathToFile).EndsWith(filename);
            }

            if (!string.IsNullOrWhiteSpace(folder))
            {
                Check.That(localPathToFile).Contains(folder);
            }
        }

        [TestMethod]
        [DataRow(null, null, 2)]
        [DataRow("", null, 2)]
        [DataRow(null, "", 2)]
        [DataRow("", "", 2)]
        [DataRow("THEME", null, 1)]
        [DataRow("", "SUBTHEME", 1)]
        [DataRow("THEME", "SUBTHEME", 0)]
        public void GetThumbnailFolder_ReturnsValidPath(string theme, string subtheme, int countOfFallbackFolderName)
        {
            var thumbnailFolder = _fileSystemService.ClassUnderTest.GetThumbnailFolder(theme, subtheme);

            Check.That(thumbnailFolder).Not.IsNullOrWhiteSpace().And.Contains(Constants.AllMyBricksDataFolder).And.Contains(Constants.ThumbnailCacheFolder);

            Check.That(FallbackFolderNameRegex().Matches(thumbnailFolder).Count).IsEqualTo(countOfFallbackFolderName);
        }

        [TestMethod]
#pragma warning disable CA1861 // Avoid constant arrays as arguments
        [DataRow(null, null, false)]
        [DataRow("", null, false)]
        [DataRow(null, new byte[] { }, false)]
        [DataRow("", new byte[] { }, false)]
        [DataRow("FILENAME", null, false)]
        [DataRow("FILENAME", new byte[] { }, false)]
        [DataRow(null, new byte[] { 0 }, false)]
        [DataRow("", new byte[] { 0 }, false)]
        [DataRow("FILENAME", new byte[] { 0 }, true)]
#pragma warning restore CA1861 // Avoid constant arrays as arguments
        public async Task SaveThumbnailToCache_InvokesWriteAllBytes(string filename, byte[] thumbnail, bool invokesWriteAllBytes)
        {
            await _fileSystemService.ClassUnderTest.SaveThumbnailToCache(string.Empty, string.Empty, filename, thumbnail);

            if (invokesWriteAllBytes)
            {
                await _fileSystemService.Get<IFile>().Received().WriteAllBytes(Arg.Any<string>(), Arg.Any<byte[]>());
            }
            else
            {
                await _fileSystemService.Get<IFile>().DidNotReceive().WriteAllBytes(Arg.Any<string>(), Arg.Any<byte[]>());
            }
        }

        [GeneratedRegex(Constants.FallbackFolderName)]
        private static partial Regex FallbackFolderNameRegex();
    }
}
