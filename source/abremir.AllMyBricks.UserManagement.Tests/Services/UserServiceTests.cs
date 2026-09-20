using System;
using System.Threading.Tasks;
using abremir.AllMyBricks.Data.Enumerations;
using abremir.AllMyBricks.Data.Interfaces;
using abremir.AllMyBricks.DataSynchronizer.Interfaces;
using abremir.AllMyBricks.Platform.Interfaces;
using abremir.AllMyBricks.ThirdParty.Brickset.Interfaces;
using abremir.AllMyBricks.ThirdParty.Brickset.Models.Parameters;
using abremir.AllMyBricks.UserManagement.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NFluent;
using NSubstitute;
using NSubstituteAutoMocker.Standard;

namespace abremir.AllMyBricks.UserManagement.Tests.Services
{
    [TestClass]
    public class UserServiceTests
    {
        private NSubstituteAutoMocker<UserService> _userService;

        [TestInitialize]
        public void TestInitialize()
        {
            _userService = new NSubstituteAutoMocker<UserService>();
        }

        [TestMethod]
        public async Task AddDefaultUser_DefaultUserAlreadyDefined_DoesNotSaveDefaultUsernameAndDoesNotAddDefaultUserAndReturnsFalse()
        {
            _userService.Get<ISecureStorageService>().IsDefaultUsernameDefined().Returns(true);

            var addedDefaultUser = await _userService.ClassUnderTest.AddDefaultUser();

            await _userService.Get<ISecureStorageService>().DidNotReceive().SaveDefaultUsername(Arg.Any<string>());
            await _userService.Get<IBricksetUserRepository>().DidNotReceive().Add(BricksetUserType.None, Arg.Any<string>());
            Check.That(addedDefaultUser).IsFalse();
        }

        [TestMethod]
        public async Task AddDefaultUser_DefaultUserNotDefined_SavesDefaultUsernameAndAddsDefaultUserAndReturnsTrue()
        {
            _userService.Get<ISecureStorageService>().IsDefaultUsernameDefined().Returns(false);

            var addedDefaultUser = await _userService.ClassUnderTest.AddDefaultUser();

            await _userService.Get<ISecureStorageService>().Received().SaveDefaultUsername(Arg.Any<string>());
            await _userService.Get<IBricksetUserRepository>().Received().Add(BricksetUserType.None, Arg.Any<string>());
            Check.That(addedDefaultUser).IsTrue();
        }

