﻿using System;
using System.Threading.Tasks;
using abremir.AllMyBricks.Data.Interfaces;
using abremir.AllMyBricks.DataSynchronizer.Events.SetSynchronizationService;
using abremir.AllMyBricks.DataSynchronizer.Interfaces;
using abremir.AllMyBricks.Onboarding.Interfaces;
using Easy.MessageHub;

namespace abremir.AllMyBricks.DataSynchronizer.Services
{
    public class SetSynchronizationService : ISetSynchronizationService
    {
        private readonly IThemeSynchronizer _themeSynchronizer;
        private readonly ISubthemeSynchronizer _subthemeSynchronizer;
        private readonly ISetSynchronizer _setSynchronizer;
        private readonly IInsightsRepository _insightsRepository;
        private readonly IOnboardingService _onboardingService;
        private readonly IMessageHub _messageHub;

        public SetSynchronizationService(
            IThemeSynchronizer themeSynchronizer,
            ISubthemeSynchronizer subthemeSynchronizer,
            ISetSynchronizer setSynchronizer,
            IInsightsRepository insightsRepository,
            IOnboardingService onboardingService,
            IMessageHub messageHub)
        {
            _themeSynchronizer = themeSynchronizer;
            _subthemeSynchronizer = subthemeSynchronizer;
            _setSynchronizer = setSynchronizer;
            _insightsRepository = insightsRepository;
            _onboardingService = onboardingService;
            _messageHub = messageHub;
        }

        public async Task SynchronizeAllSets()
        {
            _messageHub.Publish(new SetSynchronizationServiceStart());

            try
            {
                var apiKey = await _onboardingService.GetBricksetApiKey().ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return;
                }

                var dataSynchronizationTimestamp = _insightsRepository.GetDataSynchronizationTimestamp();

                _messageHub.Publish(new InsightsAcquired { SynchronizationTimestamp = dataSynchronizationTimestamp });

                foreach (var theme in await _themeSynchronizer.Synchronize(apiKey).ConfigureAwait(false))
                {
                    _messageHub.Publish(new ProcessingThemeStart { Name = theme.Name });

                    try
                    {
                        var subthemes = await _subthemeSynchronizer.Synchronize(apiKey, theme).ConfigureAwait(false);

                        if (!dataSynchronizationTimestamp.HasValue)
                        {
                            foreach (var subtheme in subthemes)
                            {
                                _messageHub.Publish(new ProcessingSubthemeStart { Name = subtheme.Name });

                                await _setSynchronizer.Synchronize(apiKey, theme, subtheme).ConfigureAwait(false);

                                _messageHub.Publish(new ProcessingSubthemeEnd { Name = subtheme.Name });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _messageHub.Publish(new ProcessingThemeException { Name = theme.Name, Exception = ex });
                    }

                    _messageHub.Publish(new ProcessingThemeEnd { Name = theme.Name });
                }

                if (dataSynchronizationTimestamp.HasValue)
                {
                    await _setSynchronizer.Synchronize(apiKey, dataSynchronizationTimestamp.Value).ConfigureAwait(false);
                }

                _insightsRepository.UpdateDataSynchronizationTimestamp(DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _messageHub.Publish(new SetSynchronizationServiceException { Exception = ex });
            }

            _messageHub.Publish(new SetSynchronizationServiceEnd());
        }
    }
}
