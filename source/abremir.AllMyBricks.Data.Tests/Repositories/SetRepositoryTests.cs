using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using abremir.AllMyBricks.Data.Configuration;
using abremir.AllMyBricks.Data.Enumerations;
using abremir.AllMyBricks.Data.Models;
using abremir.AllMyBricks.Data.Repositories;
using abremir.AllMyBricks.Data.Tests.Configuration;
using abremir.AllMyBricks.Data.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NFluent;

namespace abremir.AllMyBricks.Data.Tests.Repositories
{
    [TestClass]
    public class SetRepositoryTests : DataTestsBase
    {
        private static SetRepository _setRepository;

        public SetRepositoryTests()
        {
            _setRepository = new SetRepository(MemoryRepositoryService);
        }

        [TestMethod]
        public async Task Get_InvalidSetId_ReturnsNull()
        {
            var set = await _setRepository.Get(0);

            Check.That(set).IsNull();
        }

        [TestMethod]
        public async Task Get_SetDoesNotExist_ReturnsNull()
        {
            var setUnderTest = ModelsSetup.GetSetUnderTest();

            await InsertData(setUnderTest);

            var set = await _setRepository.Get(setUnderTest.SetId + 1);

            Check.That(set).IsNull();
        }

        [TestMethod]
        public async Task Get_SetExists_ReturnsModel()
        {
            var setUnderTest = ModelsSetup.GetSetUnderTest();

            await InsertData(setUnderTest);

            var set = await _setRepository.Get(setUnderTest.SetId);

            Check.That(set.SetId).IsEqualTo(setUnderTest.SetId);
        }

        [TestMethod]
        public async Task All_NoSets_ReturnsEmpty()
        {
            var allSets = await _setRepository.All();

            Check.That(allSets).IsEmpty();
        }

        [TestMethod]
        public async Task All_HasSets_ReturnsModels()
        {
            var listOfSetsUnderTest = ModelsSetup.ListOfSetsUnderTest;

            await InsertData(listOfSetsUnderTest);

            var allSets = await _setRepository.All();

            Check.That(allSets.Select(set => set.SetId)).IsEquivalentTo(listOfSetsUnderTest.Select(set => set.SetId));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(ModelsSetup.StringEmpty)]
        public async Task AllForTheme_InvalidThemeName_ReturnsEmpty(string themeName)
        {
            var allSetsForTheme = await _setRepository.AllForTheme(themeName);

            Check.That(allSetsForTheme).IsEmpty();
        }

        [TestMethod]
        public async Task AllForTheme_SetForThemeDoesNotExist_ReturnsEmpty()
        {
            var set = ModelsSetup.GetSetUnderTest();
            set.Theme = await InsertData(ModelsSetup.GetThemeUnderTest(Guid.NewGuid().ToString()));

            await InsertData(set);

            var allSetsForTheme = await _setRepository.AllForTheme(ModelsSetup.NonExistentThemeName);

            Check.That(allSetsForTheme).IsEmpty();
        }

        [TestMethod]
        public async Task AllForTheme_SetForThemeExists_ReturnsModels()
        {
            var listOfThemes = await InsertData(ModelsSetup.ListOfThemesUnderTest);

            var listOfSets = ModelsSetup.ListOfSetsUnderTest;
            listOfSets[0].Theme = listOfThemes[0];
            listOfSets[1].Theme = listOfThemes[1];

            await InsertData(listOfSets);

            var allSetsForTheme = await _setRepository.AllForTheme(listOfThemes[0].Name);

            Check.That(allSetsForTheme).CountIs(1);
            Check.That(allSetsForTheme.First().SetId).IsEqualTo(listOfSets[0].SetId);
        }

        [TestMethod]
        [DataRow(null, null)]
        [DataRow(null, ModelsSetup.StringEmpty)]
        [DataRow(ModelsSetup.StringEmpty, null)]
        [DataRow(ModelsSetup.StringEmpty, ModelsSetup.StringEmpty)]
        public async Task AllForSubtheme_InvalidSubthemeName_ReturnsEmpty(string themeName, string subthemeName)
        {
            var allSetsForSubtheme = await _setRepository.AllForSubtheme(themeName, subthemeName);

            Check.That(allSetsForSubtheme).IsEmpty();
        }

