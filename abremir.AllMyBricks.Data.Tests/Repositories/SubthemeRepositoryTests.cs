﻿using abremir.AllMyBricks.Data.Configuration;
using abremir.AllMyBricks.Data.Interfaces;
using abremir.AllMyBricks.Data.Models;
using abremir.AllMyBricks.Data.Repositories;
using abremir.AllMyBricks.Data.Tests.Configuration;
using abremir.AllMyBricks.Data.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NFluent;
using System;
using System.Linq;

namespace abremir.AllMyBricks.Data.Tests.Repositories
{
    [TestClass]
    public class SubthemeRepositoryTests : DataTestsBase
    {
        private static ISubthemeRepository _subthemeRepository;

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            _subthemeRepository = new SubthemeRepository(MemoryRepositoryService);
        }

        [DataTestMethod]
        [DataRow(null, null)]
        [DataRow(ModelsSetup.StringEmpty, null)]
        [DataRow(null, ModelsSetup.StringEmpty)]
        [DataRow(ModelsSetup.StringEmpty, ModelsSetup.StringEmpty)]
        public void Get_InvalidParameters_ReturnsNull(string themeName, string subthemeName)
        {
            var subtheme = _subthemeRepository.Get(themeName, subthemeName);

            Check.That(subtheme).IsNull();
        }

        [DataTestMethod]
        [DataRow(ModelsSetup.ThemeUnderTestName, ModelsSetup.NonExistentSubthemeName)]
        [DataRow(ModelsSetup.NonExistentThemeName, ModelsSetup.SubthemeUnderTestName)]
        public void Get_SubthemeDoesNotExist_ReturnsNull(string themeName, string subthemeName)
        {
            var subthemeUnderTest = ModelsSetup.GetSubthemeUnderTest(subthemeName == ModelsSetup.SubthemeUnderTestName ? ModelsSetup.SubthemeUnderTestName : Guid.NewGuid().ToString());
            subthemeUnderTest.Theme = InsertData(ModelsSetup.GetThemeUnderTest(themeName == ModelsSetup.ThemeUnderTestName ? themeName : Guid.NewGuid().ToString()));

            InsertData(subthemeUnderTest);

            var subtheme = _subthemeRepository.Get(themeName, subthemeName);

            Check.That(subtheme).IsNull();
        }

        [TestMethod]
        public void Get_SubthemeExists_ReturnModel()
        {
            var subthemeUnderTest = ModelsSetup.GetSubthemeUnderTest(Guid.NewGuid().ToString());

            InsertData(subthemeUnderTest.Theme);
            InsertData(subthemeUnderTest);

            var subtheme = _subthemeRepository.Get(subthemeUnderTest.Theme.Name, subthemeUnderTest.Name);

            Check.That(subtheme).IsNotNull();
            Check.That(subtheme.Name).IsEqualTo(subthemeUnderTest.Name);
        }

        [TestMethod]
        public void All_NoSubthemes_ReturnsEmpty()
        {
            var allSubthemes = _subthemeRepository.All();

            Check.That(allSubthemes).IsEmpty();
        }

        [TestMethod]
        public void All_HasSubthemes_ReturnsModels()
        {
            var listOfThemesUnderTest = InsertData(ModelsSetup.ListOfThemesUnderTest);

            var listOfSubthemesUnderTest = ModelsSetup.ListOfSubthemesUnderTest;
            listOfSubthemesUnderTest[0].Theme = listOfThemesUnderTest[0];
            listOfSubthemesUnderTest[1].Theme = listOfThemesUnderTest[0];

            InsertData(listOfSubthemesUnderTest);

            var allSubthemes = _subthemeRepository.All();

            Check.That(allSubthemes.Select(subtheme => subtheme.Name)).IsEquivalentTo(listOfSubthemesUnderTest.Select(subtheme => subtheme.Name));
        }

