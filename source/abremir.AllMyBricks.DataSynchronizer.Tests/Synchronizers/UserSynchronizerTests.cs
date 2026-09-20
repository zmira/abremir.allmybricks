using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using abremir.AllMyBricks.Data.Enumerations;
using abremir.AllMyBricks.Data.Models;
using abremir.AllMyBricks.Data.Repositories;
using abremir.AllMyBricks.DataSynchronizer.Extensions;
using abremir.AllMyBricks.DataSynchronizer.Synchronizers;
using abremir.AllMyBricks.DataSynchronizer.Tests.Configuration;
using abremir.AllMyBricks.DataSynchronizer.Tests.Shared;
using abremir.AllMyBricks.ThirdParty.Brickset.Interfaces;
using abremir.AllMyBricks.ThirdParty.Brickset.Models;
using abremir.AllMyBricks.ThirdParty.Brickset.Models.Parameters;
using Easy.MessageHub;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NFluent;
using NSubstitute;

namespace abremir.AllMyBricks.DataSynchronizer.Tests.Synchronizers
{
    [TestClass]
    public class UserSynchronizerTests : DataSynchronizerTestsBase
    {
        private static BricksetUserRepository _bricksetUserRepository;
        private static ThemeRepository _themeRepository;
        private static SubthemeRepository _subthemeRepository;
        private static SetRepository _setRepository;

        public UserSynchronizerTests()
        {
            _bricksetUserRepository = new BricksetUserRepository(MemoryRepositoryService);
            _themeRepository = new ThemeRepository(MemoryRepositoryService);
            _subthemeRepository = new SubthemeRepository(MemoryRepositoryService);
            _setRepository = new SetRepository(MemoryRepositoryService);
        }

        [TestMethod]
        public async Task SynchronizeBricksetPrimaryUser_UserSynchronizationTimestampNotSetAndDoesNotHaveRemoteSets_DoesNotUpdateLocalSetsAndUpdatesUserSynchronizationTimestamp()
        {
            const string testUser = "TESTUSER";
            await _bricksetUserRepository.Add(BricksetUserType.Primary, testUser);

            var bricksetApiService = Substitute.For<IBricksetApiService>();
            bricksetApiService.GetSets(Arg.Is<GetSetsParameters>(parameter => parameter.Owned.Value)).Returns([]);
            bricksetApiService.GetSets(Arg.Is<GetSetsParameters>(parameter => parameter.Wanted.Value)).Returns([]);

            var userSynchronizer = CreateTarget(bricksetApiService);

            await userSynchronizer.SynchronizeBricksetPrimaryUser(string.Empty, testUser, string.Empty);

            var user = await _bricksetUserRepository.Get(testUser);

            Check.That(user.Sets).IsEmpty();
            Check.That(user.UserSynchronizationTimestamp).HasAValue();
        }

