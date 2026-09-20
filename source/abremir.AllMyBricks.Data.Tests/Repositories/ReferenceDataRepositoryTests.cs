using System.Threading.Tasks;
using abremir.AllMyBricks.Data.Interfaces;
using abremir.AllMyBricks.Data.Models;
using abremir.AllMyBricks.Data.Repositories;
using abremir.AllMyBricks.Data.Tests.Configuration;
using abremir.AllMyBricks.Data.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NFluent;

namespace abremir.AllMyBricks.Data.Tests.Repositories
{
    [TestClass]
    public class ReferenceDataRepositoryTests : DataTestsBase
    {
        private static ReferenceDataRepository _referenceDataRepository;

        public ReferenceDataRepositoryTests()
        {
            _referenceDataRepository = new ReferenceDataRepository(MemoryRepositoryService);
        }

        [TestMethod]
        [DataRow(ModelsSetup.StringEmpty)]
        [DataRow(null)]
        public async Task GetOrAdd_InvalidCategoryReferenceDataValue_ReturnsNull(string referenceDataValue)
        {
            await GetOrAddTestHelper<Category>(referenceDataValue, null);
        }

        [TestMethod]
        [DataRow(ModelsSetup.StringEmpty)]
        [DataRow(null)]
        public async Task GetOrAdd_InvalidPackagingTypeReferenceDataValue_ReturnsNull(string referenceDataValue)
        {
            await GetOrAddTestHelper<PackagingType>(referenceDataValue, null);
        }

        [TestMethod]
        [DataRow(ModelsSetup.StringEmpty)]
        [DataRow(null)]
        public async Task GetOrAdd_InvalidTagReferenceDataValue_ReturnsNull(string referenceDataValue)
        {
            await GetOrAddTestHelper<Tag>(referenceDataValue, null);
        }

        [TestMethod]
        [DataRow(ModelsSetup.StringEmpty)]
        [DataRow(null)]
        public async Task GetOrAdd_InvalidThemeGroupReferenceDataValue_ReturnsNull(string referenceDataValue)
        {
            await GetOrAddTestHelper<ThemeGroup>(referenceDataValue, null);
        }

        [TestMethod]
        public async Task GetOrAdd_CategoryReferenceDataDoesNotExist_InsertsReferenceData()
        {
            await GetOrAddTestHelper(ModelsSetup.CategoryReferenceDataValue, ModelsSetup.CategoryReferenceData, insert: true);
        }

        [TestMethod]
        public async Task GetOrAdd_PackagingTypeReferenceDataDoesNotExist_InsertsReferenceData()
        {
            await GetOrAddTestHelper(ModelsSetup.PackagingTypeReferenceDataValue, ModelsSetup.PackagingTypeReferenceData, insert: true);
        }

        [TestMethod]
        public async Task GetOrAdd_TagReferenceDataDoesNotExist_InsertsReferenceData()
        {
            await GetOrAddTestHelper(ModelsSetup.TagReferenceDataValue, ModelsSetup.TagReferenceData, insert: true);
        }

        [TestMethod]
        public async Task GetOrAdd_ThemeGroupReferenceDataDoesNotExist_InsertsReferenceData()
        {
            await GetOrAddTestHelper(ModelsSetup.ThemeGroupReferenceDataValue, ModelsSetup.ThemeGroupReferenceData, insert: true);
        }

        [TestMethod]
        public async Task GetOrAdd_CategoryReferenceDataExists_DoesNotInsertReferenceData()
        {
            await GetOrAddTestHelper(ModelsSetup.CategoryReferenceDataValue, ModelsSetup.CategoryReferenceData, exists: true);
        }

        [TestMethod]
        public async Task GetOrAdd_PackagingTypeReferenceDataExists_DoesNotInsertReferenceData()
        {
            await GetOrAddTestHelper(ModelsSetup.PackagingTypeReferenceDataValue, ModelsSetup.PackagingTypeReferenceData, exists: true);
        }

        [TestMethod]
        public async Task GetOrAdd_TagReferenceDataExists_DoesNotInsertReferenceData()
        {
            await GetOrAddTestHelper(ModelsSetup.TagReferenceDataValue, ModelsSetup.TagReferenceData, exists: true);
        }

        [TestMethod]
        public async Task GetOrAdd_ThemeGroupReferenceDataExists_DoesNotInsertReferenceData()
        {
            await GetOrAddTestHelper(ModelsSetup.ThemeGroupReferenceDataValue, ModelsSetup.ThemeGroupReferenceData, exists: true);
        }

        private async Task GetOrAddTestHelper<T>(string referenceDataValue, T expectedReferenceData, bool insert = false, bool exists = false) where T : IReferenceData, new()
        {
            if (exists)
            {
                await _referenceDataRepository.GetOrAdd<T>(expectedReferenceData.Value);
            }

            T referenceData = await _referenceDataRepository.GetOrAdd<T>(referenceDataValue);

            if (expectedReferenceData is not null)
            {
                Check.That(referenceData?.GetType()).IsEqualTo(typeof(T));
            }

            Check.That(referenceData?.Value).IsEqualTo(expectedReferenceData?.Value);

            if (insert || exists)
            {
                using var repository = MemoryRepositoryService.GetRepository();
                var referenceDataList = await repository.Query<T>().ToListAsync();

                Check.That(referenceDataList).CountIs(1);
                Check.That(referenceDataList[0].Value).IsEqualTo(referenceDataValue);
            }
        }
    }
}
