using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using abremir.AllMyBricks.Onboarding.Shared.Models;
using abremir.AllMyBricks.Platform.Configuration;
using abremir.AllMyBricks.Platform.Interfaces;
using Microsoft.Maui.Storage;

namespace abremir.AllMyBricks.Platform.Services
{
    public class SecureStorageService(ISecureStorage secureStorage) : ISecureStorageService
    {
        private readonly ISecureStorage _secureStorage = secureStorage;

        public async Task<string> GetBricksetApiKey()
        {
            return await _secureStorage.GetAsync(Constants.BricksetApiKeySecureStorageKey).ConfigureAwait(false);
        }

        public async Task<bool> IsBricksetApiKeyAcquired()
        {
            return !string.IsNullOrWhiteSpace(await GetBricksetApiKey().ConfigureAwait(false));
        }

        public async Task SaveBricksetApiKey(string bricksetApiKey)
        {
            if (!await IsBricksetApiKeyAcquired().ConfigureAwait(false))
            {
                await _secureStorage.SetAsync(Constants.BricksetApiKeySecureStorageKey, bricksetApiKey).ConfigureAwait(false);
            }
        }

        public async Task<Identification> GetDeviceIdentification()
        {
            return JsonSerializer.Deserialize<Identification>(
                await GetRawDeviceIdentification().ConfigureAwait(false),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value);
        }

        public async Task<bool> IsDeviceIdentificationCreated()
        {
            return !string.IsNullOrWhiteSpace(await GetRawDeviceIdentification().ConfigureAwait(false));
        }

        public async Task SaveDeviceIdentification(Identification deviceIdentification)
        {
            if (!await IsDeviceIdentificationCreated().ConfigureAwait(false))
            {
                await _secureStorage.SetAsync(Constants.DeviceIdentificationSecureStorageKey, JsonSerializer.Serialize(deviceIdentification)).ConfigureAwait(false);
            }
        }

        public async Task<string> GetBricksetUserHash(string username)
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(
                await GetRawBricksetPrimaryUsers().ConfigureAwait(false),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value)?[username];
        }

        public async Task SaveBricksetPrimaryUser(string username, string userHash)
        {
            var bricksetUsers = JsonSerializer.Deserialize<Dictionary<string, string>>(
                await GetRawBricksetPrimaryUsers().ConfigureAwait(false),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value) ?? [];

            if (bricksetUsers.TryAdd(username, userHash))
            {
                await _secureStorage.SetAsync(Constants.BricksetPrimaryUsersStorageKey, JsonSerializer.Serialize(bricksetUsers)).ConfigureAwait(false);
            }
        }

        public async Task<bool> ClearBricksetPrimaryUser(string username)
        {
            var bricksetUsers = JsonSerializer.Deserialize<Dictionary<string, string>>(
                await GetRawBricksetPrimaryUsers().ConfigureAwait(false),
                Onboarding.Shared.Configuration.Constants.JsonSerializerOptions.Value) ?? [];

            if (!bricksetUsers.ContainsKey(username))
            {
                return false;
            }

            bricksetUsers.Remove(username);

            await _secureStorage.SetAsync(Constants.BricksetPrimaryUsersStorageKey, JsonSerializer.Serialize(bricksetUsers)).ConfigureAwait(false);

            return true;
        }

        public async Task<bool> IsBricksetPrimaryUsersDefined()
        {
            return !string.IsNullOrWhiteSpace(await GetRawBricksetPrimaryUsers().ConfigureAwait(false));
        }

        public async Task<string> GetDefaultUsername()
        {
            return await _secureStorage.GetAsync(Constants.DefaultUsernameStorageKey).ConfigureAwait(false);
        }

        public async Task<bool> IsDefaultUsernameDefined()
        {
            return !string.IsNullOrWhiteSpace(await GetDefaultUsername().ConfigureAwait(false));
        }

        public async Task SaveDefaultUsername(string username)
        {
            if (!await IsDefaultUsernameDefined().ConfigureAwait(false))
            {
                await _secureStorage.SetAsync(Constants.DefaultUsernameStorageKey, username).ConfigureAwait(false);
            }
        }

        private async Task<string> GetRawDeviceIdentification()
        {
            return await _secureStorage.GetAsync(Constants.DeviceIdentificationSecureStorageKey).ConfigureAwait(false);
        }

        private async Task<string> GetRawBricksetPrimaryUsers()
        {
            return await _secureStorage.GetAsync(Constants.BricksetPrimaryUsersStorageKey).ConfigureAwait(false);
        }
    }
}