        [TestMethod]
        public void AllForYear_NoSubthemesForYear_ReturnsEmpty()
        {
            var listOfThemesUnderTest = InsertData(ModelsSetup.ListOfThemesUnderTest);

            var listOfSubthemesUnderTest = ModelsSetup.ListOfSubthemesUnderTest;
            listOfSubthemesUnderTest[0].Theme = listOfThemesUnderTest[0];
            listOfSubthemesUnderTest[1].Theme = listOfThemesUnderTest[0];

            InsertData(listOfSubthemesUnderTest);

            var allSubthemesForYear = _subthemeRepository.AllForYear(ModelsSetup.FirstThemeYearTo + 1);

            Check.That(allSubthemesForYear).IsEmpty();
        }

        [TestMethod]
        public void AllForYear_YearIsLessThanMinimumConstant_ReturnsEmpty()
        {
            var listOfThemesUnderTest = InsertData(ModelsSetup.ListOfThemesUnderTest);

            var listOfSubthemesUnderTest = ModelsSetup.ListOfSubthemesUnderTest;
            listOfSubthemesUnderTest[0].Theme = listOfThemesUnderTest[0];
            listOfSubthemesUnderTest[1].Theme = listOfThemesUnderTest[0];

            InsertData(listOfSubthemesUnderTest);

            var allSubthemesForYear = _subthemeRepository.AllForYear(Constants.MinimumSetYear - 1);

            Check.That(allSubthemesForYear).IsEmpty();
        }

        [DataTestMethod]
        [DataRow(ModelsSetup.FirstSubthemeYearFrom, 1)]
        [DataRow(ModelsSetup.SecondSubthemeYearFrom, 2)]
        public void AllForYear_HasSubthemesForYear_ReturnsModels(short year, int expectedCount)
        {
            var listOfThemesUnderTest = InsertData(ModelsSetup.ListOfThemesUnderTest);

            var listOfSubthemesUnderTest = ModelsSetup.ListOfSubthemesUnderTest;
            listOfSubthemesUnderTest[0].Theme = listOfThemesUnderTest[0];
            listOfSubthemesUnderTest[1].Theme = listOfThemesUnderTest[0];

            InsertData(listOfSubthemesUnderTest);

            var allSubthemesForYear = _subthemeRepository.AllForYear(year);

            Check.That(allSubthemesForYear).CountIs(expectedCount);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow(ModelsSetup.StringEmpty)]
        public void AllForTheme_InvalidThemeName_ReturnsEmpty(string themeName)
        {
            var listOfThemesUnderTest = InsertData(ModelsSetup.ListOfThemesUnderTest);

            var listOfSubthemesUnderTest = ModelsSetup.ListOfSubthemesUnderTest;
            listOfSubthemesUnderTest[0].Theme = listOfThemesUnderTest[0];
            listOfSubthemesUnderTest[1].Theme = listOfThemesUnderTest[0];

            InsertData(listOfSubthemesUnderTest);

            var allSubthemesForTheme = _subthemeRepository.AllForTheme(themeName);

            Check.That(allSubthemesForTheme).IsEmpty();
        }

        [TestMethod]
        public void AllForTheme_NoSubthemesForTheme_ReturnsEmpty()
        {
            var listOfThemesUnderTest = InsertData(ModelsSetup.ListOfThemesUnderTest);

            var listOfSubthemesUnderTest = ModelsSetup.ListOfSubthemesUnderTest;
            listOfSubthemesUnderTest[0].Theme = listOfThemesUnderTest[0];
            listOfSubthemesUnderTest[1].Theme = listOfThemesUnderTest[0];

            InsertData(listOfSubthemesUnderTest);

            var allSubthemesForTheme = _subthemeRepository.AllForTheme(ModelsSetup.NonExistentThemeName);

            Check.That(allSubthemesForTheme).IsEmpty();
        }

        [TestMethod]
        public void AllForTheme_HasSubthemesForTheme_ReturnsModels()
        {
            var listOfThemesUnderTest = InsertData(ModelsSetup.ListOfThemesUnderTest);

            var listOfSubthemesUnderTest = ModelsSetup.ListOfSubthemesUnderTest;
            listOfSubthemesUnderTest[0].Theme = listOfThemesUnderTest[0];
            listOfSubthemesUnderTest[1].Theme = listOfThemesUnderTest[0];

            InsertData(listOfSubthemesUnderTest);

            var allSubthemesForTheme = _subthemeRepository.AllForTheme(listOfThemesUnderTest[0].Name);

            Check.That(allSubthemesForTheme).CountIs(listOfSubthemesUnderTest.Length);
        }

