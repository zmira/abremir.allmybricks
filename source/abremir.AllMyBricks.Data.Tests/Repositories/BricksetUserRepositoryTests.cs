using System;
using System.Linq;
using System.Threading.Tasks;
using abremir.AllMyBricks.Data.Enumerations;
using abremir.AllMyBricks.Data.Models;
using abremir.AllMyBricks.Data.Repositories;
using abremir.AllMyBricks.Data.Tests.Configuration;
using abremir.AllMyBricks.Data.Tests.Shared;
using abremir.AllMyBricks.Onboarding.Shared.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NFluent;

namespace abremir.AllMyBricks.Data.Tests.Repositories
{
    [TestClass]
    public class BricksetUserRepositoryTests : DataTestsBase
    {
        private static BricksetUserRepository _bricksetUserRepository;

        public BricksetUserRepositoryTests()
        {
            _bricksetUserRepository = new BricksetUserRepository(MemoryRepositoryService);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(ModelsSetup.StringEmpty)]
        public async Task Add_InvalidUsername_ReturnsNull(string username)
        {
            var bricksetUser = await _bricksetUserRepository.Add(BricksetUserType.None, username);

            Check.That(bricksetUser).IsNull();
        }

        [TestMethod]
        public async Task Add_BricksetUserAlreadyExists_ReturnsModel()
        {
            var bricksetUserUnderTest = ModelsSetup.GetBricksetUserUnderTest();

            await InsertData(bricksetUserUnderTest);

            var bricksetUser = await _bricksetUserRepository.Add(bricksetUserUnderTest.UserType, bricksetUserUnderTest.BricksetUsername);

            Check.That(bricksetUser.BricksetUsername).IsEqualTo(bricksetUserUnderTest.BricksetUsername);
        }

        [TestMethod]
        public async Task Add_BricksetUserDoesNotExist_InsertsAndReturnsModel()
        {
            var bricksetUserUnderTest = ModelsSetup.GetBricksetUserUnderTest();

            await InsertData(bricksetUserUnderTest);

            const string newUsername = "new username";

            var bricksetUser = await _bricksetUserRepository.Add(bricksetUserUnderTest.UserType, newUsername);

            Check.That(bricksetUser.BricksetUsername).IsEqualTo(newUsername);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(ModelsSetup.StringEmpty)]
        public async Task Get_InvalidUsername_ReturnsNull(string username)
        {
            var bricksetUser = await _bricksetUserRepository.Get(username);

            Check.That(bricksetUser).IsNull();
        }

        [TestMethod]
        public async Task Get_BricksetUserDoesNotExist_ReturnsNull()
        {
            var bricksetUserUnderTest = ModelsSetup.GetBricksetUserUnderTest();

            await InsertData(bricksetUserUnderTest);

            var bricksetUser = await _bricksetUserRepository.Get("username");

            Check.That(bricksetUser).IsNull();
        }

        [TestMethod]
        public async Task Get_BricksetUserExists_ReturnsModel()
        {
            var bricksetUserUnderTest = ModelsSetup.GetBricksetUserUnderTest();

            await InsertData(bricksetUserUnderTest);

            var bricksetUser = await _bricksetUserRepository.Get(bricksetUserUnderTest.BricksetUsername);

            Check.That(bricksetUser.BricksetUsername).IsEqualTo(bricksetUserUnderTest.BricksetUsername);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(ModelsSetup.StringEmpty)]
        public async Task Exists_InvalidUsername_ReturnsFalse(string username)
        {
            var bricksetUserExists = await _bricksetUserRepository.Exists(username);

            Check.That(bricksetUserExists).IsFalse();
        }

        [TestMethod]
        public async Task Exists_ValidUsernameAndDoesNotExist_ReturnsFalse()
        {
            var bricksetUserUnderTest = ModelsSetup.GetBricksetUserUnderTest();

            await InsertData(bricksetUserUnderTest);

            var bricksetUserExists = await _bricksetUserRepository.Exists(Guid.NewGuid().ToString());

            Check.That(bricksetUserExists).IsFalse();
        }

        [TestMethod]
        public async Task Exists_ValidUsernameAndExists_ReturnsTrue()
        {
            var bricksetUserUnderTest = ModelsSetup.GetBricksetUserUnderTest();

            await InsertData(bricksetUserUnderTest);

            var bricksetUserExists = await _bricksetUserRepository.Exists(bricksetUserUnderTest.BricksetUsername);

            Check.That(bricksetUserExists).IsTrue();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(ModelsSetup.StringEmpty)]
        public async Task Remove_InvalidUsername_ReturnsFalse(string username)
        {
            var removedUser = await _bricksetUserRepository.Remove(username);

            Check.That(removedUser).IsFalse();
        }

