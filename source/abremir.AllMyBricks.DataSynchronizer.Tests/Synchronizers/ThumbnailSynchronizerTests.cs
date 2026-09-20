using System.Net;
using System.Threading.Tasks;
using abremir.AllMyBricks.Data.Models;
using abremir.AllMyBricks.DataSynchronizer.Synchronizers;
using abremir.AllMyBricks.Platform.Enumerations;
using abremir.AllMyBricks.Platform.Interfaces;
using Flurl.Http.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstituteAutoMocker.Standard;

namespace abremir.AllMyBricks.DataSynchronizer.Tests.Synchronizers
{
    [TestClass]
    public class ThumbnailSynchronizerTests
    {
        private NSubstituteAutoMocker<ThumbnailSynchronizer> _thumbnailSynchronizer;
        private HttpTest _httpTest;

        [TestInitialize]
        public void TestInitialize()
        {
            _httpTest = new HttpTest();
            _thumbnailSynchronizer = new NSubstituteAutoMocker<ThumbnailSynchronizer>();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _httpTest.Dispose();
        }

        [TestMethod]
        public async Task Synchronize_InvalidCachingPreferences_HttpNotInvoked()
        {
            _thumbnailSynchronizer.Get<IPreferencesService>().ThumbnailCachingStrategy.Returns(ThumbnailCachingStrategy.NeverCache);

            await _thumbnailSynchronizer.ClassUnderTest.Synchronize(new Set
            {
                Images =
                [
                    new() {
                        ThumbnailUrl = "THUMBNAIL_URL"
                    }
                ]
            });

            _httpTest.ShouldNotHaveMadeACall();
        }

        [TestMethod]
        public async Task Synchronize_InvalidSet_HttpNotInvoked()
        {
            _thumbnailSynchronizer.Get<IPreferencesService>().ThumbnailCachingStrategy.Returns(ThumbnailCachingStrategy.CacheAllThumbnailsWhenSynchronizing);

            await _thumbnailSynchronizer.ClassUnderTest.Synchronize(null);

            _httpTest.ShouldNotHaveMadeACall();
        }

        [TestMethod]
        public async Task Synchronize_NoImages_HttpNotInvoked()
        {
            _thumbnailSynchronizer.Get<IPreferencesService>().ThumbnailCachingStrategy.Returns(ThumbnailCachingStrategy.CacheAllThumbnailsWhenSynchronizing);

            await _thumbnailSynchronizer.ClassUnderTest.Synchronize(new Set
            {
                Images = []
            });

            _httpTest.ShouldNotHaveMadeACall();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public async Task Synchronize_InvalidThumbnailUrl_HttpNotInvoked(string thumbnailUrl)
        {
            _thumbnailSynchronizer.Get<IPreferencesService>().ThumbnailCachingStrategy.Returns(ThumbnailCachingStrategy.CacheAllThumbnailsWhenSynchronizing);

            await _thumbnailSynchronizer.ClassUnderTest.Synchronize(new Set
            {
                Images =
                [
                    new() {
                        ThumbnailUrl = thumbnailUrl
                    }
                ]
            });

            _httpTest.ShouldNotHaveMadeACall();
        }

        [TestMethod]
        public async Task Synchronize_InvalidImage_SaveThumbnailToCacheNotInvoked()
        {
            _thumbnailSynchronizer.Get<IPreferencesService>().ThumbnailCachingStrategy.Returns(ThumbnailCachingStrategy.CacheAllThumbnailsWhenSynchronizing);
            _httpTest.RespondWith(string.Empty);

            await _thumbnailSynchronizer.ClassUnderTest.Synchronize(new Set
            {
                Images =
                [
                    new() {
                        ThumbnailUrl = "http://www.url.com/thumbnails/thumbnail.jpg"
                    }
                ]
            });

            _httpTest.ShouldHaveMadeACall();
            await _thumbnailSynchronizer.Get<IFileSystemService>().DidNotReceiveWithAnyArgs().SaveThumbnailToCache(null, null, null, null);
        }

        [TestMethod]
        public async Task Synchronize_NotOkResult_SaveThumbnailToCacheNotInvoked()
        {
            _thumbnailSynchronizer.Get<IPreferencesService>().ThumbnailCachingStrategy.Returns(ThumbnailCachingStrategy.CacheAllThumbnailsWhenSynchronizing);
            _httpTest.RespondWith(status: (int)HttpStatusCode.NotFound);

            await _thumbnailSynchronizer.ClassUnderTest.Synchronize(new Set
            {
                Images =
                [
                    new() {
                        ThumbnailUrl = "http://www.url.com/thumbnails/thumbnail.jpg"
                    }
                ]
            });

            _httpTest.ShouldHaveMadeACall();
            await _thumbnailSynchronizer.Get<IFileSystemService>().DidNotReceiveWithAnyArgs().SaveThumbnailToCache(null, null, null, null);
        }

        [TestMethod]
        public async Task Synchronize_ValidImage_SaveThumbnailToCacheInvoked()
        {
            _thumbnailSynchronizer.Get<IPreferencesService>().ThumbnailCachingStrategy.Returns(ThumbnailCachingStrategy.CacheAllThumbnailsWhenSynchronizing);
            _httpTest.RespondWith("THUMBNAIL_IMAGE");

            await _thumbnailSynchronizer.ClassUnderTest.Synchronize(new Set
            {
                Images =
                [
                    new() {
                        ThumbnailUrl = "http://www.url.com/thumbnails/thumbnail.jpg"
                    }
                ],
                Theme = new Theme(),
                Subtheme = new Subtheme()
            });

            _httpTest.ShouldHaveMadeACall();
            await _thumbnailSynchronizer.Get<IFileSystemService>().ReceivedWithAnyArgs().SaveThumbnailToCache(null, null, null, null);
        }
    }
}