        [TestMethod]
        public void AddOrUpdate_NullSubtheme_ReturnsNull()
        {
            Subtheme subthemeUnderTest = null;

            var subtheme = _subthemeRepository.AddOrUpdate(subthemeUnderTest);

            Check.That(subtheme).IsNull();
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow(ModelsSetup.StringEmpty)]
        public void AddOrUpdate_InvalidSubtheme_ReturnsNull(string subthemeName)
        {
            var subthemeUnderTest = new Subtheme { Name = subthemeName };

            var subtheme = _subthemeRepository.AddOrUpdate(subthemeUnderTest);

            Check.That(subtheme).IsNull();
        }

        [TestMethod]
        public void AddOrUpdate_NullTheme_ReturnsNull()
        {
            var subthemeUnderTest = new Subtheme { Name = ModelsSetup.NonExistentSubthemeName };

            var subtheme = _subthemeRepository.AddOrUpdate(subthemeUnderTest);

            Check.That(subtheme).IsNull();
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow(ModelsSetup.StringEmpty)]
        public void AddOrUpdate_InvalidTheme_ReturnsNull(string themeName)
        {
            var subthemeUnderTest = new Subtheme { Name = ModelsSetup.NonExistentSubthemeName, Theme = new Theme { Name = themeName } };

            var subtheme = _subthemeRepository.AddOrUpdate(subthemeUnderTest);

            Check.That(subtheme).IsNull();
        }

        [TestMethod]
        public void AddOrUpdate_SubthemeYearFromIsLessThanMinimumConstant_ReturnsNull()
        {
            var subthemeUnderTest = ModelsSetup.GetSubthemeUnderTest(Guid.NewGuid().ToString());
            subthemeUnderTest.YearFrom = Constants.MinimumSetYear - 1;

            var subtheme = _subthemeRepository.AddOrUpdate(subthemeUnderTest);

            Check.That(subtheme).IsNull();
        }

        [TestMethod]
        public void AddOrUpdate_ThemeYearFromIsLessThanMinimumConstant_ReturnsNull()
        {
            var subthemeUnderTest = ModelsSetup.GetSubthemeUnderTest(Guid.NewGuid().ToString());
            subthemeUnderTest.Theme.YearFrom = Constants.MinimumSetYear - 1;

            var subtheme = _subthemeRepository.AddOrUpdate(subthemeUnderTest);

            Check.That(subtheme).IsNull();
        }

        [TestMethod]
        public void AddOrUpdate_NewValidSubtheme_InsertsModel()
        {
            var subthemeUnderTest = ModelsSetup.GetSubthemeUnderTest(Guid.NewGuid().ToString());

            InsertData(subthemeUnderTest.Theme);

            _subthemeRepository.AddOrUpdate(subthemeUnderTest);

            var subtheme = _subthemeRepository.Get(subthemeUnderTest.Theme.Name, subthemeUnderTest.Name);

            Check.That(subtheme).IsNotNull();
            Check.That(subtheme.Name).IsEqualTo(subthemeUnderTest.Name);
        }

        [TestMethod]
        public void AddOrUpdate_ExistingValidSubtheme_UpdatesModel()
        {
            var subtheme = ModelsSetup.GetSubthemeUnderTest(Guid.NewGuid().ToString());

            InsertData(subtheme.Theme);

            _subthemeRepository.AddOrUpdate(subtheme);

            var subthemeUnderTest = _subthemeRepository.Get(subtheme.Theme.Name, subtheme.Name);

            subthemeUnderTest.SetCount = 66;
            subthemeUnderTest.YearTo = 2099;

            _subthemeRepository.AddOrUpdate(subthemeUnderTest);

            var savedSubtheme = _subthemeRepository.Get(subtheme.Theme.Name, subtheme.Name);

            Check.That(savedSubtheme).IsNotNull();
            Check.That(savedSubtheme.SetCount).IsEqualTo(subthemeUnderTest.SetCount);
            Check.That(savedSubtheme.YearTo).IsEqualTo(subthemeUnderTest.YearTo);
        }
    }
}
