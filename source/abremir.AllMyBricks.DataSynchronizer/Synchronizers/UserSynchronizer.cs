using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using abremir.AllMyBricks.Data.Enumerations;
using abremir.AllMyBricks.Data.Interfaces;
using abremir.AllMyBricks.Data.Models;
using abremir.AllMyBricks.DataSynchronizer.Configuration;
using abremir.AllMyBricks.DataSynchronizer.Events.UserSynchronizer;
using abremir.AllMyBricks.DataSynchronizer.Interfaces;
using abremir.AllMyBricks.ThirdParty.Brickset.Interfaces;
using abremir.AllMyBricks.ThirdParty.Brickset.Models;
using abremir.AllMyBricks.ThirdParty.Brickset.Models.Parameters;
using Easy.MessageHub;

namespace abremir.AllMyBricks.DataSynchronizer.Synchronizers
{
    public class UserSynchronizer(
        IBricksetApiService bricksetApiService,
        IBricksetUserRepository bricksetUserRepository,
        IMessageHub messageHub,
        ISetRepository setRepository)
        : IUserSynchronizer
    {
        private readonly IMessageHub _messageHub = messageHub;
        private readonly IBricksetUserRepository _bricksetUserRepository = bricksetUserRepository;
        private readonly ISetRepository _setRepository = setRepository;
        private readonly IBricksetApiService _bricksetApiService = bricksetApiService;

        public async Task SynchronizeBricksetPrimaryUser(string apiKey, string username, string userHash)
        {
            _messageHub.Publish(new UserSynchronizerStart { UserType = BricksetUserType.Primary, Username = username });

            try
            {
                var user = await _bricksetUserRepository.Get(username).ConfigureAwait(false);

                if (user.UserSynchronizationTimestamp.HasValue)
                {
                    _messageHub.Publish(new AllMyBricksToBricksetStart());

                    _messageHub.Publish(new AllMyBricksToBricksetAcquiringSetsStart());

                    List<BricksetUserSet> updatedSetsSinceLastSynchronization = [.. user.Sets.Where(set => set.LastChangeTimestamp > user.UserSynchronizationTimestamp.Value)];

                    _messageHub.Publish(new AllMyBricksToBricksetAcquiringSetsEnd { Count = updatedSetsSinceLastSynchronization.Count });

                    updatedSetsSinceLastSynchronization.ForEach(async bricksetUserSet =>
                    {
                        _messageHub.Publish(new UserSynchronizerSynchronizingSetStart { SetId = bricksetUserSet.Set.SetId });

                        var setCollectionParameter = new SetCollectionParameters
                        {
                            ApiKey = apiKey,
                            UserHash = userHash,
                            SetID = bricksetUserSet.Set.SetId,
                            QtyOwned = bricksetUserSet.QuantityOwned,
                            Want = bricksetUserSet.Wanted,
                            Own = bricksetUserSet.Owned
                        };

                        await _bricksetApiService.SetCollection(setCollectionParameter).ConfigureAwait(false);

                        _messageHub.Publish(new UserSynchronizerSynchronizingSetEnd { SetId = bricksetUserSet.Set.SetId });
                    });

                    _messageHub.Publish(new AllMyBricksToBricksetEnd());
                }

                _messageHub.Publish(new BricksetToAllMyBricksStart());

                _messageHub.Publish(new BricksetToAllMyBricksAcquiringSetsStart());

                var bricksetUserSets = await GetAllUserSetsFromBrickset(apiKey, userHash: userHash).ConfigureAwait(false);

                var allMyBricksUserSetIds = user.Sets.Select(bricksetUserSet => bricksetUserSet.Set.SetId);
                var bricksetUserSetIds = bricksetUserSets.Select(bricksetUserSet => bricksetUserSet.Set.SetId);
                List<long> setIdsNotInAllMyBricks = [.. bricksetUserSetIds.Except(allMyBricksUserSetIds)];

                _messageHub.Publish(new BricksetToAllMyBricksAcquiringSetsEnd { Count = setIdsNotInAllMyBricks.Count });

                foreach (var userSetNotInAllMyBricks in bricksetUserSets.Where(bricksetUserSet => setIdsNotInAllMyBricks.Contains(bricksetUserSet.Set.SetId)))
                {
                    _messageHub.Publish(new UserSynchronizerSynchronizingSetStart { SetId = userSetNotInAllMyBricks.Set.SetId });

                    await _bricksetUserRepository.AddOrUpdateSet(username, userSetNotInAllMyBricks).ConfigureAwait(false);

                    _messageHub.Publish(new UserSynchronizerSynchronizingSetEnd { SetId = userSetNotInAllMyBricks.Set.SetId });
                }

                _messageHub.Publish(new BricksetToAllMyBricksEnd());

                await _bricksetUserRepository.UpdateUserSynchronizationTimestamp(username, DateTimeOffset.Now).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _messageHub.Publish(new UserSynchronizerException { UserType = BricksetUserType.Primary, Username = username, Exception = ex });
            }

            _messageHub.Publish(new UserSynchronizerEnd { UserType = BricksetUserType.Primary, Username = username });
        }

        public async Task SynchronizeBricksetFriend(string apiKey, string username)
        {
            _messageHub.Publish(new UserSynchronizerStart { UserType = BricksetUserType.Friend, Username = username });

            try
            {
                _messageHub.Publish(new BricksetToAllMyBricksAcquiringSetsStart());

                List<BricksetUserSet> bricksetUserSets = [.. (await GetAllUserSetsFromBrickset(apiKey, username).ConfigureAwait(false))];

                _messageHub.Publish(new BricksetToAllMyBricksAcquiringSetsEnd { Count = bricksetUserSets.Count });

                foreach (var bricksetUserSet in bricksetUserSets)
                {
                    _messageHub.Publish(new UserSynchronizerSynchronizingSetStart { SetId = bricksetUserSet.Set.SetId });

                    await _bricksetUserRepository.AddOrUpdateSet(username, bricksetUserSet).ConfigureAwait(false);

                    _messageHub.Publish(new UserSynchronizerSynchronizingSetEnd { SetId = bricksetUserSet.Set.SetId });
                }

                await _bricksetUserRepository.UpdateUserSynchronizationTimestamp(username, DateTimeOffset.Now).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _messageHub.Publish(new UserSynchronizerException { UserType = BricksetUserType.Friend, Username = username, Exception = ex });
            }

            _messageHub.Publish(new UserSynchronizerEnd { UserType = BricksetUserType.Friend, Username = username });
        }

        private async Task<IEnumerable<BricksetUserSet>> GetAllUserSetsFromBrickset(string apiKey, string username = null, string userHash = null)
        {
            var allUserSets = await GetAllOwnedSets(apiKey, username, userHash).ConfigureAwait(false);
            var allWantedSetIds = await GetAllWantedSetIds(apiKey, username, userHash).ConfigureAwait(false);

            foreach (var wantedSetId in allWantedSetIds)
            {
                if (allUserSets.ContainsKey(wantedSetId))
                {
                    allUserSets[wantedSetId].Wanted = true;
                }
                else
                {
                    allUserSets.Add(wantedSetId, new BricksetUserSet
                    {
                        Set = await _setRepository.Get(wantedSetId).ConfigureAwait(false),
                        Wanted = true
                    });
                }
            }

            return allUserSets.Values;
        }

        private async Task<IDictionary<long, BricksetUserSet>> GetAllOwnedSets(string apiKey, string username = null, string userHash = null)
        {
            if (string.IsNullOrWhiteSpace(apiKey)
                || (string.IsNullOrWhiteSpace(userHash) && string.IsNullOrWhiteSpace(username))
                || (!string.IsNullOrWhiteSpace(userHash) && !string.IsNullOrWhiteSpace(username)))
            {
                return new Dictionary<long, BricksetUserSet>();
            }

            var getSetsParameter = new GetSetsParameters
            {
                ApiKey = apiKey,
                UserHash = userHash ?? string.Empty,
                Owned = true,
                PageSize = Constants.BricksetDefaultPageSizeParameter
            };

            List<BricksetUserSet> ownedSets = [];
            var pageNumber = 1;
            List<Sets> currentPageResults = [];
            do
            {
                getSetsParameter.PageNumber = pageNumber;

                currentPageResults = [.. (await _bricksetApiService.GetSets(getSetsParameter).ConfigureAwait(false))];

                var tasks = await Task.Run(() => currentPageResults.Select(async set => new BricksetUserSet
                {
                    Owned = true,
                    QuantityOwned = (short)(set.Collection?.QtyOwned ?? 0),
                    Set = await _setRepository.Get(set.SetId).ConfigureAwait(false)
                })).ConfigureAwait(false);

                ownedSets.AddRange(await Task.WhenAll(tasks).ConfigureAwait(false));

                pageNumber++;
            } while (currentPageResults.Count is Constants.BricksetDefaultPageSizeParameter);

            return ownedSets.ToDictionary(bricksetUserSet => bricksetUserSet.Set.SetId);
        }

        private async Task<IEnumerable<int>> GetAllWantedSetIds(string apiKey, string username = null, string userHash = null)
        {
            if (string.IsNullOrWhiteSpace(apiKey)
                || (string.IsNullOrWhiteSpace(userHash) && string.IsNullOrWhiteSpace(username))
                || (!string.IsNullOrWhiteSpace(userHash) && !string.IsNullOrWhiteSpace(username)))
            {
                return [];
            }

            var getSetsParameter = new GetSetsParameters
            {
                ApiKey = apiKey,
                UserHash = userHash ?? string.Empty,
                Wanted = true,
                PageSize = Constants.BricksetDefaultPageSizeParameter
            };

            List<int> wantedSets = [];
            var pageNumber = 1;
            List<Sets> currentPageResults = [];
            do
            {
                getSetsParameter.PageNumber = pageNumber;

                currentPageResults = [.. (await _bricksetApiService.GetSets(getSetsParameter).ConfigureAwait(false))];

                wantedSets.AddRange(currentPageResults.Select(set => set.SetId));

                pageNumber++;
            } while (currentPageResults.Count is Constants.BricksetDefaultPageSizeParameter);

            return wantedSets;
        }
    }
}