        [TestMethod]
        public async Task AllForSubtheme_SetForSubthemeDoesNotExist_ReturnsEmpty()
        {
            var listOfThemes = await InsertData(ModelsSetup.ListOfThemesUnderTest);

            var listOfSubthemes = ModelsSetup.ListOfSubthemesUnderTest;
            listOfSubthemes[0].Theme = listOfThemes[0];
            listOfSubthemes[1].Theme = listOfThemes[1];

            var listOfSubthemesUnderTest = await InsertData(listOfSubthemes);

            var listOfSets = ModelsSetup.ListOfSetsUnderTest;
            listOfSets[0].Theme = listOfThemes[0];
            listOfSets[0].Subtheme = listOfSubthemesUnderTest[0];
            listOfSets[1].Theme = listOfThemes[1];
            listOfSets[1].Subtheme = listOfSubthemesUnderTest[1];

            await InsertData(listOfSets);

            var allSetsForSubtheme = await _setRepository.AllForSubtheme(listOfSets[0].Theme.Name, ModelsSetup.NonExistentSubthemeName);

            Check.That(allSetsForSubtheme).IsEmpty();
        }

        [TestMethod]
        public async Task AllForSubtheme_SetForSubthemeExists_ReturnsModels()
        {
            var listOfThemes = await InsertData(ModelsSetup.ListOfThemesUnderTest);

            var listOfSubthemes = ModelsSetup.ListOfSubthemesUnderTest;
            listOfSubthemes[0].Theme = listOfThemes[0];
            listOfSubthemes[1].Theme = listOfThemes[1];

            var listOfSubthemesUnderTest = await InsertData(listOfSubthemes);

            var listOfSets = ModelsSetup.ListOfSetsUnderTest;
            listOfSets[0].Theme = listOfThemes[0];
            listOfSets[0].Subtheme = listOfSubthemesUnderTest[0];
            listOfSets[1].Theme = listOfThemes[1];
            listOfSets[1].Subtheme = listOfSubthemesUnderTest[1];

            await InsertData(listOfSets);

            var allSetsForSubtheme = await _setRepository.AllForSubtheme(listOfSets[0].Theme.Name, listOfSets[0].Subtheme.Name);

            Check.That(allSetsForSubtheme).CountIs(1);
            Check.That(allSetsForSubtheme.First().SetId).IsEqualTo(listOfSets[0].SetId);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(ModelsSetup.StringEmpty)]
        public async Task AllForThemeGroup_InvalidThemeGroupName_ReturnsEmpty(string themeGroupName)
        {
            var allSetsForThemeGroup = await _setRepository.AllForThemeGroup(themeGroupName);

            Check.That(allSetsForThemeGroup).IsEmpty();
        }

        [TestMethod]
        public async Task AllForThemeGroup_ThemeGroupDoesNotExist_ReturnsEmpty()
        {
            var listOfSets = ModelsSetup.ListOfSetsUnderTest;
            listOfSets[0].ThemeGroup = await InsertData(ModelsSetup.ThemeGroupReferenceData);

            await InsertData(listOfSets);

            var allSetsForThemeGroup = await _setRepository.AllForThemeGroup($"{ModelsSetup.ThemeGroupReferenceDataValue}_NON-EXISTENT");

            Check.That(allSetsForThemeGroup).IsEmpty();
        }

        [TestMethod]
        public async Task AllForThemeGroup_ThemeGroupExists_ReturnsModels()
        {
            var listOfSets = ModelsSetup.ListOfSetsUnderTest;
            listOfSets[0].ThemeGroup = await InsertData(ModelsSetup.ThemeGroupReferenceData);

            await InsertData(listOfSets);

            var allSetsForThemeGroup = await _setRepository.AllForThemeGroup(ModelsSetup.ThemeGroupReferenceDataValue);

            Check.That(allSetsForThemeGroup).CountIs(1);
            Check.That(allSetsForThemeGroup.First().SetId).IsEqualTo(listOfSets[0].SetId);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(ModelsSetup.StringEmpty)]
        public async Task AllForCategory_InvalidCategoryName_ReturnsEmpty(string categoryName)
        {
            var allSetsForCategory = await _setRepository.AllForCategory(categoryName);

            Check.That(allSetsForCategory).IsEmpty();
        }

