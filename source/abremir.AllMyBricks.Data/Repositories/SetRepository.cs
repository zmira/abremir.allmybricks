using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using abremir.AllMyBricks.Data.Configuration;
using abremir.AllMyBricks.Data.Enumerations;
using abremir.AllMyBricks.Data.Extensions;
using abremir.AllMyBricks.Data.Interfaces;
using abremir.AllMyBricks.Data.Models;
using LiteDB;
using LiteDB.Async;

namespace abremir.AllMyBricks.Data.Repositories
{
    public class SetRepository(IRepositoryService repositoryService) : ISetRepository
    {
        private readonly IRepositoryService _repositoryService = repositoryService;

        public async Task<Set> AddOrUpdate(Set set)
        {
            if (set is null
                || set.SetId is 0)
            {
                return null;
            }

            set.TrimAllStrings();

            using var repository = _repositoryService.GetRepository();

            await repository.UpsertAsync(set).ConfigureAwait(false);

            return set;
        }

        public async Task<IEnumerable<Set>> All()
        {
            using var repository = _repositoryService.GetRepository();

            return await GetQueryable(repository).ToListAsync().ConfigureAwait(false);
        }

        public async Task<Set> Get(long setId)
        {
            if (setId is 0)
            {
                return null;
            }

            using var repository = _repositoryService.GetRepository();

            return await GetQueryable(repository)
                .Where(set => set.SetId == setId)
                .FirstOrDefaultAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<Set>> AllForTheme(string themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName))
            {
                return [];
            }

            using var repository = _repositoryService.GetRepository();

            return await GetQueryable(repository)
                .Where(set => set.Theme.Name == themeName.Trim())
                .ToListAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<Set>> AllForSubtheme(string themeName, string subthemeName)
        {
            if (string.IsNullOrWhiteSpace(themeName)
                || string.IsNullOrWhiteSpace(subthemeName))
            {
                return [];
            }

            using var repository = _repositoryService.GetRepository();

            return await GetQueryable(repository)
                .Where(set => set.Theme.Name == themeName.Trim() && set.Subtheme.Name == subthemeName.Trim())
                .ToListAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<Set>> AllForThemeGroup(string themeGroupName)
        {
            if (string.IsNullOrWhiteSpace(themeGroupName))
            {
                return [];
            }

            using var repository = _repositoryService.GetRepository();

            return await GetQueryable(repository)
                .Where(set => set.ThemeGroup.Value == themeGroupName.Trim())
                .ToListAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<Set>> AllForCategory(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return [];
            }

            using var repository = _repositoryService.GetRepository();

            return await GetQueryable(repository)
                .Where(set => set.Category.Value == categoryName.Trim())
                .ToListAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<Set>> AllForTag(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return [];
            }

            using var repository = _repositoryService.GetRepository();

            return await GetQueryable(repository)
                .Where("Tags[*].Value ANY = @0", tagName.Trim())
                .ToListAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<Set>> AllForYear(short year)
        {
            if (year < Constants.MinimumSetYear)
            {
                return [];
            }

            using var repository = _repositoryService.GetRepository();

            return await GetQueryable(repository)
                .Where(set => set.Year == year)
                .ToListAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<Set>> AllForPriceRange(PriceRegion priceRegion, float minimumPrice, float maximumPrice)
        {
            if (minimumPrice < 0 || maximumPrice < 0)
            {
                return [];
            }

            using var repository = _repositoryService.GetRepository();

            return await GetQueryable(repository)
                .Where("Prices[*].Region ANY = @0", priceRegion.ToString())
                .Where("Prices[*].Value ANY >= @0", minimumPrice)
                .Where("Prices[*].Value ANY <= @0", maximumPrice)
                .ToListAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<Set>> SearchBy(string searchQuery)
        {
            var queryBsonExpression = BuildBsonExpressionFromSearchQuery(searchQuery);

            if (queryBsonExpression is null)
            {
                return [];
            }

            using var repository = _repositoryService.GetRepository();

            return await GetQueryable(repository)
                .Where(queryBsonExpression)
                .ToListAsync().ConfigureAwait(false);
        }

        public async Task<int> Count()
        {
            using var repository = _repositoryService.GetRepository();

            return await repository.Query<Set>().CountAsync().ConfigureAwait(false);
        }

        public async Task<int> DeleteMany(List<long> setIds)
        {
            if ((setIds?.Count ?? 0) is 0)
            {
                return 0;
            }

            using var repository = _repositoryService.GetRepository();

            return await repository.DeleteManyAsync<Set>(set => setIds.Contains(set.SetId)).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Set>> Find(Expression<Func<Set, bool>> predicate)
        {
            using var repository = _repositoryService.GetRepository();

            return await GetQueryable(repository)
                .Where(predicate)
                .ToListAsync().ConfigureAwait(false);
        }

        private static BsonExpression BuildBsonExpressionFromSearchQuery(string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return null;
            }

            Dictionary<string, string> queryList = [];
            List<string> searchTerms = [.. searchQuery.Split(' ', '-').Where(term => (term?.Trim().Length ?? 0) >= Constants.MinimumSearchQuerySize).Distinct()];

            for (int i = 0; i < searchTerms.Count; i++)
            {
                var queryString = $"Number LIKE @{i}" +
                    $" OR Name LIKE @{i}" +
                    $" OR Theme.Name LIKE @{i}" +
                    $" OR Subtheme.Name LIKE @{i}" +
                    $" OR ThemeGroup.Value LIKE @{i}" +
                    $" OR PackagingType.Value LIKE @{i}" +
                    $" OR Category.Value LIKE @{i}" +
                    $" OR Barcodes[*].Value ANY LIKE @{i}" +
                    $" OR Tags[*].Value ANY LIKE @{i}";

                queryList.Add($"%{searchTerms[i]}%", queryString);
            }

            return queryList.Keys.Count is 0
                ? null
                : BsonExpression.Create(string.Join(" OR ", queryList.Values), queryList.Keys.Select(key => new BsonValue(key)).ToArray());
        }

        private static ILiteQueryableAsync<Set> GetQueryable(ILiteRepositoryAsync repository) => repository
                .Query<Set>()
                .IncludeAll();
    }
}