        [TestMethod]
        [DataRow(null, "password", true, false, false)]
        [DataRow("", "password", true, false, false)]
        [DataRow(" ", "password", true, false, false)]
        [DataRow("username", null, true, false, false)]
        [DataRow("username", "", true, false, false)]
        [DataRow("username", " ", true, false, false)]
        [DataRow("username", "password", false, false, false)]
        [DataRow("username", "password", true, true, false)]
        [DataRow("username", "password", true, false, true)]
        public async Task AddBricksetPrimaryUser_InvalidUsernameOrChecks_DoesNotSavePrimaryUserHashAndDoesNotCreatePrimaryUserAndDoesNotSynchronizeSetsAndReturnsFalse(
            string username,
            string password,
            bool bricksetApiKeyAcquired,
            bool bricksetPrimaryUserDefined,
            bool bricksetUserExists)
        {
            _userService.Get<ISecureStorageService>().IsBricksetApiKeyAcquired().Returns(bricksetApiKeyAcquired);
            _userService.Get<ISecureStorageService>().IsBricksetPrimaryUsersDefined().Returns(bricksetPrimaryUserDefined);
            _userService.Get<IBricksetUserRepository>().Exists(Arg.Any<string>()).Returns(bricksetUserExists);

            var addedPrimaryUser = await _userService.ClassUnderTest.AddBricksetPrimaryUser(username, password);

            await _userService.Get<IBricksetApiService>().DidNotReceive().Login(Arg.Any<ParameterLogin>());
            await _userService.Get<ISecureStorageService>().DidNotReceive().SaveBricksetPrimaryUser(Arg.Any<string>(), Arg.Any<string>());
            await _userService.Get<IBricksetUserRepository>().DidNotReceive().Add(BricksetUserType.Primary, Arg.Any<string>());
            await _userService.Get<IUserSynchronizationService>().DidNotReceive().SynchronizeBricksetPrimaryUsersSets(Arg.Any<string>());
            Check.That(addedPrimaryUser).IsFalse();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public async Task AddBricksetPrimaryUser_ValidUsernameAndChecksAndInvalidUserHash_DoesNotSavePrimaryUserHashAndDoesNotCreatePrimaryUserAndDoesNotSynchronizeSetsAndReturnsFalse(string invalidUserHash)
        {
            _userService.Get<ISecureStorageService>().IsBricksetApiKeyAcquired().Returns(true);
            _userService.Get<ISecureStorageService>().IsBricksetPrimaryUsersDefined().Returns(false);
            _userService.Get<IBricksetUserRepository>().Exists(Arg.Any<string>()).Returns(false);
            _userService.Get<IBricksetApiService>().Login(Arg.Any<ParameterLogin>()).Returns(invalidUserHash);

            var addedPrimaryUser = await _userService.ClassUnderTest.AddBricksetPrimaryUser(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            await _userService.Get<IBricksetApiService>().Received().Login(Arg.Any<ParameterLogin>());
            await _userService.Get<ISecureStorageService>().DidNotReceive().SaveBricksetPrimaryUser(Arg.Any<string>(), Arg.Any<string>());
            await _userService.Get<IBricksetUserRepository>().DidNotReceive().Add(BricksetUserType.Primary, Arg.Any<string>());
            await _userService.Get<IUserSynchronizationService>().DidNotReceive().SynchronizeBricksetPrimaryUsersSets(Arg.Any<string>());
            Check.That(addedPrimaryUser).IsFalse();
        }

        [TestMethod]
        public async Task AddBricksetPrimaryUser_ValidUsernameAndChecksAndValidUserHash_SavesPrimaryUserHashAndCreatesPrimaryUserAndSynchronizesSetsAndReturnsTrue()
        {
            _userService.Get<ISecureStorageService>().IsBricksetApiKeyAcquired().Returns(true);
            _userService.Get<ISecureStorageService>().IsBricksetPrimaryUsersDefined().Returns(false);
            _userService.Get<IBricksetUserRepository>().Exists(Arg.Any<string>()).Returns(false);
            _userService.Get<IBricksetApiService>().Login(Arg.Any<ParameterLogin>()).Returns(Guid.NewGuid().ToString());

            var addedPrimaryUser = await _userService.ClassUnderTest.AddBricksetPrimaryUser(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            await _userService.Get<IBricksetApiService>().Received().Login(Arg.Any<ParameterLogin>());
            await _userService.Get<ISecureStorageService>().Received().SaveBricksetPrimaryUser(Arg.Any<string>(), Arg.Any<string>());
            await _userService.Get<IBricksetUserRepository>().Received().Add(BricksetUserType.Primary, Arg.Any<string>());
            await _userService.Get<IUserSynchronizationService>().Received().SynchronizeBricksetPrimaryUsersSets(Arg.Any<string>());
            Check.That(addedPrimaryUser).IsTrue();
        }

        [TestMethod]
        [DataRow(null, true, false)]
        [DataRow("", true, false)]
        [DataRow(" ", true, false)]
        [DataRow("username", false, false)]
        [DataRow("username", true, true)]
        public async Task AddBricksetFriend_InvalidUsernameOrChecks_DoesNotSaveFriendUserAndDoesNotSynchronizeSetsAndReturnsFalse(
            string username,
            bool bricksetApiKeyAcquired,
            bool bricksetUserExists)
        {
            _userService.Get<ISecureStorageService>().IsBricksetApiKeyAcquired().Returns(bricksetApiKeyAcquired);
            _userService.Get<IBricksetUserRepository>().Exists(Arg.Any<string>()).Returns(bricksetUserExists);

            var addedFriend = await _userService.ClassUnderTest.AddBricksetFriend(username);

            await _userService.Get<IBricksetUserRepository>().DidNotReceive().Add(BricksetUserType.Friend, Arg.Any<string>());
            await _userService.Get<IUserSynchronizationService>().DidNotReceive().SynchronizeBricksetFriendsSets(Arg.Any<string>());
            Check.That(addedFriend).IsFalse();
        }

        [TestMethod]
        public async Task AddBricksetFriend_ValidUsernameAndChecks_AddsFriendUserAndSynchronizesSetsAndReturnsTrue()
        {
            _userService.Get<ISecureStorageService>().IsBricksetApiKeyAcquired().Returns(true);
            _userService.Get<IBricksetUserRepository>().Exists(Arg.Any<string>()).Returns(false);

            var addedFriend = await _userService.ClassUnderTest.AddBricksetFriend(Guid.NewGuid().ToString());

            await _userService.Get<IBricksetUserRepository>().Received().Add(BricksetUserType.Friend, Arg.Any<string>());
            await _userService.Get<IUserSynchronizationService>().Received().SynchronizeBricksetFriendsSets(Arg.Any<string>());
            Check.That(addedFriend).IsTrue();
        }

        [TestMethod]
        [DataRow(null, true)]
        [DataRow("", true)]
        [DataRow(" ", true)]
        [DataRow("username", false)]
        public async Task RemoveBricksetPrimaryUser_InvalidUsernameOrChecks_DoesNotRemoveUserAndDoesNotClearSecureStorageAndReturnsFalse(string username, bool bricksetUserExists)
        {
            _userService.Get<IBricksetUserRepository>().Exists(Arg.Any<string>()).Returns(bricksetUserExists);

            var bricksetPrimaryUserRemoved = await _userService.ClassUnderTest.RemoveBricksetPrimaryUser(username);

            await _userService.Get<IBricksetUserRepository>().DidNotReceive().Remove(Arg.Any<string>());
            Check.That(bricksetPrimaryUserRemoved).IsFalse();
        }

        [TestMethod]
        public async Task RemoveBricksetPrimaryUser_ValidUsernameAndChecks_RemovesUserAndClearsSecureStorageAndReturnsTrue()
        {
            _userService.Get<IBricksetUserRepository>().Exists(Arg.Any<string>()).Returns(true);
            _userService.Get<ISecureStorageService>().ClearBricksetPrimaryUser(Arg.Any<string>()).Returns(true);

            var bricksetPrimaryUserRemoved = await _userService.ClassUnderTest.RemoveBricksetPrimaryUser(Guid.NewGuid().ToString());

            await _userService.Get<IBricksetUserRepository>().Received().Remove(Arg.Any<string>());
            Check.That(bricksetPrimaryUserRemoved).IsTrue();
        }

        [TestMethod]
        [DataRow(null, true)]
        [DataRow("", true)]
        [DataRow(" ", true)]
        [DataRow("username", false)]
        public async Task RemoveBricksetFriend_InvalidUsernameOrChecks_DoesNotRemoveUserAndReturnsFalse(string username, bool bricksetUserExists)
        {
            _userService.Get<IBricksetUserRepository>().Exists(Arg.Any<string>()).Returns(bricksetUserExists);

            var bricksetFriendRemoved = await _userService.ClassUnderTest.RemoveBricksetFriend(username);

            await _userService.Get<IBricksetUserRepository>().DidNotReceive().Remove(Arg.Any<string>());
            Check.That(bricksetFriendRemoved).IsFalse();
        }

        [TestMethod]
        public async Task RemoveBricksetFriend_ValidUsernameAndChecks_RemovesUserAndReturnsTrue()
        {
            _userService.Get<IBricksetUserRepository>().Exists(Arg.Any<string>()).Returns(true);

            var bricksetFriendRemoved = await _userService.ClassUnderTest.RemoveBricksetFriend(Guid.NewGuid().ToString());

            await _userService.Get<IBricksetUserRepository>().Received().Remove(Arg.Any<string>());
            Check.That(bricksetFriendRemoved).IsTrue();
        }
    }
}