        [TestMethod]
        public async Task Remove_ValidUsernameAndDoesNotExist_ReturnsFalse()
        {
            var bricksetUserUnderTest = ModelsSetup.GetBricksetUserUnderTest();

            await InsertData(bricksetUserUnderTest);

            var removedUser = await _bricksetUserRepository.Remove(Guid.NewGuid().ToString());

            Check.That(removedUser).IsFalse();
        }

        [TestMethod]
        public async Task Remove_ValidUsernameAndExists_ReturnsTrueAndRemovesUser()
        {
            var bricksetUserUnderTest = ModelsSetup.GetBricksetUserUnderTest();
            string bricksetUsernameUnderTest = bricksetUserUnderTest.BricksetUsername;

            await InsertData(bricksetUserUnderTest);

            var removedUser = await _bricksetUserRepository.Remove(bricksetUsernameUnderTest);

            Check.That(removedUser).IsTrue();
            Check.That(await _bricksetUserRepository.Exists(bricksetUsernameUnderTest)).IsFalse();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(ModelsSetup.StringEmpty)]
        public async Task AddOrUpdateSet_InvalidUsername_ReturnsNull(string username)
        {
            var set = ModelsSetup.GetSetUnderTest();
            var bricksetUser = ModelsSetup.GetBricksetUserUnderTest();

            await InsertData(set);
            await InsertData(bricksetUser);

            var bricksetUserSet = await _bricksetUserRepository.AddOrUpdateSet(username, new BricksetUserSet { Set = new Set { SetId = set.SetId } });

            Check.That(bricksetUserSet).IsNull();
        }

        [TestMethod]
        public async Task AddOrUpdateSet_NullBricksetUserSet_ReturnsNull()
        {
            var bricksetUser = ModelsSetup.GetBricksetUserUnderTest();

            await InsertData(bricksetUser);

            var bricksetUserSet = await _bricksetUserRepository.AddOrUpdateSet(bricksetUser.BricksetUsername, null);

            Check.That(bricksetUserSet).IsNull();
        }

        [TestMethod]
        public async Task AddOrUpdateSet_InvalidBricksetUserSet_ReturnsNull()
        {
            var bricksetUser = ModelsSetup.GetBricksetUserUnderTest();

            await InsertData(bricksetUser);

            var bricksetUserSet = await _bricksetUserRepository.AddOrUpdateSet(bricksetUser.BricksetUsername, new BricksetUserSet { Set = new Set { SetId = 0 } });

            Check.That(bricksetUserSet).IsNull();
        }

        [TestMethod]
        public async Task AddOrUpdateSet_BricksetUserDoesNotExist_ReturnsNull()
        {
            var set = ModelsSetup.GetSetUnderTest();

            await InsertData(set);

            var bricksetUserSet = await _bricksetUserRepository.AddOrUpdateSet("username", new BricksetUserSet { Set = new Set { SetId = set.SetId } });

            Check.That(bricksetUserSet).IsNull();
        }

        [TestMethod]
        public async Task AddOrUpdateSet_BricksetUserSetLinkedSetDoesNotExist_ReturnsNull()
        {
            var set = ModelsSetup.GetSetUnderTest();
            var bricksetUser = ModelsSetup.GetBricksetUserUnderTest();

            set = await InsertData(set);
            await InsertData(bricksetUser);

            var bricksetUserSet = await _bricksetUserRepository.AddOrUpdateSet(bricksetUser.BricksetUsername, new BricksetUserSet { Set = new Set { SetId = set.SetId + 1 } });

            Check.That(bricksetUserSet).IsNull();
        }

        [TestMethod]
        public async Task AddOrUpdateSet_BricksetUserSetDoesNotExist_InsertsAndReturnsModel()
        {
            var set = ModelsSetup.GetSetUnderTest();
            var bricksetUser = ModelsSetup.GetBricksetUserUnderTest();

            set = await InsertData(set);
            await InsertData(bricksetUser);

            var bricksetUserSetUnderTest = new BricksetUserSet
            {
                Set = set,
                Wanted = false,
                Owned = true,
                QuantityOwned = 10
            };

            var bricksetUserSet = await _bricksetUserRepository.AddOrUpdateSet(bricksetUser.BricksetUsername, bricksetUserSetUnderTest);

            Check.That(bricksetUserSet).HasFieldsWithSameValues(bricksetUserSetUnderTest);
        }

