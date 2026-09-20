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
    public class SubthemeRepositoryTests : DataTestsBase
    {
        private static SubthemeRepository _subthemeRepository;

        public SubthemeRepositoryTests()
        {
            _subthemeRepository = new SubthemeRepository(MemoryRepositoryService);
        }

        [TestMethod]
        [DataRow(null, null)]
        [DataRow(ModelsSetup.StringEmpty, null)]
        [DataRow(null, ModelsSetup.StringEmpty)]
        [DataRow(ModelsSetup.StringEmpty, ModelsSetup.StringEmpty)]
        public async Task Get_InvalidParameters_ReturnsNull(string themeName, string subthemeName)
        {
            var subtheme = await _subthemeRepository.Get(themeName, subthemeName);

            Check.That(subtheme).IsNull();
        }

        [TestMethod]
        [DataRow(ModelsSetup.ThemeUnderTestName, ModelsSetup.NonExistentSubthemeName)]
        [DataRow(ModelsSetup.NonExistentThemeName, ModelsSetup.SubthemeUnderTestName)]
        public async Task Get_SubthemeDoesNotExist_ReturnsNull(string themeName, string subthemeName)
        {
            var subthemeUnderTest = ModelsSetup.GetSubthemeUnderTest(subthemeName is ModelsSetup.SubthemeUnderTestName ? ModelsSetup.SubthemeUnderTestName : Guid.NewGuid().ToString());
            subthemeUnderTest.Theme = await InsertData(ModelsSetup.GetThemeUnderTest(themeName is ModelsSetup.ThemeUnderTestName ? themeName : Guid.NewGuid().ToString()));

            await InsertData(subthemeUnderTest);

            var subtheme = await _subthemeRepository.Get(themeName, subthemeName);

            Check.That(subtheme).IsNull();
        }

        [TestMethod]
        public async Task Get_SubthemeExists_ReturnModel()
        {
            var subthemeUnderTest = ModelsSetup.GetSubthemeUnderTest(Guid.NewGuid().ToString());

            await InsertData(subthemeUnderTest.Theme);
            await InsertData(subthemeUnderTest);

            var subtheme = await _subthemeRepository.Get(subthemeUnderTest.Theme.Name, subthemeUnderTest.Name);

            Check.That(subtheme).IsNotNull();
            Check.That(subtheme.Name).IsEqualTo(subthemeUnderTest.Name);
        }

        [TestMethod]
        public async Task All_NoSubthemes_ReturnsEmpty()
        {
            var allSubthemes = await _subthemeRepository.All();

            Check.That(allSubthemes).IsEmpty();
        }

        [TestMethod]
        public async Task All_HasSubthemes_ReturnsModels()
        {
            var listOfThemesUnderTest = await InsertData(ModelsSetup.ListOfThemesUnderTest);

            var listOfSubthemesUnderTest = ModelsSetup.ListOfSubthemesUnderTest;
            listOfSubthemesUnderTest[0].Theme = listOfThemesUnderTest[0];
            listOfSubthemesUnderTest[1].Theme = listOfThemesUnderTest[0];

            await InsertData(listOfSubthemesUnderTest);

            var allSubthemes = await _subthemeRepository.All();

            Check.That(allSubthemes.Select(subtheme => subtheme.Name)).IsEquivalentTo(listOfSubthemesUnderTest.Select(subtheme => subtheme.Name));
        }

        [TestMethod]
        public async Task AllForYear_NoSubthemesForYear_ReturnsEmpty()
        {
            var listOfThemesUnderTest = await InsertData(ModelsSetup.ListOfThemesUnderTest);

            var listOfSubthemesUnderTest = ModelsSetup.ListOfSubthemesUnderTest;
            listOfSubthemesUnderTest[0].Theme = listOfThemesUnderTest[0];
            listOfSubthemesUnderTest[1].Theme = listOfThemesUnderTest[0];

            await InsertData(listOfSubthemesUnderTest);

            var allSubthemesForYear = await _subthemeRepository.AllForYear(ModelsSetup.FirstThemeYearTo + 1);

            Check.That(allSubthemesForYear).IsEmpty();
        }

        [TestMethod]
        public async Task AllForYear_YearIsLessThanMinimumConstant_ReturnsEmpty()
        {
            var listOfThemesUnderTest = await InsertData(ModelsSetup.ListOfThemesUnderTest);

            var listOfSubthemesUnderTest = ModelsSetup.ListOfSubthemesUnderTest;
            listOfSubthemesUnderTest[0].Theme = listOfThemesUnderTest[0];
            listOfSubthemesUnderTest[1].Theme = listOfThemesUnderTest[0];

            await InsertData(listOfSubthemesUnderTest);

            var allSubthemesForYear = await _subthemeRepository.AllForYear(Constants.MinimumSetYear - 1);

            Check.That(allSubthemesForYear).IsEmpty();
        }

        [TestMethod]
        [DataRow(ModelsSetup.FirstSubthemeYearFrom, 1)]
        [DataRow(ModelsSetup.SecondSubthemeYearFrom, 2)]
        public async Task AllForYear_HasSubthemesForYear_ReturnsModels(short year, int expectedCount)
        {
            var listOfThemesUnderTest = await InsertData(ModelsSetup.ListOfThemesUnderTest);

            var listOfSubthemesUnderTest = ModelsSetup.ListOfSubthemesUnderTest;
            listOfSubthemesUnderTest[0].Theme = listOfThemesUnderTest[0];
            listOfSubthemesUnderTest[1].Theme = listOfThemesUnderTest[0];

            await InsertData(listOfSubthemesUnderTest);

            var allSubthemesForYear = await _subthemeRepository.AllForYear(year);

            Check.That(allSubthemesForYear).CountIs(expectedCount);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(ModelsSetup.StringEmpty)]
        public async Task AllForTheme_InvalidThemeName_ReturnsEmpty(string themeName)
        {
            var listOfThemesUnderTest = await InsertData(ModelsSetup.ListOfThemesUnderTest);

            var listOfSubthemesUnderTest = ModelsSetup.ListOfSubthemesUnderTest;
            listOfSubthemesUnderTest[0].Theme = listOfThemesUnderTest[0];
            listOfSubthemesUnderTest[1].Theme = listOfThemesUnderTest[0];

            await InsertData(listOfSubthemesUnderTest);

            var allSubthemesForTheme = await _subthemeRepository.AllForTheme(themeName);

            Check.That(allSubthemesForTheme).IsEmpty();
        }

        [TestMethod]
        public async Task AllForTheme_NoSubthemesForTheme_ReturnsEmpty()
        {
            var listOfThemesUnderTest = await InsertData(ModelsSetup.ListOfThemesUnderTest);

            var listOfSubthemesUnderTest = ModelsSetup.ListOfSubthemesUnderTest;
            listOfSubthemesUnderTest[0].Theme = listOfThemesUnderTest[0];
            listOfSubthemesUnderTest[1].Theme = listOfThemesUnderTest[0];

            await InsertData(listOfSubthemesUnderTest);

            var allSubthemesForTheme = await _subthemeRepository.AllForTheme(ModelsSetup.NonExistentThemeName);

            Check.That(allSubthemesForTheme).IsEmpty();
        }

        [TestMethod]
        public async Task AllForTheme_HasSubthemesForTheme_ReturnsModels()
        {
            var listOfThemesUnderTest = await InsertData(ModelsSetup.ListOfThemesUnderTest);

            var listOfSubthemesUnderTest = ModelsSetup.ListOfSubthemesUnderTest;
            listOfSubthemesUnderTest[0].Theme = listOfThemesUnderTest[0];
            listOfSubthemesUnderTest[1].Theme = listOfThemesUnderTest[0];

            await InsertData(listOfSubthemesUnderTest);

            var allSubthemesForTheme = await _subthemeRepository.AllForTheme(listOfThemesUnderTest[0].Name);

            Check.That(allSubthemesForTheme).CountIs(listOfSubthemesUnderTest.Length);
        }

        [TestMethod]
        public async Task AddOrUpdate_NullSubtheme_ReturnsNull()
        {
            Subtheme subthemeUnderTest = null;

            var subtheme = await _subthemeRepository.AddOrUpdate(subthemeUnderTest);

            Check.That(subtheme).IsNull();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(ModelsSetup.StringEmpty)]
        public async Task AddOrUpdate_InvalidSubtheme_ReturnsNull(string subthemeName)
        {
            var subthemeUnderTest = new Subtheme { Name = subthemeName };

            var subtheme = await _subthemeRepository.AddOrUpdate(subthemeUnderTest);

            Check.That(subtheme).IsNull();
        }

        [TestMethod]
        public async Task AddOrUpdate_NullTheme_ReturnsNull()
        {
            var subthemeUnderTest = new Subtheme { Name = ModelsSetup.NonExistentSubthemeName };

            var subtheme = await _subthemeRepository.AddOrUpdate(subthemeUnderTest);

            Check.That(subtheme).IsNull();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(ModelsSetup.StringEmpty)]
        public async Task AddOrUpdate_InvalidTheme_ReturnsNull(string themeName)
        {
            var subthemeUnderTest = new Subtheme { Name = ModelsSetup.NonExistentSubthemeName, Theme = new Theme { Name = themeName } };

            var subtheme = await _subthemeRepository.AddOrUpdate(subthemeUnderTest);

            Check.That(subtheme).IsNull();
        }

        [TestMethod]
        public async Task AddOrUpdate_SubthemeYearFromIsLessThanMinimumConstant_ReturnsNull()
        {
            var subthemeUnderTest = ModelsSetup.GetSubthemeUnderTest(Guid.NewGuid().ToString());
            subthemeUnderTest.YearFrom = Constants.MinimumSetYear - 1;

            var subtheme = await _subthemeRepository.AddOrUpdate(subthemeUnderTest);

            Check.That(subtheme).IsNull();
        }

        [TestMethod]
        public async Task AddOrUpdate_ThemeYearFromIsLessThanMinimumConstant_ReturnsNull()
        {
            var subthemeUnderTest = ModelsSetup.GetSubthemeUnderTest(Guid.NewGuid().ToString());
            subthemeUnderTest.Theme.YearFrom = Constants.MinimumSetYear - 1;

            var subtheme = await _subthemeRepository.AddOrUpdate(subthemeUnderTest);

            Check.That(subtheme).IsNull();
        }

        [TestMethod]
        public async Task AddOrUpdate_NewValidSubtheme_InsertsModel()
        {
            var subthemeUnderTest = ModelsSetup.GetSubthemeUnderTest(Guid.NewGuid().ToString());

            await InsertData(subthemeUnderTest.Theme);

            await _subthemeRepository.AddOrUpdate(subthemeUnderTest);

            var subtheme = await _subthemeRepository.Get(subthemeUnderTest.Theme.Name, subthemeUnderTest.Name);

            Check.That(subtheme).IsNotNull();
            Check.That(subtheme.Name).IsEqualTo(subthemeUnderTest.Name);
        }

        [TestMethod]
        public async Task AddOrUpdate_ExistingValidSubtheme_UpdatesModel()
        {
            var subtheme = ModelsSetup.GetSubthemeUnderTest(Guid.NewGuid().ToString());

            await InsertData(subtheme.Theme);

            await _subthemeRepository.AddOrUpdate(subtheme);

            var subthemeUnderTest = await _subthemeRepository.Get(subtheme.Theme.Name, subtheme.Name);

            subthemeUnderTest.SetCount = 66;
            subthemeUnderTest.YearTo = 2099;

            await _subthemeRepository.AddOrUpdate(subthemeUnderTest);

            var savedSubtheme = await _subthemeRepository.Get(subtheme.Theme.Name, subtheme.Name);

            Check.That(savedSubtheme).IsNotNull();
            Check.That(savedSubtheme.SetCount).IsEqualTo(subthemeUnderTest.SetCount);
            Check.That(savedSubtheme.YearTo).IsEqualTo(subthemeUnderTest.YearTo);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public async Task DeleteMany_InvalidThemeName_ReturnsZero(string themeName)
        {
            var deletedSubthemes = await _subthemeRepository.DeleteMany(themeName, ["subtheme-name"]);

            Check.That(deletedSubthemes).IsZero();
        }

        [TestMethod]
        public async Task DeleteMany_NullListOfSubthemeNames_ReturnsZero()
        {
            var deletedSubthemes = await _subthemeRepository.DeleteMany("theme-name", null);

            Check.That(deletedSubthemes).IsZero();
        }

        [TestMethod]
        public async Task DeleteMany_EmptyListOfSubthemeNames_ReturnsZero()
        {
            var deletedSubthemes = await _subthemeRepository.DeleteMany("theme-name", []);

            Check.That(deletedSubthemes).IsZero();
        }

        [TestMethod]
        public async Task DeleteMany_NonEmptyListOfSubthemeNames_ReturnsNumberOfDeletedSets()
        {
            var theme = ModelsSetup.GetThemeUnderTest(Guid.NewGuid().ToString());

            await InsertData(theme);

            var random = new Random();
            var numberOfSubthemes = random.Next(10, 100);
            var subthemesToDelete = new List<string>();
            for (var subthemeId = 1; subthemeId <= numberOfSubthemes; subthemeId++)
            {
                await _subthemeRepository.AddOrUpdate(new Subtheme { Id = subthemeId, Name = subthemeId.ToString(), Theme = theme, YearFrom = (short)DateTime.Now.Year });

                if (subthemeId % 3 is 0)
                {
                    subthemesToDelete.Add(subthemeId.ToString());
                }
            }

            var deletedSubthemes = await _subthemeRepository.DeleteMany(theme.Name, subthemesToDelete);

            Check.That(deletedSubthemes).Is(subthemesToDelete.Count);
        }
    }
}
