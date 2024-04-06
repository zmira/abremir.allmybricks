﻿using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using abremir.AllMyBricks.Data.Interfaces;
using abremir.AllMyBricks.DataSynchronizer.Enumerations;
using abremir.AllMyBricks.DataSynchronizer.Events.SetSynchronizer;
using abremir.AllMyBricks.DataSynchronizer.Interfaces;
using abremir.AllMyBricks.Onboarding.Interfaces;
using abremir.AllMyBricks.ThirdParty.Brickset.Interfaces;
using abremir.AllMyBricks.ThirdParty.Brickset.Models.Parameters;
using Easy.MessageHub;
using LiteDB;

namespace abremir.AllMyBricks.DataSynchronizer.Synchronizers
{
    public class SetSanitizer : SetSynchronizerBase, ISetSanitizer
    {
        public SetSanitizer(
            IInsightsRepository insightsRepository,
            IOnboardingService onboardingService,
            IBricksetApiService bricksetApiService,
            ISetRepository setRepository,
            IReferenceDataRepository referenceDataRepository,
            IThemeRepository themeRepository,
            ISubthemeRepository subthemeRepository,
            IThumbnailSynchronizer thumbnailSynchronizer,
            IMessageHub messageHub)
            : base(insightsRepository, onboardingService, bricksetApiService, setRepository, referenceDataRepository, themeRepository, subthemeRepository, thumbnailSynchronizer, messageHub) { }

        public async Task Synchronize()
        {
            MessageHub.Publish(new SetSynchronizerStart { Type = SetAcquisitionType.Sanitize });

            var apiKey = await OnboardingService.GetBricksetApiKey().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                var exception = new Exception("Invalid Brickset API key");
                MessageHub.Publish(new SetSynchronizerException { Exception = exception });

                throw exception;
            }

            var expectedTotalNumberOfSets = (await ThemeRepository
                .All().ConfigureAwait(false))
                .Sum(theme => theme.SetCount);
            var actualTotalNumberOfSets = await SetRepository.Count().ConfigureAwait(false);

            if (actualTotalNumberOfSets == expectedTotalNumberOfSets)
            {
                MessageHub.Publish(new SetSynchronizerEnd { Type = SetAcquisitionType.Sanitize });

                return;
            }

            MessageHub.Publish(new MismatchingNumberOfSetsWarning { Expected = expectedTotalNumberOfSets, Actual = actualTotalNumberOfSets });

            var numberOfSetsPerYearFromSets = (await SetRepository.All().ConfigureAwait(false))
                .GroupBy(set => set.Year)
                .ToFrozenDictionary(group => group.Key, group => group.Count());
            var numberOfSetsPerYearFromThemes = (await ThemeRepository.All().ConfigureAwait(false))
                .SelectMany(theme => theme.SetCountPerYear)
                .GroupBy(setCountPerYear => setCountPerYear.Year)
                .ToFrozenDictionary(group => group.Key, group => group.Sum(value => value.SetCount));

            Dictionary<short, HashSet<string>> themesWithDifferences = [];

            foreach (var year in numberOfSetsPerYearFromSets.Keys.Order())
            {
                var fromThemeHasYear = numberOfSetsPerYearFromThemes.TryGetValue(year, out var setCountForYearFromTheme);
                if (!fromThemeHasYear || numberOfSetsPerYearFromSets[year] != setCountForYearFromTheme)
                {
                    themesWithDifferences.TryAdd(year, []);

                    var setThemesFromYearWithDifference = (await SetRepository.AllForYear(year).ConfigureAwait(false))
                        .GroupBy(set => set.Theme.Name)
                        .ToFrozenDictionary(group => group.Key, group => group.Count());
                    var themesFromYearWithDifference = (await ThemeRepository.AllForYear(year).ConfigureAwait(false))
                        .ToFrozenDictionary(theme => theme.Name, theme => theme.SetCountPerYear.First(setCountPerYear => setCountPerYear.Year == year).SetCount);

                    foreach (var theme in setThemesFromYearWithDifference.Keys.Order())
                    {
                        var fromThemesHasTheme = themesFromYearWithDifference.TryGetValue(theme, out var setCountForThemeFromTheme);
                        if (!fromThemesHasTheme || setThemesFromYearWithDifference[theme] != setCountForThemeFromTheme)
                        {
                            themesWithDifferences[year].Add(theme);
                        }
                    }
                }
            }

            if (themesWithDifferences.Count > 0)
            {
                MessageHub.Publish(new AdjustingThemesWithDifferencesStart { AffectedThemes = themesWithDifferences });

                foreach (var year in themesWithDifferences.Keys)
                {
                    var getSetsParameters = new GetSetsParameters
                    {
                        Theme = string.Join(",", themesWithDifferences[year]),
                        Year = year.ToString()
                    };

                    MessageHub.Publish(new AcquiringSetsStart { Type = SetAcquisitionType.Sanitize, Parameters = getSetsParameters });

                    var bricksetSetsFromThemeWithDifferences = await GetAllSetsFor(apiKey, getSetsParameters).ConfigureAwait(false);
                    var bricksetSetIds = bricksetSetsFromThemeWithDifferences
                        .Select(set => (long)set.SetId)
                        .Order();

                    MessageHub.Publish(new AcquiringSetsEnd { Count = bricksetSetsFromThemeWithDifferences.Count, Type = SetAcquisitionType.Sanitize, Parameters = getSetsParameters });

                    var identifiedThemes = themesWithDifferences[year].ToList();
                    var allMyBricksSetsFromThemeWithDifferences = (await SetRepository.Find(set => set.Year == year && identifiedThemes.Contains(set.Theme.Name)))
                        .Select(set => set.SetId)
                        .Order();

                    var setsToDelete = allMyBricksSetsFromThemeWithDifferences.Except(bricksetSetIds).ToList();

                    await SetRepository.DeleteMany(setsToDelete).ConfigureAwait(false);

                    foreach (var bricksetSet in bricksetSetsFromThemeWithDifferences)
                    {
                        var theme = await ThemeRepository.Get(bricksetSet.Theme).ConfigureAwait(false);
                        var subtheme = await SubthemeRepository.Get(theme.Name, bricksetSet.Subtheme).ConfigureAwait(false);

                        await AddOrUpdateSet(apiKey, theme, subtheme, bricksetSet).ConfigureAwait(false);
                    }
                }

                MessageHub.Publish(new AdjustingThemesWithDifferencesEnd { AffectedThemes = themesWithDifferences });
            }

            MessageHub.Publish(new SetSynchronizerEnd { Type = SetAcquisitionType.Sanitize });
        }
    }
}