        [TestMethod]
        public async Task AddOrUpdateSet_BricksetUserSetExists_UpdatesAndReturnsModel()
        {
            var set = ModelsSetup.GetSetUnderTest();
            var bricksetUser = ModelsSetup.GetBricksetUserUnderTest();

            set = await InsertData(set);
            await InsertData(bricksetUser);

            var bricksetUserSetUnderTest = new BricksetUserSet
            {
                Set = set,
                Wanted = true
            };

            await _bricksetUserRepository.AddOrUpdateSet(bricksetUser.BricksetUsername, bricksetUserSetUnderTest);

            bricksetUserSetUnderTest.Wanted = false;
            bricksetUserSetUnderTest.Owned = true;
            bricksetUserSetUnderTest.QuantityOwned = 2;

            var bricksetUserSet = await _bricksetUserRepository.AddOrUpdateSet(bricksetUser.BricksetUsername, bricksetUserSetUnderTest);

            Check.That(bricksetUserSet).HasFieldsWithSameValues(bricksetUserSetUnderTest);
            Check.That(bricksetUserSet.Wanted).IsFalse();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(ModelsSetup.StringEmpty)]
        public async Task GetSet_InvalidUsername_ReturnsNull(string username)
        {
            var bricksetUser = ModelsSetup.GetBricksetUserUnderTest();

            await InsertData(bricksetUser);

            var bricksetUserSet = await _bricksetUserRepository.GetSet(username, 1);

            Check.That(bricksetUserSet).IsNull();
        }

        [TestMethod]
        public async Task GetSet_InvalidSetId_ReturnsNull()
        {
            var bricksetUser = ModelsSetup.GetBricksetUserUnderTest();

            await InsertData(bricksetUser);

            var bricksetUserSet = await _bricksetUserRepository.GetSet(bricksetUser.BricksetUsername, 0);

            Check.That(bricksetUserSet).IsNull();
        }

        [TestMethod]
        public async Task GetSet_BricksetUserDoesNotExist_ReturnsNull()
        {
            var bricksetUser = ModelsSetup.GetBricksetUserUnderTest();

            await InsertData(bricksetUser);

            var bricksetUserSet = await _bricksetUserRepository.GetSet("user name", 1);

            Check.That(bricksetUserSet).IsNull();
        }

        [TestMethod]
        public async Task GetSet_BricksetUserDoesNotHaveSetId_ReturnsNull()
        {
            var set = ModelsSetup.GetSetUnderTest();
            var bricksetUser = ModelsSetup.GetBricksetUserUnderTest();

            set = await InsertData(set);
            await InsertData(bricksetUser);

            var bricksetUserSetUnderTest = new BricksetUserSet
            {
                Set = set,
                Wanted = true
            };

            await _bricksetUserRepository.AddOrUpdateSet(bricksetUser.BricksetUsername, bricksetUserSetUnderTest);

            var bricksetUserSet = await _bricksetUserRepository.GetSet(bricksetUser.BricksetUsername, bricksetUserSetUnderTest.Set.SetId + 1);

            Check.That(bricksetUserSet).IsNull();
        }

        [TestMethod]
        public async Task GetSet_BricksetUserHasSetId_ReturnsModel()
        {
            var set = ModelsSetup.GetSetUnderTest();
            var bricksetUser = ModelsSetup.GetBricksetUserUnderTest();

            set = await InsertData(set);
            await InsertData(bricksetUser);

            var bricksetUserSetUnderTest = new BricksetUserSet
            {
                Set = set,
                Wanted = true
            };

            await _bricksetUserRepository.AddOrUpdateSet(bricksetUser.BricksetUsername, bricksetUserSetUnderTest);

            var bricksetUserSet = await _bricksetUserRepository.GetSet(bricksetUser.BricksetUsername, bricksetUserSetUnderTest.Set.SetId);

            Check.That(bricksetUserSet.Set.SetId).IsEqualTo(bricksetUserSetUnderTest.Set.SetId);
        }