        [TestMethod]
        public async Task AllForCategory_CategoryDoesNotExist_ReturnsEmpty()
        {
            var category = await InsertData(ModelsSetup.CategoryReferenceData);

            var setUnderTest = ModelsSetup.GetSetUnderTest();
            setUnderTest.Category = category;

            await InsertData(setUnderTest);

            var allSetsForCategory = await _setRepository.AllForCategory($"{ModelsSetup.CategoryReferenceDataValue}_NON-EXISTENT");

            Check.That(allSetsForCategory).IsEmpty();
        }

        [TestMethod]
        public async Task AllForCategory_CategoryExists_ReturnsModels()
        {
            var listOfSets = ModelsSetup.ListOfSetsUnderTest;
            listOfSets[0].Category = await InsertData(ModelsSetup.CategoryReferenceData);

            await InsertData(listOfSets);

            var allSetsForCategory = await _setRepository.AllForCategory(ModelsSetup.CategoryReferenceDataValue);

            Check.That(allSetsForCategory).CountIs(1);
            Check.That(allSetsForCategory.First().SetId).IsEqualTo(listOfSets[0].SetId);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(ModelsSetup.StringEmpty)]
        public async Task AllForTag_InvalidTagName_ReturnsEmpty(string tagName)
        {
            var allSetsForTag = await _setRepository.AllForTag(tagName);

            Check.That(allSetsForTag).IsEmpty();
        }

        [TestMethod]
        public async Task AllForTag_TagDoesNotExist_ReturnsEmpty()
        {
            var listOfSets = ModelsSetup.ListOfSetsUnderTest;
            listOfSets[0].Tags.Add(await InsertData(ModelsSetup.TagReferenceData));

            await InsertData(listOfSets);

            var allSetsForTag = await _setRepository.AllForTag($"{ModelsSetup.TagReferenceDataValue}_NON-EXISTENT");

            Check.That(allSetsForTag).IsEmpty();
        }

        [TestMethod]
        public async Task AllForTag_TagExists_ReturnsModels()
        {
            var listOfSets = ModelsSetup.ListOfSetsUnderTest;
            listOfSets[0].Tags.Add(await InsertData(ModelsSetup.TagReferenceData));

            await InsertData(listOfSets);

            var allSetsForTag = await _setRepository.AllForTag(ModelsSetup.TagReferenceDataValue);

            Check.That(allSetsForTag).CountIs(1);
            Check.That(allSetsForTag.First().SetId).IsEqualTo(listOfSets[0].SetId);
        }

        [TestMethod]
        public async Task AllForYear_InvalidYear_ReturnsEmpty()
        {
            var allSetsForYear = await _setRepository.AllForYear(Constants.MinimumSetYear - 1);

            Check.That(allSetsForYear).IsEmpty();
        }

        [TestMethod]
        public async Task AllForYear_YearDoesNotExist_ReturnsEmpty()
        {
            var listOfSets = ModelsSetup.ListOfSetsUnderTest;
            listOfSets[0].Year = (short)Constants.MinimumSetYear;
            listOfSets[1].Year = Constants.MinimumSetYear + 1;

            await InsertData(listOfSets);

            var allSetsForYear = await _setRepository.AllForYear((short)Constants.MinimumSetYear + 2);

            Check.That(allSetsForYear).IsEmpty();
        }

        [TestMethod]
        public async Task AllForYear_YearExists_ReturnsModel()
        {
            var listOfSets = ModelsSetup.ListOfSetsUnderTest;
            listOfSets[0].Year = (short)Constants.MinimumSetYear;
            listOfSets[1].Year = Constants.MinimumSetYear + 1;

            await InsertData(listOfSets);

            var allSetsForYear = await _setRepository.AllForYear((short)Constants.MinimumSetYear);

            Check.That(allSetsForYear).CountIs(1);
            Check.That(allSetsForYear.First().SetId).IsEqualTo(listOfSets[0].SetId);
        }

