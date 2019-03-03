﻿using abremir.AllMyBricks.Data.Enumerations;
using abremir.AllMyBricks.Data.Extensions;
using abremir.AllMyBricks.Data.Interfaces;
using abremir.AllMyBricks.Data.Models;
using Realms;
using System;
using System.Collections.Generic;
using System.Linq;
using Managed = abremir.AllMyBricks.Data.Models.Realm;

namespace abremir.AllMyBricks.Data.Repositories
{
    public class BricksetUserRepository : IBricksetUserRepository
    {
        private readonly IRepositoryService _repositoryService;

        public BricksetUserRepository(IRepositoryService repositoryService)
        {
            _repositoryService = repositoryService;
        }

        public BricksetUser Add(string username, BricksetUserTypeEnum userType)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            var existingBricksetUser = Get(username);

            if (existingBricksetUser != null)
            {
                return existingBricksetUser;
            }

            var repository = _repositoryService.GetRepository();

            var managedBricksetUser = new BricksetUser
            {
                BricksetUsername = username,
                UserType = userType,
                Sets = new List<BricksetUserSet>()
            }.ToRealmObject();

            repository.Write(() => repository.Add(managedBricksetUser));

            return managedBricksetUser.ToPlainObject();
        }

        public BricksetUser Get(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            return GetQueryable()
                .FirstOrDefault(bricksetUser => bricksetUser.BricksetUsername.Equals(username, StringComparison.OrdinalIgnoreCase))
                ?.ToPlainObject();
        }

        public BricksetUserSet AddOrUpdateSet(string username, BricksetUserSet bricksetUserSet)
        {
            if (string.IsNullOrWhiteSpace(username)
                || bricksetUserSet == null
                || bricksetUserSet.SetId == 0
                || _repositoryService.GetRepository().All<Managed.Set>().FirstOrDefault(set => set.SetId == bricksetUserSet.SetId) == null)
            {
                return null;
            }

            var bricksetUser = Get(username);

            if(bricksetUser == null)
            {
                return null;
            }

            var existingBricksetUserSet = bricksetUser.Sets.FirstOrDefault(set => set.SetId == bricksetUserSet.SetId);

            if(existingBricksetUserSet != null)
            {
                if (existingBricksetUserSet.Owned == bricksetUserSet.Owned
                    && existingBricksetUserSet.Wanted == bricksetUserSet.Wanted
                    && existingBricksetUserSet.QuantityOwned == bricksetUserSet.QuantityOwned)
                {
                    return existingBricksetUserSet;
                }

                bricksetUser.Sets.Remove(existingBricksetUserSet);
            }

            if (bricksetUserSet.LastChangeTimestamp == DateTimeOffset.MinValue)
            {
                bricksetUserSet.LastChangeTimestamp = DateTimeOffset.Now;
            }

            bricksetUser.Sets.Add(bricksetUserSet);

            var repository = _repositoryService.GetRepository();

            var managedBricksetUser = bricksetUser.ToRealmObject();

            repository.Write(() => repository.Add(managedBricksetUser, true));

            return managedBricksetUser
                .Sets
                .FirstOrDefault(set => set.SetId == bricksetUserSet.SetId)
                ?.ToPlainObject();
        }

        public BricksetUserSet GetSet(string username, long setId)
        {
            if (string.IsNullOrWhiteSpace(username)
                || setId == 0)
            {
                return null;
            }

            return GetQueryable()
                .Filter($"BricksetUsername ==[c] \"{username}\" && Sets.SetId == {setId}")
                .FirstOrDefault()
                ?.Sets[0]
                .ToPlainObject();
        }

        private IQueryable<Managed.BricksetUser> GetQueryable()
        {
            return _repositoryService
                .GetRepository()
                .All<Managed.BricksetUser>();
        }
    }
}