        [TestMethod]
        public async Task GetAllUsernames_BricksetUserTypeDoesNotExist_ReturnsEmptyList()
        {
            var bricksetUser = ModelsSetup.GetBricksetUserUnderTest();

            await InsertData(bricksetUser);

            var usernameList = await _bricksetUserRepository.GetAllUsernames(BricksetUserType.Friend);

            Check.That(usernameList).IsEmpty();
        }

        [TestMethod]
        public async Task GetAllUsernames_BricksetUserTypeExists_ReturnsListOfUsernames()
        {
            var bricksetUser = ModelsSetup.GetBricksetUserUnderTest();

            await InsertData(bricksetUser);

            var usernameList = await _bricksetUserRepository.GetAllUsernames(bricksetUser.UserType);

            Check.That(usernameList).Not.IsEmpty();
            Check.That(usernameList.First()).IsEqualTo(bricksetUser.BricksetUsername);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(ModelsSetup.StringEmpty)]
        public async Task UpdateUserSynchronizationTimestamp_InvalidUsername_ReturnsNull(string username)
        {
            var bricksetUser = await _bricksetUserRepository.UpdateUserSynchronizationTimestamp(username, DateTimeOffset.Now);

            Check.That(bricksetUser).IsNull();
        }

        [TestMethod]
        public async Task UpdateUserSynchronizationTimestamp_BricksetUserDoesNotExist_ReturnsNull()
        {
            var bricksetUserUnderTest = ModelsSetup.GetBricksetUserUnderTest();

            await InsertData(bricksetUserUnderTest);

            var bricksetUser = await _bricksetUserRepository.UpdateUserSynchronizationTimestamp(Guid.NewGuid().ToString(), DateTimeOffset.Now);

            Check.That(bricksetUser).IsNull();
        }

        [TestMethod]
        public async Task UpdateUserSynchronizationTimestamp_BricksetUserExists_UpdatesUserSynchronizationTimestampAndReturnsModel()
        {
            var bricksetUserUnderTest = ModelsSetup.GetBricksetUserUnderTest();

            await InsertData(bricksetUserUnderTest);

            var synchronizationTimestamp = DateTimeOffset.Now.AddHours(-2);

            var bricksetUser = await _bricksetUserRepository.UpdateUserSynchronizationTimestamp(bricksetUserUnderTest.BricksetUsername, synchronizationTimestamp);

            Check.That(bricksetUser).IsNotNull();
            Check.That(bricksetUser.UserSynchronizationTimestamp).IsEqualTo(synchronizationTimestamp.ToHundredthOfSecond());
        }

        [TestMethod]
        public async Task GetWantedSets_BricksetUserDoesNotExist_ReturnsEmptyList()
        {
            var bricksetUserSetList = await _bricksetUserRepository.GetWantedSets(string.Empty);

            Check.That(bricksetUserSetList).IsEmpty();
        }

        [TestMethod]
        public async Task GetWantedSets_BricksetUserExists_ReturnsListOfWantedSets()
        {
            var wantedSet = ModelsSetup.GetSetUnderTest();
            var ownedSet = ModelsSetup.GetSecondSetUnderTest();
            var bricksetUser = ModelsSetup.GetBricksetUserUnderTest();

            wantedSet = await InsertData(wantedSet);
            ownedSet = await InsertData(ownedSet);
            await InsertData(bricksetUser);

            var bricksetUserSetWanted = new BricksetUserSet
            {
                Set = wantedSet,
                Wanted = true
            };

            var bricksetUserSetOwned = new BricksetUserSet
            {
                Set = ownedSet,
                Owned = true
            };

            await _bricksetUserRepository.AddOrUpdateSet(bricksetUser.BricksetUsername, bricksetUserSetWanted);
            await _bricksetUserRepository.AddOrUpdateSet(bricksetUser.BricksetUsername, bricksetUserSetOwned);

            var bricksetUserSetList = (await _bricksetUserRepository.GetWantedSets(bricksetUser.BricksetUsername)).ToList();

            Check.That(bricksetUserSetList).CountIs(1);
            Check.That(bricksetUserSetList.Select(bricksetUserSet => bricksetUserSet.Set.SetId)).Contains(bricksetUserSetWanted.Set.SetId);
        }

        [TestMethod]
        public async Task GetOwnedSets_BricksetUserDoesNotExist_ReturnsEmptyList()
        {
            var bricksetUserSetList = await _bricksetUserRepository.GetWantedSets(string.Empty);

            Check.That(bricksetUserSetList).IsEmpty();
        }