        [TestMethod]
        public async Task SynchronizeBricksetPrimaryUser_SynchronizationTimestampNotSetAndHasRemoteSets_UpdatesLocalSetsAndUpdatesUserSynchronizationTimestamp()
        {
            const string apiKey = "APIKEY";
            const string userHash = "USERHASH";
            const string testUser = "TESTUSER";
            await _bricksetUserRepository.Add(BricksetUserType.Primary, testUser);

            var themesList = JsonSerializer.Deserialize<List<Themes>>(
                GetResultFileFromResource(Constants.JsonFileGetThemes),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);
            var subthemesList = JsonSerializer.Deserialize<List<Subthemes>>(
                GetResultFileFromResource(Constants.JsonFileGetSubthemes),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);
            var setsList = JsonSerializer.Deserialize<List<Sets>>(
                GetResultFileFromResource(Constants.JsonFileGetSets),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);

            var testSetOwned = setsList[0];
            testSetOwned.Collection = new SetCollection
            {
                Owned = true,
                QtyOwned = 2
            };

            var ownedTheme = themesList.First(theme => theme.Theme == testSetOwned.Theme).ToTheme();

            ownedTheme = await _themeRepository.AddOrUpdate(ownedTheme);

            var ownedSubtheme = subthemesList.First(subtheme => subtheme.Theme == testSetOwned.Theme && subtheme.Subtheme == testSetOwned.Subtheme).ToSubtheme();
            ownedSubtheme.Theme = ownedTheme;

            ownedSubtheme = await _subthemeRepository.AddOrUpdate(ownedSubtheme);

            var ownedSet = testSetOwned.ToSet();
            ownedSet.Theme = ownedTheme;
            ownedSet.Subtheme = ownedSubtheme;

            await _setRepository.AddOrUpdate(ownedSet);

            var testSetWanted = setsList[1];
            testSetWanted.Collection = new SetCollection
            {
                Wanted = true
            };

            var wantedTheme = themesList.First(theme => theme.Theme == testSetWanted.Theme).ToTheme();

            wantedTheme = wantedTheme.Name == ownedTheme.Name
                ? ownedTheme
                : await _themeRepository.AddOrUpdate(wantedTheme);

            var wantedSubtheme = subthemesList.First(subtheme => subtheme.Theme == testSetWanted.Theme && subtheme.Subtheme == testSetWanted.Subtheme).ToSubtheme();
            wantedSubtheme.Theme = wantedTheme;

            wantedSubtheme = wantedSubtheme.Name == ownedSubtheme.Name && wantedSubtheme.Theme.Name == ownedSubtheme.Theme.Name
                ? wantedSubtheme = ownedSubtheme
                : await _subthemeRepository.AddOrUpdate(wantedSubtheme);

            var wantedSet = testSetWanted.ToSet();
            wantedSet.Theme = wantedTheme;
            wantedSet.Subtheme = wantedSubtheme;

            await _setRepository.AddOrUpdate(wantedSet);

            var bricksetApiService = Substitute.For<IBricksetApiService>();
            bricksetApiService.GetSets(Arg.Is<GetSetsParameters>(parameter => parameter.Owned.Value)).Returns([testSetOwned]);
            bricksetApiService.GetSets(Arg.Is<GetSetsParameters>(parameter => parameter.Wanted.Value)).Returns([testSetWanted]);

            var userSynchronizer = CreateTarget(bricksetApiService);

            await userSynchronizer.SynchronizeBricksetPrimaryUser(apiKey, testUser, userHash);

            var user = await _bricksetUserRepository.Get(testUser);

            Check.That(user.Sets).Not.IsEmpty();
            Check.That(user.Sets.Where(userSet => userSet.Set.SetId == testSetOwned.SetId && userSet.Owned == testSetOwned.Collection.Owned && userSet.QuantityOwned == testSetOwned.Collection.QtyOwned)).Not.IsEmpty();
            Check.That(user.Sets.Where(userSet => userSet.Set.SetId == testSetWanted.SetId && userSet.Wanted == testSetWanted.Collection.Wanted)).Not.IsEmpty();
            Check.That(user.UserSynchronizationTimestamp).HasAValue();
        }