        [TestMethod]
        [DataRow(-1, 0)]
        [DataRow(0, -1)]
        public async Task AllForPriceRange_InvalidPrice_ReturnsEmpty(float minPrice, float maxPrice)
        {
            var set = ModelsSetup.GetSetUnderTest();
            set.Prices.Add(new Price
            {
                Region = PriceRegion.CA,
                Value = 1
            });

            await InsertData(set);

            var allSetsForPriceRange = await _setRepository.AllForPriceRange(PriceRegion.CA, minPrice, maxPrice);

            Check.That(allSetsForPriceRange).IsEmpty();
        }

        [TestMethod]
        public async Task AllForPriceRange_RegionDoesNotExists_ReturnsEmpty()
        {
            var set = ModelsSetup.GetSetUnderTest();
            set.Prices.Add(new Price
            {
                Region = PriceRegion.CA,
                Value = 1
            });

            await InsertData(set);

            var allSetsForPriceRange = await _setRepository.AllForPriceRange(PriceRegion.DE, 0, 10);

            Check.That(allSetsForPriceRange).IsEmpty();
        }

        [TestMethod]
        public async Task AllForPriceRange_PriceDoesNotExist_ReturnsEmpty()
        {
            var set = ModelsSetup.GetSetUnderTest();
            set.Prices.Add(new Price
            {
                Region = PriceRegion.CA,
                Value = 10
            });

            await InsertData(set);

            var allSetsForPriceRange = await _setRepository.AllForPriceRange(PriceRegion.CA, 0, 5);

            Check.That(allSetsForPriceRange).IsEmpty();
        }

        [TestMethod]
        public async Task AllForPriceRange_PriceExists_ReturnsModel()
        {
            var set = ModelsSetup.GetSetUnderTest();
            set.Prices.Add(new Price
            {
                Region = PriceRegion.CA,
                Value = 1
            });

            await InsertData(set);

            var allSetsForPriceRange = await _setRepository.AllForPriceRange(PriceRegion.CA, 0, 5);

            Check.That(allSetsForPriceRange).CountIs(1);
            Check.That(allSetsForPriceRange.First().SetId).IsEqualTo(set.SetId);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("-")]
        [DataRow("a-a")]
        [DataRow("ab-ab")]
        [DataRow("a a")]
        [DataRow("ab ab")]
        public async Task SearchBy_InvalidSearchTerm_ReturnsEmpty(string searchTerm)
        {
            var searchResult = await _setRepository.SearchBy(searchTerm);

            Check.That(searchResult).IsEmpty();
        }

        [TestMethod]
        public async Task SearchBy_SearchTermDoesNotExist_ReturnsEmpty()
        {
            var setUnderTest = ModelsSetup.GetSetUnderTest();
            setUnderTest.Name = "SETUNDERTEST";

            await InsertData(setUnderTest);

            var searchResult = await _setRepository.SearchBy($"{setUnderTest.Name}_NONEXISTENT");

            Check.That(searchResult).IsEmpty();
        }

        [TestMethod]
        [DataRow("name")]
        [DataRow("number")]
        [DataRow("ean")]
        [DataRow("upc")]
        [DataRow("theme")]
        [DataRow("subtheme")]
        [DataRow("themegroup")]
        [DataRow("packagingtype")]
        [DataRow("category")]
        [DataRow("tag")]
        public async Task SearchBy_SearchTermExists_ReturnsSearchResult(string searchTerm)
        {
            var setUnderTest = await SetupSetForSearch(0);

            var searchResult = await _setRepository.SearchBy(searchTerm);

            Check.That(searchResult).CountIs(1);
            Check.That(searchResult.First().SetId).IsEqualTo(setUnderTest.SetId);
        }

        [TestMethod]
        public async Task SearchBy_MultipleSearchTerms_ReturnsSearchResult()
        {
            await SetupSetForSearch(0);
            await SetupSetForSearch(1);

            var searchResult = await _setRepository.SearchBy("tag1 subtheme0");
            Check.That(searchResult).CountIs(2);
        }

