using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using abremir.AllMyBricks.Data.Enumerations;
using abremir.AllMyBricks.Data.Interfaces;
using abremir.AllMyBricks.Data.Models;
using abremir.AllMyBricks.DataSynchronizer.Configuration;
using abremir.AllMyBricks.DataSynchronizer.Events.SetSynchronizer;
using abremir.AllMyBricks.DataSynchronizer.Extensions;
using abremir.AllMyBricks.DataSynchronizer.Interfaces;
using abremir.AllMyBricks.Onboarding.Interfaces;
using abremir.AllMyBricks.ThirdParty.Brickset.Interfaces;
using abremir.AllMyBricks.ThirdParty.Brickset.Models;
using abremir.AllMyBricks.ThirdParty.Brickset.Models.Parameters;
using Easy.MessageHub;

namespace abremir.AllMyBricks.DataSynchronizer.Synchronizers
{
    public abstract class SetSynchronizerBase(
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
    {
        protected readonly IInsightsRepository InsightsRepository = insightsRepository;
        protected readonly IOnboardingService OnboardingService = onboardingService;
        protected readonly IBricksetApiService BricksetApiService = bricksetApiService;
        protected readonly ISetRepository SetRepository = setRepository;
        protected readonly IReferenceDataRepository ReferenceDataRepository = referenceDataRepository;
        protected readonly IThemeRepository ThemeRepository = themeRepository;
        protected readonly ISubthemeRepository SubthemeRepository = subthemeRepository;
        protected readonly IBricksetUserRepository BricksetUserRepository = bricksetUserRepository;
        protected readonly IThumbnailSynchronizer ThumbnailSynchronizer = thumbnailSynchronizer;
        protected readonly IMessageHub MessageHub = messageHub;

        protected async Task AddOrUpdateSet(string apiKey, Theme theme, Subtheme subtheme, Sets bricksetSet, short? year = null)
        {
            MessageHub.Publish(new SynchronizingSetStart { Theme = theme.Name, Subtheme = subtheme?.Name, Name = bricksetSet.Name, Number = bricksetSet.Number, NumberVariant = bricksetSet.NumberVariant, Year = year });

            try
            {
                var set = await MapSet(apiKey, theme, subtheme, bricksetSet).ConfigureAwait(false);

                await SetRepository.AddOrUpdate(set).ConfigureAwait(false);

                await ThumbnailSynchronizer.Synchronize(set, true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                MessageHub.Publish(new SynchronizingSetException { Theme = theme.Name, Subtheme = subtheme?.Name, Name = bricksetSet.Name, Number = bricksetSet.Number, NumberVariant = bricksetSet.NumberVariant, Exception = ex });
            }

            MessageHub.Publish(new SynchronizingSetEnd { Theme = theme.Name, Subtheme = subtheme?.Name, Name = bricksetSet.Name, Number = bricksetSet.Number, NumberVariant = bricksetSet.NumberVariant });
        }

        private async Task<Set> MapSet(string apiKey, Theme theme, Subtheme subtheme, Sets bricksetSet)
        {
            var set = bricksetSet.ToSet();

            set.Theme = theme;
            set.Subtheme = subtheme;
            set.Category = await ReferenceDataRepository.GetOrAdd<Category>(bricksetSet.Category).ConfigureAwait(false);
            set.PackagingType = await ReferenceDataRepository.GetOrAdd<PackagingType>(bricksetSet.PackagingType).ConfigureAwait(false);
            set.ThemeGroup = await ReferenceDataRepository.GetOrAdd<ThemeGroup>(bricksetSet.ThemeGroup).ConfigureAwait(false);

            await SetTagList(set, bricksetSet.ExtendedData?.Tags).ConfigureAwait(false);
            SetPriceList(set, bricksetSet.LegoCom);

            await SetImageList(apiKey, set, bricksetSet).ConfigureAwait(false);
            await SetInstructionList(apiKey, set, bricksetSet).ConfigureAwait(false);

            return set;
        }

        private async Task SetTagList(Set set, IEnumerable<string> tags)
        {
            if (tags is null)
            {
                return;
            }

            foreach (var tag in tags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag))
            {
                set.Tags.Add(await ReferenceDataRepository.GetOrAdd<Tag>(tag).ConfigureAwait(false));
            }
        }

        private async Task SetImageList(string apiKey, Set set, Sets bricksetSet)
        {
            if (bricksetSet.Image is not null)
            {
                set.Images.Add(new Image
                {
                    ImageUrl = bricksetSet.Image.ImageUrl?.SanitizeBricksetString(),
                    ThumbnailUrl = bricksetSet.Image.ThumbnailUrl?.SanitizeBricksetString()
                });
            }

            if (bricksetSet.AdditionalImageCount is 0)
            {
                return;
            }

            var getAdditionalImagesParameters = new ParameterSetId
            {
                ApiKey = apiKey,
                SetID = set.SetId
            };

            set.Images = [.. (await BricksetApiService.GetAdditionalImages(getAdditionalImagesParameters).ConfigureAwait(false)).ToImageEnumerable()];
        }

        private static void SetPriceList(Set set, SetLegoCom legoCom)
        {
            if (legoCom is null)
            {
                return;
            }

            if (legoCom.CA?.RetailPrice.HasValue is true)
            {
                set.Prices.Add(new Price
                {
                    Region = PriceRegion.CA,
                    Value = legoCom.CA.RetailPrice.Value
                });
            }

            if (legoCom.DE?.RetailPrice.HasValue is true)
            {
                set.Prices.Add(new Price
                {
                    Region = PriceRegion.DE,
                    Value = legoCom.DE.RetailPrice.Value
                });
            }

            if (legoCom.UK?.RetailPrice.HasValue is true)
            {
                set.Prices.Add(new Price
                {
                    Region = PriceRegion.UK,
                    Value = legoCom.UK.RetailPrice.Value
                });
            }

            if (legoCom.US?.RetailPrice.HasValue is true)
            {
                set.Prices.Add(new Price
                {
                    Region = PriceRegion.US,
                    Value = legoCom.US.RetailPrice.Value
                });
            }
        }

        private async Task SetInstructionList(string apiKey, Set set, Sets bricksetSet)
        {
            if (bricksetSet.InstructionsCount is 0)
            {
                return;
            }

            var getInstructionsParameters = new ParameterSetId
            {
                ApiKey = apiKey,
                SetID = set.SetId
            };

            set.Instructions = [.. (await BricksetApiService.GetInstructions(getInstructionsParameters).ConfigureAwait(false)).ToInstructionEnumerable()];
        }

        protected async Task<IList<Sets>> GetAllSetsFor(string apiKey, GetSetsParameters getSetsParameters)
        {
            getSetsParameters.ApiKey = apiKey;
            getSetsParameters.PageSize ??= Constants.BricksetDefaultPageSizeParameter;
            getSetsParameters.ExtendedData = true;

            List<Sets> foundSets = [];
            var pageNumber = 1;

            List<Sets> currentPageResults;
            do
            {
                getSetsParameters.PageNumber = pageNumber;

                currentPageResults = [.. (await BricksetApiService.GetSets(getSetsParameters).ConfigureAwait(false))];

                foundSets.AddRange(currentPageResults);

                pageNumber++;
            } while (currentPageResults.Count == getSetsParameters.PageSize);

            return foundSets;
        }

        protected async Task<(int ExpectedNumberOfSets, int ActualNumberOfSets)> GetSetNumbers()
        {
            var expectedTotalNumberOfSets = (await ThemeRepository
                .All().ConfigureAwait(false))
                .Sum(theme => theme.SetCount);
            var actualTotalNumberOfSets = await SetRepository.Count().ConfigureAwait(false);

            return (expectedTotalNumberOfSets, actualTotalNumberOfSets);
        }

        protected async Task DeleteSets(List<long> setsToDelete)
        {
            if ((setsToDelete?.Count ?? 0) is 0)
            {
                return;
            }

            MessageHub.Publish(new DeletingSetsStart { AffectedSets = setsToDelete });

            var tasks = new List<Task>
            {
                SetRepository.DeleteMany(setsToDelete)
            };

            var primaryUsernames = await BricksetUserRepository.GetAllUsernames(BricksetUserType.Primary).ConfigureAwait(false);
            var friendUsernames = await BricksetUserRepository.GetAllUsernames(BricksetUserType.Friend).ConfigureAwait(false);

            foreach (var username in primaryUsernames.Concat(friendUsernames))
            {
                tasks.Add(BricksetUserRepository.RemoveSets(username, setsToDelete));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            MessageHub.Publish(new DeletingSetsEnd { AffectedSets = setsToDelete });
        }
    }
}