        [TestMethod]
        public async Task GetOwnedSets_BricksetUserExists_ReturnsListOfOwnedSets()
        {
            var wantedSet = ModelsSetup.GetSetUnderTest();
            var ownedSet = ModelsSetup.GetSecondSetUnderTest();
            var bricksetUser = ModelsSetup.GetBricksetUserUnderTest();

            wantedSet = await InsertData(wantedSet);
            ownedSet = await InsertData(ownedSet);
            await InsertData(bricksetUser);

            var bricksetUserSetWanted = new BricksetUserSet
            {
                Set = wantedSet,
                Wanted = true
            };

            var bricksetUserSetOwned = new BricksetUserSet
            {
                Set = ownedSet,
                Owned = true
            };

            await _bricksetUserRepository.AddOrUpdateSet(bricksetUser.BricksetUsername, bricksetUserSetWanted);
            await _bricksetUserRepository.AddOrUpdateSet(bricksetUser.BricksetUsername, bricksetUserSetOwned);

            var bricksetUserSetList = (await _bricksetUserRepository.GetOwnedSets(bricksetUser.BricksetUsername)).ToList();

            Check.That(bricksetUserSetList).CountIs(1);
            Check.That(bricksetUserSetList.Select(bricksetUserSet => bricksetUserSet.Set.SetId)).Contains(bricksetUserSetOwned.Set.SetId);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(ModelsSetup.StringEmpty)]
        public async Task RemoveSets_InvalidUsername_Returns(string username)
        {
            var result = await _bricksetUserRepository.RemoveSets(username, [555]);

            Check.That(result).IsZero();
        }

        [TestMethod]
        public async Task RemoveSets_NullListOfSetIds_Returns()
        {
            var result = await _bricksetUserRepository.RemoveSets("username", null);

            Check.That(result).IsZero();
        }

        [TestMethod]
        public async Task RemoveSets_EmptyListOfSetIds_Returns()
        {
            var result = await _bricksetUserRepository.RemoveSets("username", []);

            Check.That(result).IsZero();
        }

        [TestMethod]
        public async Task RemoveSets_UserDoesNotExists_Returns()
        {
            var set = ModelsSetup.GetSetUnderTest();
            var bricksetUser = ModelsSetup.GetBricksetUserUnderTest();

            set = await InsertData(set);
            await InsertData(bricksetUser);

            var bricksetUserSetUnderTest = new BricksetUserSet
            {
                Set = set,
                Wanted = true
            };

            await _bricksetUserRepository.AddOrUpdateSet(bricksetUser.BricksetUsername, bricksetUserSetUnderTest);

            var result = await _bricksetUserRepository.RemoveSets("username", [bricksetUserSetUnderTest.Set.SetId]);

            Check.That(result).IsZero();
        }

        [TestMethod]
        public async Task RemoveSets_UserDoesNotHaveSetsToBeDeleted_Returns()
        {
            var set = ModelsSetup.GetSetUnderTest();
            var bricksetUser = ModelsSetup.GetBricksetUserUnderTest();

            set = await InsertData(set);
            await InsertData(bricksetUser);

            var bricksetUserSetUnderTest = new BricksetUserSet
            {
                Set = set,
                Wanted = true
            };

            await _bricksetUserRepository.AddOrUpdateSet(bricksetUser.BricksetUsername, bricksetUserSetUnderTest);

            var result = await _bricksetUserRepository.RemoveSets(bricksetUser.BricksetUsername, [bricksetUserSetUnderTest.Set.SetId + 1]);

            Check.That(result).IsZero();
        }

        [TestMethod]
        public async Task RemoveSets_UserHasSetsToBeDeleted_UserIsUpdated()
        {
            var set = ModelsSetup.GetSetUnderTest();
            var bricksetUser = ModelsSetup.GetBricksetUserUnderTest();

            set = await InsertData(set);
            await InsertData(bricksetUser);

            var bricksetUserSetUnderTest = new BricksetUserSet
            {
                Set = set,
                Wanted = true
            };

            await _bricksetUserRepository.AddOrUpdateSet(bricksetUser.BricksetUsername, bricksetUserSetUnderTest);

            var result = await _bricksetUserRepository.RemoveSets(bricksetUser.BricksetUsername, [bricksetUserSetUnderTest.Set.SetId]);

            Check.That(result).Is(1);
            Check.That((await _bricksetUserRepository.Get(bricksetUser.BricksetUsername)).Sets.Count).IsZero();
        }
    }
}
