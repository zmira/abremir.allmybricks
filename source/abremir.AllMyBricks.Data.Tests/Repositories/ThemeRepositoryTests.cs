using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using abremir.AllMyBricks.Data.Configuration;
using abremir.AllMyBricks.Data.Models;
using abremir.AllMyBricks.Data.Repositories;
using abremir.AllMyBricks.Data.Tests.Configuration;
using abremir.AllMyBricks.Data.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NFluent;

namespace abremir.AllMyBricks.Data.Tests.Repositories
{
    [TestClass]
    public class ThemeRepositoryTests : DataTestsBase
    {
        private static ThemeRepository _themeRepository;

        public ThemeRepositoryTests()
        {
            _themeRepository = new ThemeRepository(MemoryRepositoryService);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(ModelsSetup.StringEmpty)]
        public async Task Get_InvalidThemeName_ReturnsNull(string themeName)
        {
            var theme = await _themeRepository.Get(themeName);

            Check.That(theme).IsNull();
        }

        [TestMethod]
        public async Task Get_ThemeDoesNotExist_ReturnsNull()
        {
            await InsertData(ModelsSetup.GetThemeUnderTest(Guid.NewGuid().ToString()));

            var theme = await _themeRepository.Get(ModelsSetup.NonExistentThemeName);

            Check.That(theme).IsNull();
        }

        [TestMethod]
        public async Task Get_ThemeExists_ReturnsModel()
        {
            var themeUnderTest = await InsertData(ModelsSetup.GetThemeUnderTest(Guid.NewGuid().ToString()));

            var theme = await _themeRepository.Get(themeUnderTest.Name);

            Check.That(theme.Name).IsEqualTo(themeUnderTest.Name);
        }

        [TestMethod]
        public async Task All_NoThemes_ReturnsEmpty()
        {
            var allThemes = await _themeRepository.All();

            Check.That(allThemes).IsEmpty();
        }

        [TestMethod]
        public async Task All_HasThemes_ReturnsModels()
        {
            var listOfThemesUnderTest = await InsertData(ModelsSetup.ListOfThemesUnderTest);

            var allThemes = await _themeRepository.All();

            Check.That(allThemes.Select(theme => theme.Name)).IsEquivalentTo(listOfThemesUnderTest.Select(theme => theme.Name));
        }

        [TestMethod]
        public async Task AllForYear_YearIsLessThanMinimumConstant_ReturnsEmpty()
        {
            await InsertData(ModelsSetup.ListOfThemesUnderTest);

            var allThemesForYear = await _themeRepository.AllForYear(Constants.MinimumSetYear - 1);

            Check.That(allThemesForYear).IsEmpty();
        }

        [TestMethod]
        public async Task AllForYear_NoThemesForYear_ReturnsEmpty()
        {
            await InsertData(ModelsSetup.ListOfThemesUnderTest);

            var allThemesForYear = await _themeRepository.AllForYear(ModelsSetup.SecondThemeYearTo + 1);

            Check.That(allThemesForYear).IsEmpty();
        }

        [TestMethod]
        [DataRow(ModelsSetup.FirstThemeYearTo, 2)]
        [DataRow(ModelsSetup.SecondThemeYearTo, 1)]
        public async Task AllForYear_HasThemesForYear_ReturnsModels(short year, int expectedCount)
        {
            await InsertData(ModelsSetup.ListOfThemesUnderTest);

            List<Theme> allThemesForYear = [.. (await _themeRepository.AllForYear(year))];

            Check.That(allThemesForYear).CountIs(expectedCount);
        }

        [TestMethod]
        public async Task AddOrUpdate_NullTheme_ReturnsNull()
        {
            var theme = await _themeRepository.AddOrUpdate(null);

            Check.That(theme).IsNull();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(ModelsSetup.StringEmpty)]
        public async Task AddOrUpdate_InvalidTheme_ReturnsNull(string themeName)
        {
            var theme = await _themeRepository.AddOrUpdate(new Theme { Name = themeName });

            Check.That(theme).IsNull();
        }

        [TestMethod]
        public async Task AddOrUpdate_ThemeYearFromIsLessThanMinimumConstant_ReturnsNull()
        {
            var themeUnderTest = ModelsSetup.GetThemeUnderTest(Guid.NewGuid().ToString());
            themeUnderTest.YearFrom = Constants.MinimumSetYear - 1;

            var theme = await _themeRepository.AddOrUpdate(themeUnderTest);

            Check.That(theme).IsNull();
        }

        [TestMethod]
        public async Task AddOrUpdate_NewValidTheme_InsertsModel()
        {
            var themeUnderTest = ModelsSetup.GetThemeUnderTest(Guid.NewGuid().ToString());

            await _themeRepository.AddOrUpdate(themeUnderTest);

            var theme = await _themeRepository.Get(themeUnderTest.Name);

            Check.That(theme.Name).IsEqualTo(themeUnderTest.Name);
        }

        [TestMethod]
        public async Task AddOrUpdate_ExistingValidTheme_UpdatesModel()
        {
            var themeUnderTest = ModelsSetup.GetThemeUnderTest(Guid.NewGuid().ToString());

            await _themeRepository.AddOrUpdate(themeUnderTest);

            var themeFromDb = await _themeRepository.Get(themeUnderTest.Name);

            themeFromDb.SetCount = 99;
            themeFromDb.YearTo = 2099;

            await _themeRepository.AddOrUpdate(themeFromDb);

            var theme = await _themeRepository.Get(themeFromDb.Name);

            Check.That(theme).HasFieldsWithSameValues(themeFromDb);
        }

        [TestMethod]
        public async Task DeleteMany_NullListOfThemeNames_ReturnsZero()
        {
            var deletedThemes = await _themeRepository.DeleteMany(null);

            Check.That(deletedThemes).IsZero();
        }

        [TestMethod]
        public async Task DeleteMany_EmptyListOfThemeNames_ReturnsZero()
        {
            var deletedThemes = await _themeRepository.DeleteMany([]);

            Check.That(deletedThemes).IsZero();
        }

        [TestMethod]
        public async Task DeleteMany_NonEmptyListOfThemeNames_ReturnsNumberOfDeletedSets()
        {
            var random = new Random();
            var numberOfThemes = random.Next(10, 100);
            var themesToDelete = new List<string>();
            for (var themeId = 1; themeId <= numberOfThemes; themeId++)
            {
                await _themeRepository.AddOrUpdate(new Theme { Id = themeId, Name = themeId.ToString(), YearFrom = (short)DateTime.Now.Year });

                if (themeId % 3 is 0)
                {
                    themesToDelete.Add(themeId.ToString());
                }
            }

            var deletedThemes = await _themeRepository.DeleteMany(themesToDelete);

            Check.That(deletedThemes).Is(themesToDelete.Count);
        }
    }
}
