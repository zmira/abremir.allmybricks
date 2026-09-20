using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using abremir.AllMyBricks.Data.Repositories;
using abremir.AllMyBricks.DataSynchronizer.Extensions;
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
    public class SubthemeSynchronizerTests : DataSynchronizerTestsBase
    {
        private static SubthemeRepository _subthemeRepository;
        private static ThemeRepository _themeRepository;

        public SubthemeSynchronizerTests()
        {
            _subthemeRepository = new SubthemeRepository(MemoryRepositoryService);
            _themeRepository = new ThemeRepository(MemoryRepositoryService);
        }

        [TestMethod]
        public void Synchronize_OnboardingServiceReturnsEmptyBricksetApiKey_ThrowsException()
        {
            var onboardingService = Substitute.For<IOnboardingService>();
            onboardingService.GetBricksetApiKey().Returns(string.Empty);

            var subthemeSynchronizer = CreateTarget(onboardingService);

            Check.That(subthemeSynchronizer.Synchronize()).Throws<Exception>();
        }

        [TestMethod]
        public async Task Synchronize_ThemeRepositoryReturnsEmptyListOfThemes_NothingIsSaved()
        {
            var subthemeSynchronizer = CreateTarget();

            await subthemeSynchronizer.Synchronize();

            Check.That(await _subthemeRepository.All()).IsEmpty();
        }

        [TestMethod]
        public async Task Synchronize_BricksetApiServiceReturnsEmptyListOfSubthemes_NothingIsSaved()
        {
            var testTheme = JsonSerializer.Deserialize<List<Themes>>(
                GetResultFileFromResource(Constants.JsonFileGetThemes),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value)
                .First(themes => themes.Theme is Constants.TestThemeArchitecture);
            var yearsList = JsonSerializer.Deserialize<List<Years>>(
                GetResultFileFromResource(Constants.JsonFileGetYears),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);

            var theme = testTheme.ToTheme();
            theme.SetCountPerYear = [.. yearsList.ToYearSetCountEnumerable()];

            await _themeRepository.AddOrUpdate(theme);

            var bricksetApiService = Substitute.For<IBricksetApiService>();
            bricksetApiService.GetSubthemes(Arg.Any<ParameterTheme>()).Returns([]);

            var subthemeSynchronizer = CreateTarget(bricksetApiService: bricksetApiService);

            await subthemeSynchronizer.Synchronize();

            Check.That(await _subthemeRepository.All()).IsEmpty();
        }

        [TestMethod]
        public async Task Synchronize_BricksetApiServiceReturnsListOfSubthemes_AllSubthemesAreSaved()
        {
            var testTheme = JsonSerializer.Deserialize<List<Themes>>(
                GetResultFileFromResource(Constants.JsonFileGetThemes),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value)
                .First(themes => themes.Theme is Constants.TestThemeArchitecture);
            var yearsList = JsonSerializer.Deserialize<List<Years>>(
                GetResultFileFromResource(Constants.JsonFileGetYears),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);
            var subthemesList = JsonSerializer.Deserialize<List<Subthemes>>(
                GetResultFileFromResource(Constants.JsonFileGetSubthemes),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);

            var theme = testTheme.ToTheme();
            theme.SetCountPerYear = [.. yearsList.ToYearSetCountEnumerable()];

            await _themeRepository.AddOrUpdate(theme);

            var bricksetApiService = Substitute.For<IBricksetApiService>();
            bricksetApiService.GetSubthemes(Arg.Any<ParameterTheme>()).Returns(subthemesList);

            var subthemeSynchronizer = CreateTarget(bricksetApiService: bricksetApiService);

            await subthemeSynchronizer.Synchronize();

            Check.That(await _subthemeRepository.All()).CountIs(subthemesList.Count);
        }

        private static SubthemeSynchronizer CreateTarget(IOnboardingService onboardingService = null, IBricksetApiService bricksetApiService = null)
        {
            if (onboardingService is null)
            {
                onboardingService = Substitute.For<IOnboardingService>();
                onboardingService.GetBricksetApiKey().Returns("brickset-api-key");
            }

            bricksetApiService ??= Substitute.For<IBricksetApiService>();

            return new SubthemeSynchronizer(
                onboardingService,
                bricksetApiService,
                _themeRepository,
                _subthemeRepository,
                Substitute.For<IMessageHub>());
        }
    }
}
