using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using abremir.AllMyBricks.Data.Repositories;
using abremir.AllMyBricks.DataSynchronizer.Synchronizers;
using abremir.AllMyBricks.DataSynchronizer.Tests.Configuration;
using abremir.AllMyBricks.DataSynchronizer.Tests.Shared;
using abremir.AllMyBricks.Onboarding.Interfaces;
using abremir.AllMyBricks.ThirdParty.Brickset.Interfaces;
using abremir.AllMyBricks.ThirdParty.Brickset.Models;
using abremir.AllMyBricks.ThirdParty.Brickset.Models.Parameters;
using Easy.MessageHub;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NFluent;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace abremir.AllMyBricks.DataSynchronizer.Tests.Synchronizers
{
    [TestClass]
    public class ThemeSynchronizerTests : DataSynchronizerTestsBase
    {
        private static ThemeRepository _themeRepository;

        public ThemeSynchronizerTests()
        {
            _themeRepository = new ThemeRepository(MemoryRepositoryService);
        }

        [TestMethod]
        public void Synchronize_OnboardingServiceReturnsEmptyBricksetApiKey_ThrowsException()
        {
            var onboardingService = Substitute.For<IOnboardingService>();
            onboardingService.GetBricksetApiKey().Returns(string.Empty);

            var themeSynchronizer = CreateTarget(onboardingService);

            Check.That(themeSynchronizer.Synchronize()).Throws<Exception>();
        }

        [TestMethod]
        public async Task Synchronize_BricksetApiServiceReturnEmptyListOfThemes_NothingIsSaved()
        {
            var bricksetApiService = Substitute.For<IBricksetApiService>();
            bricksetApiService.GetThemes(Arg.Any<ParameterApiKey>()).Returns([]);

            var themeSynchronizer = CreateTarget(bricksetApiService: bricksetApiService);

            await themeSynchronizer.Synchronize();

            Check.That(await _themeRepository.All()).IsEmpty();
        }

        [TestMethod]
        public async Task Synchronize_BricksetApiServiceReturnsListOfThemes_AllThemesAreSaved()
        {
            var themesList = JsonSerializer.Deserialize<List<Themes>>(
                GetResultFileFromResource(Constants.JsonFileGetThemes),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);
            var yearsList = JsonSerializer.Deserialize<List<Years>>(
                GetResultFileFromResource(Constants.JsonFileGetYears),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);

            var bricksetApiService = Substitute.For<IBricksetApiService>();
            bricksetApiService.GetThemes(Arg.Any<ParameterApiKey>()).Returns(themesList);
            bricksetApiService.GetYears(Arg.Is<ParameterTheme>(parameter => parameter.Theme == Constants.TestThemeArchitecture)).Returns(yearsList);

            var themeSynchronizer = CreateTarget(bricksetApiService: bricksetApiService);

            await themeSynchronizer.Synchronize();

            Check.That(await _themeRepository.All()).CountIs(themesList.Count);
            Check.That((await _themeRepository.Get(Constants.TestThemeArchitecture)).SetCountPerYear).Not.IsEmpty();
        }

        private static ThemeSynchronizer CreateTarget(IOnboardingService onboardingService = null, IBricksetApiService bricksetApiService = null)
        {
            if (onboardingService is null)
            {
                onboardingService = Substitute.For<IOnboardingService>();
                onboardingService.GetBricksetApiKey().Returns("brickset-api-key");
            }

            bricksetApiService ??= Substitute.For<IBricksetApiService>();

            return new ThemeSynchronizer(onboardingService, bricksetApiService, _themeRepository, Substitute.For<IMessageHub>());
        }
    }
}