        [TestMethod]
        public async Task SynchronizeBricksetPrimaryUser_SynchronizationTimestampSetAndDoesNotHaveSetsToUpdate_DoesNotUpdateRemoteCollection()
        {
            const string apiKey = "APIKEY";
            const string userHash = "USERHASH";
            const string testUser = "TESTUSER";
            await _bricksetUserRepository.Add(BricksetUserType.Primary, testUser);

            var themesList = JsonSerializer.Deserialize<List<Themes>>(
                GetResultFileFromResource(Constants.JsonFileGetThemes),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);
            var subthemesList = JsonSerializer.Deserialize<List<Subthemes>>(
                GetResultFileFromResource(Constants.JsonFileGetSubthemes),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);
            var setsList = JsonSerializer.Deserialize<List<Sets>>(
                GetResultFileFromResource(Constants.JsonFileGetSets),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);

            var testSetOwned = setsList[0];
            var ownedTheme = themesList.First(theme => theme.Theme == testSetOwned.Theme).ToTheme();
            var ownedSubtheme = subthemesList.First(subtheme => subtheme.Theme == testSetOwned.Theme && subtheme.Subtheme == testSetOwned.Subtheme).ToSubtheme();
            ownedSubtheme.Theme = ownedTheme;

            var ownedSet = testSetOwned.ToSet();
            ownedSet.Theme = ownedTheme;
            ownedSet.Subtheme = ownedSubtheme;

            await _themeRepository.AddOrUpdate(ownedTheme);
            await _subthemeRepository.AddOrUpdate(ownedSubtheme);
            ownedSet = await _setRepository.AddOrUpdate(ownedSet);

            var bricksetUserSet = new BricksetUserSet
            {
                Set = ownedSet,
                Owned = true,
                QuantityOwned = 2
            };

            await _bricksetUserRepository.AddOrUpdateSet(testUser, bricksetUserSet);

            await _bricksetUserRepository.UpdateUserSynchronizationTimestamp(testUser, DateTimeOffset.Now.AddSeconds(1));

            var bricksetApiService = Substitute.For<IBricksetApiService>();
            bricksetApiService.GetSets(Arg.Is<GetSetsParameters>(parameter => parameter.Owned.Value)).Returns([]);
            bricksetApiService.GetSets(Arg.Is<GetSetsParameters>(parameter => parameter.Wanted.Value)).Returns([]);

            var userSynchronizer = CreateTarget(bricksetApiService);

            await userSynchronizer.SynchronizeBricksetPrimaryUser(apiKey, testUser, userHash);

            var user = await _bricksetUserRepository.Get(testUser);

            await bricksetApiService.DidNotReceive().SetCollection(Arg.Any<SetCollectionParameters>());
            Check.That(user.Sets).Not.IsEmpty().And.CountIs(1);
            Check.That(user.Sets[0].Set.SetId).IsEqualTo(bricksetUserSet.Set.SetId);
            Check.That(user.Sets[0].QuantityOwned).IsEqualTo(bricksetUserSet.QuantityOwned);
            Check.That(user.UserSynchronizationTimestamp).HasAValue();
        }

        [TestMethod]
        public async Task SynchronizeBricksetPrimaryUser_SynchronizationTimestampSetAndHasSetsToUpdate_UpdatesRemoteCollection()
        {
            const string apiKey = "APIKEY";
            const string userHash = "USERHASH";
            const string testUser = "TESTUSER";
            await _bricksetUserRepository.Add(BricksetUserType.Primary, testUser);

            var themesList = JsonSerializer.Deserialize<List<Themes>>(
                GetResultFileFromResource(Constants.JsonFileGetThemes),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);
            var subthemesList = JsonSerializer.Deserialize<List<Subthemes>>(
                GetResultFileFromResource(Constants.JsonFileGetSubthemes),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);
            var setsList = JsonSerializer.Deserialize<List<Sets>>(
                GetResultFileFromResource(Constants.JsonFileGetSets),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);

            var testSetOwned = setsList[0];
            var ownedTheme = themesList.First(theme => theme.Theme == testSetOwned.Theme).ToTheme();
            var ownedSubtheme = subthemesList.First(subtheme => subtheme.Theme == testSetOwned.Theme && subtheme.Subtheme == testSetOwned.Subtheme).ToSubtheme();
            ownedSubtheme.Theme = ownedTheme;

            var ownedSet = testSetOwned.ToSet();
            ownedSet.Theme = ownedTheme;
            ownedSet.Subtheme = ownedSubtheme;

            await _themeRepository.AddOrUpdate(ownedTheme);
            await _subthemeRepository.AddOrUpdate(ownedSubtheme);
            ownedSet = await _setRepository.AddOrUpdate(ownedSet);

            await _bricksetUserRepository.AddOrUpdateSet(testUser, new BricksetUserSet
            {
                Set = ownedSet,
                Owned = true,
                QuantityOwned = 2
            });

            await _bricksetUserRepository.UpdateUserSynchronizationTimestamp(testUser, DateTimeOffset.Now.AddSeconds(-1));

            var bricksetUserSet = new BricksetUserSet
            {
                Set = ownedSet,
                Owned = true,
                QuantityOwned = 1
            };

            await _bricksetUserRepository.AddOrUpdateSet(testUser, bricksetUserSet);

            var bricksetApiService = Substitute.For<IBricksetApiService>();
            bricksetApiService.GetSets(Arg.Is<GetSetsParameters>(parameter => parameter.Owned.Value)).Returns([]);
            bricksetApiService.GetSets(Arg.Is<GetSetsParameters>(parameter => parameter.Wanted.Value)).Returns([]);

            var userSynchronizer = CreateTarget(bricksetApiService);

            await userSynchronizer.SynchronizeBricksetPrimaryUser(apiKey, testUser, userHash);

            var user = await _bricksetUserRepository.Get(testUser);

            await bricksetApiService.Received().SetCollection(Arg.Any<SetCollectionParameters>());
            Check.That(user.Sets).Not.IsEmpty().And.CountIs(1);
            Check.That(user.Sets[0].Set.SetId).IsEqualTo(bricksetUserSet.Set.SetId);
            Check.That(user.Sets[0].QuantityOwned).IsEqualTo(bricksetUserSet.QuantityOwned);
            Check.That(user.UserSynchronizationTimestamp).HasAValue();
        }

