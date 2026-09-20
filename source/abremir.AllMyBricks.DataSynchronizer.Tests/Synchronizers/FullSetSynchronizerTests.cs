using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using abremir.AllMyBricks.Data.Interfaces;
using abremir.AllMyBricks.Data.Repositories;
using abremir.AllMyBricks.DataSynchronizer.Extensions;
using abremir.AllMyBricks.DataSynchronizer.Interfaces;
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
    public class FullSetSynchronizerTests : DataSynchronizerTestsBase
    {
        private static SetRepository _setRepository;
        private static ThemeRepository _themeRepository;
        private static SubthemeRepository _subthemeRepository;

        public FullSetSynchronizerTests()
        {
            _setRepository = new SetRepository(MemoryRepositoryService);
            _themeRepository = new ThemeRepository(MemoryRepositoryService);
            _subthemeRepository = new SubthemeRepository(MemoryRepositoryService);
        }

        [TestMethod]
        public async Task Synchronize_InsightsRepositoryReturnsDataSynchronizationTimestamp_Returns()
        {
            var insightsRepository = Substitute.For<IInsightsRepository>();
            insightsRepository.GetDataSynchronizationTimestamp().Returns(DateTimeOffset.Now);
            var onboardingService = Substitute.For<IOnboardingService>();
            var bricksetApiService = Substitute.For<IBricksetApiService>();

            var partialSetSynchronizer = CreateTarget(insightsRepository, onboardingService, bricksetApiService);

            await partialSetSynchronizer.Synchronize();

            await onboardingService.DidNotReceive().GetBricksetApiKey();
            await bricksetApiService.DidNotReceive().GetSets(Arg.Any<GetSetsParameters>());
        }

        [TestMethod]
        public void Synchronize_OnboardingServiceReturnsEmptyBricksetApiKey_ThrowsException()
        {
            var onboardingService = Substitute.For<IOnboardingService>();
            onboardingService.GetBricksetApiKey().Returns(string.Empty);

            var partialSetSynchronizer = CreateTarget(onboardingService: onboardingService);

            Check.That(partialSetSynchronizer.Synchronize()).Throws<Exception>();
        }

        [TestMethod]
        public async Task Synchronize_ThemeRepositoryReturnsEmptyListOfThemes_NothingIsSaved()
        {
            var bricksetApiService = Substitute.For<IBricksetApiService>();

            var subthemeSynchronizer = CreateTarget(bricksetApiService: bricksetApiService);

            await subthemeSynchronizer.Synchronize();

            await bricksetApiService.DidNotReceive().GetSets(Arg.Any<GetSetsParameters>());
            Check.That(await _setRepository.All()).IsEmpty();
        }

        [TestMethod]
        public async Task Synchronize_BricksetApiServiceReturnsEmptyListOfSets_NothingIsSaved()
        {
            var themesList = JsonSerializer.Deserialize<List<Themes>>(
                GetResultFileFromResource(Constants.JsonFileGetThemes),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);
            var testTheme = themesList.First(themes => themes.Theme is Constants.TestThemeArchitecture);
            var yearsList = JsonSerializer.Deserialize<List<Years>>(
                GetResultFileFromResource(Constants.JsonFileGetYears),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);

            var theme = testTheme.ToTheme();
            theme.SetCountPerYear = yearsList.ToYearSetCountEnumerable().ToList();

            await _themeRepository.AddOrUpdate(theme);

            var bricksetApiService = Substitute.For<IBricksetApiService>();
            bricksetApiService.GetSets(Arg.Any<GetSetsParameters>()).Returns([]);

            var setSynchronizer = CreateTarget(bricksetApiService: bricksetApiService);

            await setSynchronizer.Synchronize();

            await bricksetApiService.Received().GetSets(Arg.Any<GetSetsParameters>());
            Check.That(await _setRepository.All()).IsEmpty();
        }

        [TestMethod]
        public async Task Synchronize_BricksetApiServiceReturnsListOfSets_AllSetsAreSaved()
        {
            var themesList = JsonSerializer.Deserialize<List<Themes>>(
                GetResultFileFromResource(Constants.JsonFileGetThemes),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);
            var testTheme = themesList.First(themes => themes.Theme is Constants.TestThemeArchitecture);
            var yearsList = JsonSerializer.Deserialize<List<Years>>(
                GetResultFileFromResource(Constants.JsonFileGetYears),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);
            var subthemesList = JsonSerializer.Deserialize<List<Subthemes>>(
                GetResultFileFromResource(Constants.JsonFileGetSubthemes),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);
            var setsList = JsonSerializer.Deserialize<List<Sets>>(
                GetResultFileFromResource(Constants.JsonFileGetSets),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);
            var testSet = setsList.First(set => set.SetId is Constants.TestSetId);
            var additionalImagesList = JsonSerializer.Deserialize<List<SetImage>>(
                GetResultFileFromResource(Constants.JsonFileGetAdditionalImages),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);
            var instructionsList = JsonSerializer.Deserialize<List<Instructions>>(
                GetResultFileFromResource(Constants.JsonFileGetInstructions),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);
            var testSubtheme = subthemesList.First(bricksetSubtheme => bricksetSubtheme.Subtheme == testSet.Subtheme);

            var theme = testTheme.ToTheme();
            theme.SetCountPerYear = yearsList.ToYearSetCountEnumerable().ToList();

            await _themeRepository.AddOrUpdate(theme);

            foreach (var subthemeItem in subthemesList)
            {
                var subthemeTheme = subthemeItem.ToSubtheme();
                subthemeTheme.Theme = theme;

                await _subthemeRepository.AddOrUpdate(subthemeTheme);
            }

            var subtheme = testSubtheme.ToSubtheme();
            subtheme.Theme = theme;

            var bricksetApiService = Substitute.For<IBricksetApiService>();
            bricksetApiService.GetSets(Arg.Any<GetSetsParameters>()).Returns(setsList);
            bricksetApiService.GetAdditionalImages(Arg.Is<ParameterSetId>(parameter => parameter.SetID == testSet.SetId)).Returns(additionalImagesList);
            bricksetApiService.GetInstructions(Arg.Is<ParameterSetId>(parameter => parameter.SetID == testSet.SetId)).Returns(instructionsList);

            var setSynchronizer = CreateTarget(bricksetApiService: bricksetApiService);

            await setSynchronizer.Synchronize();

            Check.That(await _setRepository.All()).CountIs(setsList.Count);
            var persistedSet = await _setRepository.Get(testSet.SetId);
            Check.That(persistedSet.Images).CountIs(additionalImagesList.Count);
            Check.That(persistedSet.Instructions).CountIs(instructionsList.Count);
        }

        private FullSetSynchronizer CreateTarget(
            IInsightsRepository insightsRepository = null,
            IOnboardingService onboardingService = null,
            IBricksetApiService bricksetApiService = null)
        {
            if (insightsRepository is null)
            {
                insightsRepository = Substitute.For<IInsightsRepository>();
                insightsRepository.GetDataSynchronizationTimestamp().Returns(default(DateTimeOffset?));
            }

            if (onboardingService is null)
            {
                onboardingService = Substitute.For<IOnboardingService>();
                onboardingService.GetBricksetApiKey().Returns("brickset-api-key");
            }

            bricksetApiService ??= Substitute.For<IBricksetApiService>();

            return new FullSetSynchronizer(
                insightsRepository,
                onboardingService,
                bricksetApiService,
                _setRepository,
                new ReferenceDataRepository(MemoryRepositoryService),
                _themeRepository,
                _subthemeRepository,
                Substitute.For<IBricksetUserRepository>(),
                Substitute.For<IThumbnailSynchronizer>(),
                Substitute.For<IMessageHub>());
        }
    }
}
