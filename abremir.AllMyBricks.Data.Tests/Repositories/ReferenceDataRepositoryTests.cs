﻿using abremir.AllMyBricks.Data.Interfaces;
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
        private static IReferenceDataRepository _referenceDataRepository;

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            _referenceDataRepository = new ReferenceDataRepository(MemoryRepositoryService);
        }

        [DataTestMethod]
        [DataRow(ModelsSetup.StringEmpty)]
        [DataRow(null)]
        public void GivenGetOrAdd_WhenInvalidCategoryReferenceDataValue_ThenReturnsNull(string referenceDataValue)
        {
            GetOrAddTestHelper<Category>(referenceDataValue, null);
        }

        [DataTestMethod]
        [DataRow(ModelsSetup.StringEmpty)]
        [DataRow(null)]
        public void GivenGetOrAdd_WhenInvalidPackagingTypeReferenceDataValue_ThenReturnsNull(string referenceDataValue)
        {
            GetOrAddTestHelper<PackagingType>(referenceDataValue, null);
        }

        [DataTestMethod]
        [DataRow(ModelsSetup.StringEmpty)]
        [DataRow(null)]
        public void GivenGetOrAdd_WhenInvalidTagReferenceDataValue_ThenReturnsNull(string referenceDataValue)
        {
            GetOrAddTestHelper<Tag>(referenceDataValue, null);
        }

        [DataTestMethod]
        [DataRow(ModelsSetup.StringEmpty)]
        [DataRow(null)]
        public void GivenGetOrAdd_WhenInvalidThemeGroupReferenceDataValue_ThenReturnsNull(string referenceDataValue)
        {
            GetOrAddTestHelper<ThemeGroup>(referenceDataValue, null);
        }

        [TestMethod]
        public void GivenGetOrAdd_WhenCategoryReferenceDataDoesNotExist_ThenInsertsReferenceData()
        {
            GetOrAddTestHelper<Category>(ModelsSetup.CategoryReferenceDataValue, ModelsSetup.CategoryReferenceData, insert: true);
        }

        [TestMethod]
        public void GivenGetOrAdd_WhenPackaginTypeReferenceDataDoesNotExist_ThenInsertsReferenceData()
        {
            GetOrAddTestHelper<PackagingType>(ModelsSetup.PackagingTypeReferenceDataValue, ModelsSetup.PackagingTypeReferenceData, insert: true);
        }

        [TestMethod]
        public void GivenGetOrAdd_WhenTagReferenceDataDoesNotExist_ThenInsertsReferenceData()
        {
            GetOrAddTestHelper<Tag>(ModelsSetup.TagReferenceDataValue, ModelsSetup.TagReferenceData, insert: true);
        }

        [TestMethod]
        public void GivenGetOrAdd_WhenThemeGroupReferenceDataDoesNotExist_ThenInsertsReferenceData()
        {
            GetOrAddTestHelper<ThemeGroup>(ModelsSetup.ThemeGroupReferenceDataValue, ModelsSetup.ThemeGroupReferenceData, insert: true);
        }

        [TestMethod]
        public void GivenGetOrAdd_WhenCategoryReferenceDataExists_ThenDoesNotInsertReferenceData()
        {
            GetOrAddTestHelper<Category>(ModelsSetup.CategoryReferenceDataValue, ModelsSetup.CategoryReferenceData, exists: true);
        }

        [TestMethod]
        public void GivenGetOrAdd_WhenPackaginTypeReferenceDataExists_ThenDoesNotInsertReferenceData()
        {
            GetOrAddTestHelper<PackagingType>(ModelsSetup.PackagingTypeReferenceDataValue, ModelsSetup.PackagingTypeReferenceData, exists: true);
        }

        [TestMethod]
        public void GivenGetOrAdd_WhenTagReferenceDataExists_ThenDoesNotInsertReferenceData()
        {
            GetOrAddTestHelper<Tag>(ModelsSetup.TagReferenceDataValue, ModelsSetup.TagReferenceData, exists: true);
        }

        [TestMethod]
        public void GivenGetOrAdd_WhenThemeGroupReferenceDataExists_ThenDoesNotInsertReferenceData()
        {
            GetOrAddTestHelper<ThemeGroup>(ModelsSetup.ThemeGroupReferenceDataValue, ModelsSetup.ThemeGroupReferenceData, exists: true);
        }

        private void GetOrAddTestHelper<T>(string referenceDataValue, T expectedReferenceData, bool insert = false, bool exists = false) where T : IReferenceData, new()
        {
            if (exists)
            {
                _referenceDataRepository.GetOrAdd<T>(expectedReferenceData.Value);
            }

            T referenceData = _referenceDataRepository.GetOrAdd<T>(referenceDataValue);

            if (!(expectedReferenceData is null))
            {
                Check.That(referenceData?.GetType()).IsEqualTo(typeof(T));
            }

            Check.That(referenceData?.Value).IsEqualTo(expectedReferenceData?.Value);

            if (insert || exists)
            {
                var referenceDataList = MemoryRepositoryService.GetRepository().Query<T>().ToList();

                Check.That(referenceDataList).CountIs(1);
                Check.That(referenceDataList[0].Value).IsEqualTo(referenceDataValue);
            }
        }
    }
}
