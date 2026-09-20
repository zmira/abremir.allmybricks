using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using abremir.AllMyBricks.Data.Enumerations;
using abremir.AllMyBricks.Data.Extensions;
using abremir.AllMyBricks.Data.Interfaces;
using abremir.AllMyBricks.Data.Models;
using LiteDB;
using LiteDB.Async;

namespace abremir.AllMyBricks.Data.Repositories
{
    public class BricksetUserRepository(IRepositoryService repositoryService) : IBricksetUserRepository
    {
        private readonly IRepositoryService _repositoryService = repositoryService;

        public async Task<BricksetUser> Add(BricksetUserType userType, string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            var existingBricksetUser = await Get(username).ConfigureAwait(false);

            if (existingBricksetUser is not null)
            {
                return existingBricksetUser;
            }

            var bricksetUser = new BricksetUser
            {
                BricksetUsername = username.Trim(),
                UserType = userType,
                Sets = []
            };

            using var repository = _repositoryService.GetRepository();

            await repository.InsertAsync(bricksetUser).ConfigureAwait(false);

            return bricksetUser;
        }

        public async Task<BricksetUser> Get(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            using var repository = _repositoryService.GetRepository();

            return await GetQueryable(repository)
                .Where(bricksetUser => bricksetUser.BricksetUsername == username.Trim())
                .FirstOrDefaultAsync().ConfigureAwait(false);
        }

        public async Task<bool> Exists(string username)
        {
            return await Get(username).ConfigureAwait(false) is not null;
        }

        public async Task<bool> Remove(string username)
        {
            var bricksetUser = await Get(username).ConfigureAwait(false);

            if (bricksetUser is null)
            {
                return false;
            }

            using var repository = _repositoryService.GetRepository();

            return await repository.DeleteAsync<BricksetUser>(bricksetUser.Id).ConfigureAwait(false);
        }

        public async Task<BricksetUserSet> AddOrUpdateSet(string username, BricksetUserSet bricksetUserSet)
        {
            if (string.IsNullOrWhiteSpace(username)
                || bricksetUserSet is null
                || bricksetUserSet.Set is null
                || bricksetUserSet.Set.SetId is 0)
            {
                return null;
            }

            using var repository = _repositoryService.GetRepository();

            if (await repository.FirstOrDefaultAsync<Set>(set => set.SetId == bricksetUserSet.Set.SetId).ConfigureAwait(false) is null)
            {
                return null;
            }

            var bricksetUser = await Get(username).ConfigureAwait(false);

            if (bricksetUser is null)
            {
                return null;
            }

            var existingBricksetUserSet = bricksetUser.Sets.FirstOrDefault(set => set.Set.SetId == bricksetUserSet.Set.SetId);

            if (existingBricksetUserSet is not null)
            {
                if (existingBricksetUserSet.Owned == bricksetUserSet.Owned
                    && existingBricksetUserSet.Wanted == bricksetUserSet.Wanted
                    && existingBricksetUserSet.QuantityOwned == bricksetUserSet.QuantityOwned)
                {
                    return existingBricksetUserSet;
                }

                bricksetUser.Sets.Remove(existingBricksetUserSet);
            }

            bricksetUserSet.LastChangeTimestamp ??= DateTimeOffset.Now;
            bricksetUser.Sets.Add(bricksetUserSet);

            await repository.UpdateAsync(bricksetUser).ConfigureAwait(false);

            return bricksetUserSet;
        }

        public async Task<BricksetUserSet> GetSet(string username, long setId)
        {
            if (string.IsNullOrWhiteSpace(username)
                || setId is 0)
            {
                return null;
            }

            return (await Get(username).ConfigureAwait(false))?.Sets.FirstOrDefault(set => set.Set.SetId == setId);
        }

        public async Task<int> RemoveSets(string username, List<long> setIds)
        {
            if (string.IsNullOrWhiteSpace(username)
                || (setIds?.Count ?? 0) is 0)
            {
                return 0;
            }

            using var repository = _repositoryService.GetRepository();

            var bricksetUser = await Get(username).ConfigureAwait(false);

            if (bricksetUser is null)
            {
                return 0;
            }

            List<BricksetUserSet> bricksetUserSets = [.. bricksetUser.Sets.Where(set => setIds.Contains(set.Set.SetId))];
            var setCount = bricksetUserSets.Count;

            if (setCount is not 0)
            {
                foreach (var userSet in bricksetUserSets)
                {
                    bricksetUser.Sets.Remove(userSet);
                }

                await repository.UpdateAsync(bricksetUser).ConfigureAwait(false);
            }

            return setCount;
        }

        public async Task<IEnumerable<string>> GetAllUsernames(BricksetUserType userType)
        {
            using var repository = _repositoryService.GetRepository();

            return (await repository
                .FetchAsync<BricksetUser>(bricksetUser => bricksetUser.UserType == userType).ConfigureAwait(false))
                .Select(bricksetUser => bricksetUser.BricksetUsername);
        }

        public async Task<BricksetUser> UpdateUserSynchronizationTimestamp(string username, DateTimeOffset userSynchronizationTimestamp)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            var bricksetUser = await Get(username).ConfigureAwait(false);

            if (bricksetUser is null)
            {
                return null;
            }

            bricksetUser.UserSynchronizationTimestamp = userSynchronizationTimestamp;

            using var repository = _repositoryService.GetRepository();

            await repository.UpdateAsync(bricksetUser).ConfigureAwait(false);

            return bricksetUser;
        }

        public async Task<IEnumerable<BricksetUserSet>> GetWantedSets(string username)
        {
            var bricksetUser = await Get(username).ConfigureAwait(false);

            if (bricksetUser is null)
            {
                return [];
            }

            return bricksetUser.Sets.Where(set => set.Wanted);
        }

        public async Task<IEnumerable<BricksetUserSet>> GetOwnedSets(string username)
        {
            var bricksetUser = await Get(username).ConfigureAwait(false);

            if (bricksetUser is null)
            {
                return [];
            }

            return bricksetUser.Sets.Where(set => set.Owned);
        }

        private static ILiteQueryableAsync<BricksetUser> GetQueryable(ILiteRepositoryAsync repository) => repository
                .Query<BricksetUser>()
                .IncludeAll();
    }
}
