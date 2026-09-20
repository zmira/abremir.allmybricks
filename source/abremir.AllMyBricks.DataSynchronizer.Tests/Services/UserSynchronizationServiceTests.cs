using System.Threading.Tasks;
using abremir.AllMyBricks.Data.Enumerations;
using abremir.AllMyBricks.Data.Interfaces;
using abremir.AllMyBricks.DataSynchronizer.Events.UserSynchronizationService;
using abremir.AllMyBricks.DataSynchronizer.Interfaces;
using abremir.AllMyBricks.DataSynchronizer.Services;
using abremir.AllMyBricks.Onboarding.Interfaces;
using abremir.AllMyBricks.Platform.Interfaces;
using Easy.MessageHub;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstituteAutoMocker.Standard;

namespace abremir.AllMyBricks.DataSynchronizer.Tests.Services
{
    [TestClass]
    public class UserSynchronizationServiceTests
    {
        private NSubstituteAutoMocker<UserSynchronizationService> _userSynchronizationService;

        [TestInitialize]
        public void TestInitialize()
        {
            _userSynchronizationService = new NSubstituteAutoMocker<UserSynchronizationService>();
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(null)]
        [DataRow(" ")]
        public async Task SynchronizeBricksetPrimaryUsersSets_NoBricksetApiKey_NothingIsDone(string apiKey)
        {
            _userSynchronizationService.Get<IOnboardingService>().GetBricksetApiKey().Returns(apiKey);

            await _userSynchronizationService.ClassUnderTest.SynchronizeBricksetPrimaryUsersSets();

            await _userSynchronizationService.Get<IBricksetUserRepository>().DidNotReceive().GetAllUsernames(Arg.Any<BricksetUserType>());
            await _userSynchronizationService.Get<ISecureStorageService>().DidNotReceive().GetBricksetUserHash(Arg.Any<string>());
            await _userSynchronizationService.Get<IUserSynchronizer>().DidNotReceive().SynchronizeBricksetPrimaryUser(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [TestMethod]
        public async Task SynchronizeBricksetPrimaryUsersSets_NoUserProvidedAndDoesNotHavePrimaryUsers_DoesNotInvokeUserSynchronizer()
        {
            const string apiKey = "APIKEY";

            _userSynchronizationService.Get<IOnboardingService>().GetBricksetApiKey().Returns(apiKey);

            await _userSynchronizationService.ClassUnderTest.SynchronizeBricksetPrimaryUsersSets();

            await _userSynchronizationService.Get<IBricksetUserRepository>().Received().GetAllUsernames(BricksetUserType.Primary);
            await _userSynchronizationService.Get<ISecureStorageService>().DidNotReceive().GetBricksetUserHash(Arg.Any<string>());
            await _userSynchronizationService.Get<IUserSynchronizer>().DidNotReceive().SynchronizeBricksetPrimaryUser(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [TestMethod]
        public async Task SynchronizeBricksetPrimaryUsersSets_NoUserProvidedAndPrimaryUsersDoNotHaveUserHash_PublishesExceptionMessageForEachUser()
        {
            const string apiKey = "APIKEY";

            var returnedBricksetPrimaryUsers = new[]
            {
                "brickset-primary-user-1",
                "brickset-primary-user-2"
            };

            _userSynchronizationService.Get<IOnboardingService>().GetBricksetApiKey().Returns(apiKey);
            _userSynchronizationService.Get<IBricksetUserRepository>().GetAllUsernames(BricksetUserType.Primary).Returns(returnedBricksetPrimaryUsers);
            _userSynchronizationService.Get<ISecureStorageService>().GetBricksetUserHash(Arg.Any<string>()).Returns(string.Empty);

            await _userSynchronizationService.ClassUnderTest.SynchronizeBricksetPrimaryUsersSets();

            await _userSynchronizationService.Get<IBricksetUserRepository>().Received().GetAllUsernames(BricksetUserType.Primary);
            await _userSynchronizationService.Get<ISecureStorageService>().Received(returnedBricksetPrimaryUsers.Length).GetBricksetUserHash(Arg.Any<string>());
            await _userSynchronizationService.Get<IUserSynchronizer>().DidNotReceive().SynchronizeBricksetPrimaryUser(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
            _userSynchronizationService.Get<IMessageHub>().Received().Publish(Arg.Any<UserSynchronizationServiceException>());
        }

        [TestMethod]
        public async Task SynchronizeBricksetPrimaryUsersSets_NoUserProvidedAndPrimaryUsersHaveUserHash_InvokesUserSynchronizerForEachUser()
        {
            const string apiKey = "APIKEY";
            const string userHash = "USERHASH";

            var returnedBricksetPrimaryUsers = new[]
            {
                "brickset-primary-user-1",
                "brickset-primary-user-2"
            };

            _userSynchronizationService.Get<IOnboardingService>().GetBricksetApiKey().Returns(apiKey);
            _userSynchronizationService.Get<IBricksetUserRepository>().GetAllUsernames(BricksetUserType.Primary).Returns(returnedBricksetPrimaryUsers);
            _userSynchronizationService.Get<ISecureStorageService>().GetBricksetUserHash(Arg.Any<string>()).Returns(userHash);

            await _userSynchronizationService.ClassUnderTest.SynchronizeBricksetPrimaryUsersSets();

            await _userSynchronizationService.Get<IBricksetUserRepository>().Received().GetAllUsernames(BricksetUserType.Primary);
            await _userSynchronizationService.Get<ISecureStorageService>().Received(returnedBricksetPrimaryUsers.Length).GetBricksetUserHash(Arg.Any<string>());
            await _userSynchronizationService.Get<IUserSynchronizer>().Received(returnedBricksetPrimaryUsers.Length).SynchronizeBricksetPrimaryUser(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
            _userSynchronizationService.Get<IMessageHub>().DidNotReceive().Publish(Arg.Any<UserSynchronizationServiceException>());
        }

        [TestMethod]
        public async Task SynchronizeBricksetPrimaryUsersSets_UserProvidedAndDoesNotExist_PublishesExceptionMessage()
        {
            const string username = "USERNAME";
            const string apiKey = "APIKEY";

            _userSynchronizationService.Get<IOnboardingService>().GetBricksetApiKey().Returns(apiKey);
            _userSynchronizationService.Get<IBricksetUserRepository>().Exists(Arg.Any<string>()).Returns(false);

            await _userSynchronizationService.ClassUnderTest.SynchronizeBricksetPrimaryUsersSets(username);

            _userSynchronizationService.Get<IMessageHub>().Received().Publish(Arg.Any<UserSynchronizationServiceException>());
        }

        [TestMethod]
        public async Task SynchronizeBricksetPrimaryUsersSets_UserProvidedAndDoesNotHaveUserHash_PublishesExceptionMessage()
        {
            const string username = "USERNAME";
            const string apiKey = "APIKEY";

            _userSynchronizationService.Get<IOnboardingService>().GetBricksetApiKey().Returns(apiKey);
            _userSynchronizationService.Get<IBricksetUserRepository>().Exists(Arg.Any<string>()).Returns(true);
            _userSynchronizationService.Get<ISecureStorageService>().GetBricksetUserHash(Arg.Any<string>()).Returns(string.Empty);

            await _userSynchronizationService.ClassUnderTest.SynchronizeBricksetPrimaryUsersSets(username);

            await _userSynchronizationService.Get<IBricksetUserRepository>().DidNotReceive().GetAllUsernames(Arg.Any<BricksetUserType>());
            await _userSynchronizationService.Get<ISecureStorageService>().Received().GetBricksetUserHash(Arg.Any<string>());
            await _userSynchronizationService.Get<IUserSynchronizer>().DidNotReceive().SynchronizeBricksetPrimaryUser(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
            _userSynchronizationService.Get<IMessageHub>().Received().Publish(Arg.Any<UserSynchronizationServiceException>());
        }

        [TestMethod]
        public async Task SynchronizeBricksetPrimaryUsersSets_UserProvidedAndHasUserHash_InvokesUserSynchronizer()
        {
            const string username = "USERNAME";
            const string apiKey = "APIKEY";
            const string userHash = "USERHASH";

            _userSynchronizationService.Get<IOnboardingService>().GetBricksetApiKey().Returns(apiKey);
            _userSynchronizationService.Get<IBricksetUserRepository>().Exists(Arg.Any<string>()).Returns(true);
            _userSynchronizationService.Get<ISecureStorageService>().GetBricksetUserHash(Arg.Any<string>()).Returns(userHash);

            await _userSynchronizationService.ClassUnderTest.SynchronizeBricksetPrimaryUsersSets(username);

            await _userSynchronizationService.Get<IBricksetUserRepository>().DidNotReceive().GetAllUsernames(Arg.Any<BricksetUserType>());
            await _userSynchronizationService.Get<ISecureStorageService>().Received().GetBricksetUserHash(Arg.Any<string>());
            await _userSynchronizationService.Get<IUserSynchronizer>().Received().SynchronizeBricksetPrimaryUser(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
            _userSynchronizationService.Get<IMessageHub>().DidNotReceive().Publish(Arg.Any<UserSynchronizationServiceException>());
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(null)]
        [DataRow(" ")]
        public async Task SynchronizeBricksetFriendsSets_NoBricksetApiKey_NothingIsDone(string apiKey)
        {
            _userSynchronizationService.Get<IOnboardingService>().GetBricksetApiKey().Returns(apiKey);

            await _userSynchronizationService.ClassUnderTest.SynchronizeBricksetFriendsSets();

            await _userSynchronizationService.Get<IBricksetUserRepository>().DidNotReceive().GetAllUsernames(Arg.Any<BricksetUserType>());
            await _userSynchronizationService.Get<IUserSynchronizer>().DidNotReceive().SynchronizeBricksetFriend(Arg.Any<string>(), Arg.Any<string>());
        }

        [TestMethod]
        public async Task SynchronizeBricksetFriendsSets_NoUserProvidedAndDoesNotHaveFriends_DoesNotInvokeUserSynchronizer()
        {
            const string apiKey = "APIKEY";

            _userSynchronizationService.Get<IOnboardingService>().GetBricksetApiKey().Returns(apiKey);

            await _userSynchronizationService.ClassUnderTest.SynchronizeBricksetFriendsSets();

            await _userSynchronizationService.Get<IBricksetUserRepository>().Received().GetAllUsernames(BricksetUserType.Friend);
            await _userSynchronizationService.Get<IUserSynchronizer>().DidNotReceive().SynchronizeBricksetFriend(Arg.Any<string>(), Arg.Any<string>());
        }

        [TestMethod]
        public async Task SynchronizeBricksetFriendsSets_NoUserProvidedAndHasFriends_InvokesUserSynchronizerForEachUser()
        {
            const string apiKey = "APIKEY";

            var returnedBricksetFriends = new[]
            {
                "brickset-friend-1",
                "brickset-friend-2"
            };

            _userSynchronizationService.Get<IOnboardingService>().GetBricksetApiKey().Returns(apiKey);
            _userSynchronizationService.Get<IBricksetUserRepository>().GetAllUsernames(BricksetUserType.Friend).Returns(returnedBricksetFriends);

            await _userSynchronizationService.ClassUnderTest.SynchronizeBricksetFriendsSets();

            await _userSynchronizationService.Get<IBricksetUserRepository>().Received().GetAllUsernames(BricksetUserType.Friend);
            await _userSynchronizationService.Get<IUserSynchronizer>().Received(returnedBricksetFriends.Length).SynchronizeBricksetFriend(apiKey, Arg.Any<string>());
        }

        [TestMethod]
        public async Task SynchronizeBricksetFriendsSets_UserProvided_InvokesUserSynchronizer()
        {
            const string username = "USERNAME";
            const string apiKey = "APIKEY";

            _userSynchronizationService.Get<IOnboardingService>().GetBricksetApiKey().Returns(apiKey);
            _userSynchronizationService.Get<IBricksetUserRepository>().Exists(Arg.Any<string>()).Returns(true);

            await _userSynchronizationService.ClassUnderTest.SynchronizeBricksetFriendsSets(username);

            await _userSynchronizationService.Get<IUserSynchronizer>().Received().SynchronizeBricksetFriend(apiKey, username);
        }

        [TestMethod]
        public async Task SynchronizeBricksetFriendsSets_UserProvidedAndDoesNotExist_PublishesExceptionMessage()
        {
            const string username = "USERNAME";
            const string apiKey = "APIKEY";

            _userSynchronizationService.Get<IOnboardingService>().GetBricksetApiKey().Returns(apiKey);
            _userSynchronizationService.Get<IBricksetUserRepository>().Exists(Arg.Any<string>()).Returns(false);

            await _userSynchronizationService.ClassUnderTest.SynchronizeBricksetFriendsSets(username);

            _userSynchronizationService.Get<IMessageHub>().Received().Publish(Arg.Any<UserSynchronizationServiceException>());
        }
    }
}
