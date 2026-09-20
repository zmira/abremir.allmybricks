using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using abremir.AllMyBricks.Data.Interfaces;
using abremir.AllMyBricks.DataSynchronizer.Events.SetSynchronizer;
using abremir.AllMyBricks.DataSynchronizer.Events.ThemeSanitizer;
using abremir.AllMyBricks.DataSynchronizer.Interfaces;
using abremir.AllMyBricks.Onboarding.Interfaces;
using abremir.AllMyBricks.ThirdParty.Brickset.Interfaces;
using abremir.AllMyBricks.ThirdParty.Brickset.Models.Parameters;
using Easy.MessageHub;

namespace abremir.AllMyBricks.DataSynchronizer.Synchronizers
{
    public class ThemeSanitizer(
        IInsightsRepository insightsRepository,
        IOnboardingService onboardingService,
        IBricksetApiService bricksetApiService,
        ISetRepository setRepository,
        IReferenceDataRepository referenceDataRepository,
        IThemeRepository themeRepository,
        ISubthemeRepository subthemeRepository,
        IBricksetUserRepository bricksetUserRepository,
        IThumbnailSynchronizer thumbnailSynchronizer,
        IMessageHub messageHub)
        : SetSynchronizerBase(insightsRepository, onboardingService, bricksetApiService, setRepository, referenceDataRepository, themeRepository, subthemeRepository, bricksetUserRepository, thumbnailSynchronizer, messageHub), IThemeSanitizer
    {
        public async Task<int> Synchronize()
        {
            MessageHub.Publish(new ThemeSanitizerStart());

            var apiKey = await OnboardingService.GetBricksetApiKey().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                MessageHub.Publish(new ThemeSanitizerException { Exception = new Exception("Invalid Brickset API key") });

                return 1;
            }

            var (ExpectedNumberOfSets, ActualNumberOfSets) = await GetSetNumbers();

            if (ActualNumberOfSets == ExpectedNumberOfSets)
            {
                MessageHub.Publish(new ThemeSanitizerEnd());

                return 0;
            }

            MessageHub.Publish(new MismatchingNumberOfSetsWarning { Expected = ExpectedNumberOfSets, Actual = ActualNumberOfSets });

            var getThemesParameters = new ParameterApiKey
            {
                ApiKey = apiKey
            };

            List<string> bricksetThemes = [.. (await BricksetApiService.GetThemes(getThemesParameters).ConfigureAwait(false)).Select(theme => theme.Theme).Order()];

            MessageHub.Publish(new ThemesAcquired { Count = bricksetThemes.Count });

            List<string> allMyBricksThemes = [.. (await ThemeRepository.All().ConfigureAwait(false)).Select(theme => theme.Name).Order()];

            List<string> themesToDelete = [.. allMyBricksThemes.Except(bricksetThemes)];

            if (themesToDelete.Count is not 0)
            {
                MessageHub.Publish(new DeletingThemesStart { AffectedThemes = themesToDelete });

                foreach (var theme in themesToDelete)
                {
                    List<string> allMyBricksSubthemes = [.. (await SubthemeRepository.AllForTheme(theme).ConfigureAwait(false)).Select(subtheme => subtheme.Name).Order()];

                    MessageHub.Publish(new DeletingSubthemesStart { AffectedTheme = theme, AffectedSubthemes = allMyBricksSubthemes });

                    foreach (var subtheme in allMyBricksSubthemes)
                    {
                        List<long> allMyBricksSets = [.. (await SetRepository.AllForSubtheme(theme, subtheme).ConfigureAwait(false)).Select(set => set.SetId)];

                        await DeleteSets(allMyBricksSets);
                    }

                    await SubthemeRepository.DeleteMany(theme, allMyBricksSubthemes).ConfigureAwait(false);

                    MessageHub.Publish(new DeletingSubthemesEnd { AffectedTheme = theme, AffectedSubthemes = allMyBricksSubthemes });
                }

                await ThemeRepository.DeleteMany(themesToDelete).ConfigureAwait(false);

                MessageHub.Publish(new DeletingThemesEnd { AffectedThemes = themesToDelete });
            }

            MessageHub.Publish(new ThemeSanitizerEnd());

            return 0;
        }
    }
}
