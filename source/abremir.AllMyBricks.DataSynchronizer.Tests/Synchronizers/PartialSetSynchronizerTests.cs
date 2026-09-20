using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using abremir.AllMyBricks.Data.Interfaces;
using abremir.AllMyBricks.Data.Models;
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
    public class PartialSetSynchronizerTests : DataSynchronizerTestsBase
    {
        private static SetRepository _setRepository;
        private static ThemeRepository _themeRepository;
        private static SubthemeRepository _subthemeRepository;

        public PartialSetSynchronizerTests()
        {
            _setRepository = new SetRepository(MemoryRepositoryService);
            _themeRepository = new ThemeRepository(MemoryRepositoryService);
            _subthemeRepository = new SubthemeRepository(MemoryRepositoryService);
        }

        [TestMethod]
        public async Task Synchronize_InsightsRepositoryReturnsEmptyDataSynchronizationTimestamp_Returns()
        {
            var insightsRepository = Substitute.For<IInsightsRepository>();
            insightsRepository.GetDataSynchronizationTimestamp().Returns(default(DateTimeOffset?));
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
        public async Task Synchronize_BricksetApiServiceReturnsEmptyListOfSets_NothingIsSaved()
        {
            var bricksetApiService = Substitute.For<IBricksetApiService>();
            bricksetApiService.GetSets(Arg.Any<GetSetsParameters>()).Returns([]);

            var partialSetSynchronizer = CreateTarget(bricksetApiService: bricksetApiService);

            await partialSetSynchronizer.Synchronize();

            Check.That(await _setRepository.All()).IsEmpty();
        }

        [TestMethod]
        public async Task Synchronize_BricksetApiServiceReturnsListOfSets_AllSetsAreSaved()
        {
            var insightsRepository = Substitute.For<IInsightsRepository>();
            insightsRepository.GetDataSynchronizationTimestamp().Returns(DateTimeOffset.Now);
            var themesList = JsonSerializer.Deserialize<List<Themes>>(
                GetResultFileFromResource(Constants.JsonFileGetThemes),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);
            var testTheme = themesList.First(themes => themes.Theme is Constants.TestThemeArchitecture);
            var theme = testTheme.ToTheme();
            var recentlyUpdatedSetsList = JsonSerializer.Deserialize<List<Sets>>(
                GetResultFileFromResource(Constants.JsonFileGetRecentlyUpdatedSets),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);

            await _themeRepository.AddOrUpdate(theme);

            var subtheme = new Subtheme
            {
                Name = "",
                Theme = theme,
                YearFrom = theme.YearFrom,
                YearTo = theme.YearTo
            };

            await _subthemeRepository.AddOrUpdate(subtheme);

            var bricksetApiService = Substitute.For<IBricksetApiService>();
            bricksetApiService.GetSets(Arg.Any<GetSetsParameters>()).Returns(recentlyUpdatedSetsList);

            var partialSetSynchronizer = CreateTarget(insightsRepository, bricksetApiService: bricksetApiService);

            await partialSetSynchronizer.Synchronize();

            Check.That(await _setRepository.All()).CountIs(recentlyUpdatedSetsList.Count);
            await insightsRepository.Received().UpdateDataSynchronizationTimestamp(Arg.Any<DateTimeOffset>());
        }

        private PartialSetSynchronizer CreateTarget(
            IInsightsRepository insightsRepository = null,
            IOnboardingService onboardingService = null,
            IBricksetApiService bricksetApiService = null)
        {
            if (insightsRepository is null)
            {
                insightsRepository = Substitute.For<IInsightsRepository>();
                insightsRepository.GetDataSynchronizationTimestamp().Returns(DateTimeOffset.Now);
            }

            if (onboardingService is null)
            {
                onboardingService = Substitute.For<IOnboardingService>();
                onboardingService.GetBricksetApiKey().Returns("brickset-api-key");
            }

            bricksetApiService ??= Substitute.For<IBricksetApiService>();

            return new PartialSetSynchronizer(
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