        [TestMethod]
        public async Task SynchronizeBricksetPrimaryUser_SynchronizationTimestampSetAndHasSetsToUpdateAndHasNewRemoteSets_UpdatesRemoteCollectionAndAddRemoteSetToLocalExceptAlreadyLocalSets()
        {
            const string apiKey = "APIKEY";
            const string userHash = "USERHASH";
            const string testUser = "TESTUSER";
            await _bricksetUserRepository.Add(BricksetUserType.Primary, testUser);

            var themesList = JsonSerializer.Deserialize<List<Themes>>(
                GetResultFileFromResource(Constants.JsonFileGetThemes),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);
            var subthemesList = JsonSerializer.Deserialize<List<Subthemes>>(
                GetResultFileFromResource(Constants.JsonFileGetSubthemes),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);
            var setsList = JsonSerializer.Deserialize<List<Sets>>(
                GetResultFileFromResource(Constants.JsonFileGetSets),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);

            var testSetOwned = setsList[0];
            var ownedTheme = themesList.First(theme => theme.Theme == testSetOwned.Theme).ToTheme();

            ownedTheme = await _themeRepository.AddOrUpdate(ownedTheme);

            var ownedSubtheme = subthemesList.First(subtheme => subtheme.Theme == testSetOwned.Theme && subtheme.Subtheme == testSetOwned.Subtheme).ToSubtheme();
            ownedSubtheme.Theme = ownedTheme;

            ownedSubtheme = await _subthemeRepository.AddOrUpdate(ownedSubtheme);

            var ownedSet = testSetOwned.ToSet();
            ownedSet.Theme = ownedTheme;
            ownedSet.Subtheme = ownedSubtheme;

            ownedSet = await _setRepository.AddOrUpdate(ownedSet);

            var testSetWanted = setsList[1];
            testSetWanted.Collection = new SetCollection
            {
                Wanted = true
            };

            var wantedTheme = themesList.First(theme => theme.Theme == testSetWanted.Theme).ToTheme();

            wantedTheme = wantedTheme.Name == ownedTheme.Name
                ? ownedTheme
                : await _themeRepository.AddOrUpdate(wantedTheme);

            var wantedSubtheme = subthemesList.First(subtheme => subtheme.Theme == testSetWanted.Theme && subtheme.Subtheme == testSetWanted.Subtheme).ToSubtheme();
            wantedSubtheme.Theme = wantedTheme;

            wantedSubtheme = wantedSubtheme.Name == ownedSubtheme.Name && wantedSubtheme.Theme.Name == ownedSubtheme.Theme.Name
                ? wantedSubtheme = ownedSubtheme
                : await _subthemeRepository.AddOrUpdate(wantedSubtheme);

            var wantedSet = testSetWanted.ToSet();
            wantedSet.Theme = wantedTheme;
            wantedSet.Subtheme = wantedSubtheme;

            wantedSet = await _setRepository.AddOrUpdate(wantedSet);

            await _bricksetUserRepository.AddOrUpdateSet(testUser, new BricksetUserSet
            {
                Set = ownedSet,
                Owned = true,
                QuantityOwned = 2
            });

            await _bricksetUserRepository.UpdateUserSynchronizationTimestamp(testUser, DateTimeOffset.Now.AddSeconds(-1));

            var bricksetUserSet = new BricksetUserSet
            {
                Set = ownedSet,
                Owned = true,
                QuantityOwned = 1
            };

            await _bricksetUserRepository.AddOrUpdateSet(testUser, bricksetUserSet);

            var bricksetApiService = Substitute.For<IBricksetApiService>();
            bricksetApiService.GetSets(Arg.Is<GetSetsParameters>(parameter => parameter.Owned.Value)).Returns([]);
            bricksetApiService.GetSets(Arg.Is<GetSetsParameters>(parameter => parameter.Wanted.Value)).Returns([testSetWanted]);

            var userSynchronizer = CreateTarget(bricksetApiService);

            await userSynchronizer.SynchronizeBricksetPrimaryUser(apiKey, testUser, userHash);

            var user = await _bricksetUserRepository.Get(testUser);

            await bricksetApiService.Received().SetCollection(Arg.Any<SetCollectionParameters>());
            Check.That(user.Sets).Not.IsEmpty();
            Check.That(user.Sets.Where(userSet => userSet.Set.SetId == bricksetUserSet.Set.SetId && userSet.Owned == bricksetUserSet.Owned && userSet.QuantityOwned == bricksetUserSet.QuantityOwned)).Not.IsEmpty();
            Check.That(user.Sets.Where(userSet => userSet.Set.SetId == testSetWanted.SetId && userSet.Wanted == testSetWanted.Collection.Wanted)).Not.IsEmpty();
            Check.That(user.UserSynchronizationTimestamp).HasAValue();
        }