        [TestMethod]
        public async Task AddOrUpdate_NullSet_ReturnsNull()
        {
            var set = await _setRepository.AddOrUpdate(null);

            Check.That(set).IsNull();
        }

        [TestMethod]
        public async Task AddOrUpdate_InvalidSet_ReturnsNull()
        {
            var set = await _setRepository.AddOrUpdate(new Set { SetId = 0 });

            Check.That(set).IsNull();
        }

        [TestMethod]
        public async Task AddOrUpdate_NewValidSet_InsertsModel()
        {
            var setUnderTest = ModelsSetup.GetSetUnderTest();

            await _setRepository.AddOrUpdate(setUnderTest);

            var set = await _setRepository.Get(setUnderTest.SetId);

            Check.That(set).IsNotNull();
            Check.That(set.SetId).IsEqualTo(setUnderTest.SetId);
        }

        [TestMethod]
        public async Task AddOrUpdate_ExistingValidSet_UpdatesModel()
        {
            var setUnderTest = ModelsSetup.GetSetUnderTest();

            await _setRepository.AddOrUpdate(setUnderTest);

            var setFromDb = await _setRepository.Get(setUnderTest.SetId);

            setFromDb.Name = "NEW NAME";

            await _setRepository.AddOrUpdate(setFromDb);

            var set = await _setRepository.Get(setUnderTest.SetId);

            Check.That(set).HasFieldsWithSameValues(setFromDb);
        }

        [TestMethod]
        public async Task Count_SetCollectionIsEmpty_ReturnsZero()
        {
            var setCount = await _setRepository.Count();

            Check.That(setCount).IsZero();
        }

        [TestMethod]
        public async Task Count_SetCollectionIsNotEmpty_ReturnsSetCount()
        {
            var random = new Random();
            var numberOfSets = random.Next(10, 100);
            for (var setId = 1; setId <= numberOfSets; setId++)
            {
                await _setRepository.AddOrUpdate(new Set { SetId = setId });
            }

            var setCount = await _setRepository.Count();

            Check.That(setCount).Is(numberOfSets);
        }

        [TestMethod]
        public async Task DeleteMany_NullListOfSetIds_ReturnsZero()
        {
            var deletedSets = await _setRepository.DeleteMany(null);

            Check.That(deletedSets).IsZero();
        }

        [TestMethod]
        public async Task DeleteMany_EmptyListOfSetIds_ReturnsZero()
        {
            var deletedSets = await _setRepository.DeleteMany([]);

            Check.That(deletedSets).IsZero();
        }

        [TestMethod]
        public async Task DeleteMany_NonEmptyListOfSetIds_ReturnsNumberOfDeletedSets()
        {
            var random = new Random();
            var numberOfSets = random.Next(10, 100);
            var setsToDelete = new List<long>();
            for (var setId = 1; setId <= numberOfSets; setId++)
            {
                await _setRepository.AddOrUpdate(new Set { SetId = setId });

                if (setId % 3 is 0)
                {
                    setsToDelete.Add(setId);
                }
            }

            var deletedSets = await _setRepository.DeleteMany(setsToDelete);

            Check.That(deletedSets).Is(setsToDelete.Count);
        }

        private async Task<Set> SetupSetForSearch(int suffix)
        {
            var theme = await InsertData(ModelsSetup.GetThemeUnderTest($"SET THEME{suffix}"));

            var subtheme = ModelsSetup.GetSubthemeUnderTest($"SET SUBTHEME{suffix}");
            subtheme.Theme = theme;

            subtheme = await InsertData(subtheme);

            var set = ModelsSetup.GetSetForSearch(suffix);
            set.SetId = suffix;
            set.Theme = theme;
            set.Subtheme = subtheme;
            set.ThemeGroup = await InsertData(new ThemeGroup { Value = $"SET THEMEGROUP{suffix}" });
            set.PackagingType = await InsertData(new PackagingType { Value = $"SET PACKAGINGTYPE{suffix}" });
            set.Category = await InsertData(new Category { Value = $"SET CATEGORY{suffix}" });
            set.Tags.Add(await InsertData(new Tag { Value = $"SET TAG{suffix}" }));

            return await InsertData(set);
        }
    }
}