        [TestMethod]
        public async Task SynchronizeBricksetFriend_DoesNotHaveRemoteSets_DoesNotUpdateLocalSetsAndUpdatesUserSynchronizationTimestamp()
        {
            const string testUser = "TESTFRIEND";
            await _bricksetUserRepository.Add(BricksetUserType.Friend, testUser);

            var bricksetApiService = Substitute.For<IBricksetApiService>();
            bricksetApiService.GetSets(Arg.Is<GetSetsParameters>(parameter => parameter.Owned.Value)).Returns([]);
            bricksetApiService.GetSets(Arg.Is<GetSetsParameters>(parameter => parameter.Wanted.Value)).Returns([]);

            var userSynchronizer = CreateTarget(bricksetApiService);

            await userSynchronizer.SynchronizeBricksetPrimaryUser(string.Empty, testUser, string.Empty);

            var user = await _bricksetUserRepository.Get(testUser);

            Check.That(user.Sets).IsEmpty();
            Check.That(user.UserSynchronizationTimestamp).HasAValue();
        }

        [TestMethod]
        public async Task SynchronizeBricksetFriend_HasRemoteSets_UpdatesLocalSetsAndUpdatesUserSynchronizationTimestamp()
        {
            const string apiKey = "APIKEY";
            const string userHash = "USERHASH";
            const string testUser = "TESTFRIEND";
            await _bricksetUserRepository.Add(BricksetUserType.Friend, testUser);

            var themesList = JsonSerializer.Deserialize<List<Themes>>(
                GetResultFileFromResource(Constants.JsonFileGetThemes),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);
            var subthemesList = JsonSerializer.Deserialize<List<Subthemes>>(
                GetResultFileFromResource(Constants.JsonFileGetSubthemes),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);
            var setsList = JsonSerializer.Deserialize<List<Sets>>(
                GetResultFileFromResource(Constants.JsonFileGetSets),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);

            var testSetOwned = setsList[0];
            testSetOwned.Collection = new SetCollection
            {
                Owned = true,
                QtyOwned = 2
            };

            var ownedTheme = themesList.First(theme => theme.Theme == testSetOwned.Theme).ToTheme();

            ownedTheme = await _themeRepository.AddOrUpdate(ownedTheme);

            var ownedSubtheme = subthemesList.First(subtheme => subtheme.Theme == testSetOwned.Theme && subtheme.Subtheme == testSetOwned.Subtheme).ToSubtheme();
            ownedSubtheme.Theme = ownedTheme;

            ownedSubtheme = await _subthemeRepository.AddOrUpdate(ownedSubtheme);

            var ownedSet = testSetOwned.ToSet();
            ownedSet.Theme = ownedTheme;
            ownedSet.Subtheme = ownedSubtheme;

            await _setRepository.AddOrUpdate(ownedSet);

            var testSetWanted = setsList[1];
            testSetWanted.Collection = new SetCollection
            {
                Wanted = true
            };

            var wantedTheme = themesList.First(theme => theme.Theme == testSetWanted.Theme).ToTheme();

            wantedTheme = wantedTheme.Name == ownedTheme.Name
                ? ownedTheme
                : await _themeRepository.AddOrUpdate(wantedTheme);

            var wantedSubtheme = subthemesList.First(subtheme => subtheme.Theme == testSetWanted.Theme && subtheme.Subtheme == testSetWanted.Subtheme).ToSubtheme();
            wantedSubtheme.Theme = wantedTheme;

            wantedSubtheme = wantedSubtheme.Name == ownedSubtheme.Name && wantedSubtheme.Theme.Name == ownedSubtheme.Theme.Name
                ? wantedSubtheme = ownedSubtheme
                : await _subthemeRepository.AddOrUpdate(wantedSubtheme);

            var wantedSet = testSetWanted.ToSet();
            wantedSet.Theme = wantedTheme;
            wantedSet.Subtheme = wantedSubtheme;

            await _setRepository.AddOrUpdate(wantedSet);

            var bricksetApiService = Substitute.For<IBricksetApiService>();
            bricksetApiService.GetSets(Arg.Is<GetSetsParameters>(parameter => parameter.Owned.Value)).Returns([testSetOwned]);
            bricksetApiService.GetSets(Arg.Is<GetSetsParameters>(parameter => parameter.Wanted.Value)).Returns([testSetWanted]);

            var userSynchronizer = CreateTarget(bricksetApiService);

            await userSynchronizer.SynchronizeBricksetPrimaryUser(apiKey, testUser, userHash);

            var user = await _bricksetUserRepository.Get(testUser);

            Check.That(user.Sets).Not.IsEmpty();
            Check.That(user.Sets.Where(userSet => userSet.Set.SetId == testSetOwned.SetId && userSet.Owned == testSetOwned.Collection.Owned && userSet.QuantityOwned == testSetOwned.Collection.QtyOwned)).Not.IsEmpty();
            Check.That(user.Sets.Where(userSet => userSet.Set.SetId == testSetWanted.SetId && userSet.Wanted == testSetWanted.Collection.Wanted)).Not.IsEmpty();
            Check.That(user.UserSynchronizationTimestamp).HasAValue();
        }

        private static UserSynchronizer CreateTarget(IBricksetApiService bricksetApiService = null)
        {
            bricksetApiService ??= Substitute.For<IBricksetApiService>();

            return new UserSynchronizer(bricksetApiService, _bricksetUserRepository, Substitute.For<IMessageHub>(), _setRepository);
        }
    }
}
