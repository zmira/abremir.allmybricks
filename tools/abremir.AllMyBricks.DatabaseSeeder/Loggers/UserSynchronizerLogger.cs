﻿using abremir.AllMyBricks.DatabaseSeeder.Configuration;
using abremir.AllMyBricks.DatabaseSeeder.Enumerations;
using abremir.AllMyBricks.DataSynchronizer.Events.UserSynchronizer;
using abremir.AllMyBricks.DataSynchronizer.Synchronizers;
using Easy.MessageHub;
using Microsoft.Extensions.Logging;

namespace abremir.AllMyBricks.DatabaseSeeder.Loggers
{
    public class UserSynchronizerLogger : IDatabaseSeederLogger
    {
        private static float _setIndex;
        private static float _setProgressFraction;
        private static float _setCount;

        public UserSynchronizerLogger(
            ILoggerFactory loggerFactory,
            IMessageHub messageHub)
        {
            var logger = loggerFactory.CreateLogger<UserSynchronizer>();

            messageHub.Subscribe<UserSynchronizerStart>(message =>
            {
                _setIndex = 0;
                _setProgressFraction = 0;

                if (Logging.LogVerbosity is LogVerbosity.Full)
                {
                    logger.LogInformation($"Started user synchronizer for {message.UserType} user '{message.Username}'");
                }
            });

            messageHub.Subscribe<AllMyBricksToBricksetStart>(_ =>
            {
                _setIndex = 0;
                _setProgressFraction = 0;

                if (Logging.LogVerbosity is LogVerbosity.Full)
                {
                    logger.LogInformation("Started update of Brickset.com with changes made in All My Bricks");
                }
            });

            messageHub.Subscribe<AllMyBricksToBricksetAcquiringSetsStart>(_ =>
            {
                _setIndex = 0;
                _setProgressFraction = 0;

                if (Logging.LogVerbosity is LogVerbosity.Full)
                {
                    logger.LogInformation("Acquiring All My Bricks user sets to update in Brickset.com");
                }
            });

            messageHub.Subscribe<AllMyBricksToBricksetAcquiringSetsEnd>(message =>
            {
                _setCount = message.Count;
                _setIndex = 0;

                logger.LogInformation($"Acquired {message.Count} All My Bricks user sets to update in Brickset.com");
            });

            messageHub.Subscribe<UserSynchronizerSynchronizingSetStart>(message =>
            {
                _setIndex++;
                _setProgressFraction = _setIndex / _setCount;

                if (Logging.LogVerbosity is LogVerbosity.Full)
                {
                    logger.LogInformation($"Started synchronizing set {message.SetId}: index {_setIndex}, progress {_setProgressFraction:##0.00%}");
                }
            });

            messageHub.Subscribe<UserSynchronizerSynchronizingSetEnd>(message =>
            {
                if (Logging.LogVerbosity is LogVerbosity.Full)
                {
                    logger.LogInformation($"Finished synchronizing set {message.SetId}");
                }
            });

            messageHub.Subscribe<AllMyBricksToBricksetEnd>(_ =>
            {
                _setIndex = 0;
                _setProgressFraction = 0;

                if (Logging.LogVerbosity is LogVerbosity.Full)
                {
                    logger.LogInformation("Finished update of Brickset.com with changes made in All My Bricks");
                }
            });

            messageHub.Subscribe<BricksetToAllMyBricksStart>(_ =>
            {
                _setIndex = 0;
                _setProgressFraction = 0;

                if (Logging.LogVerbosity is LogVerbosity.Full)
                {
                    logger.LogInformation("Started adding Brickset.com user sets missing in All My Bricks");
                }
            });

            messageHub.Subscribe<BricksetToAllMyBricksAcquiringSetsStart>(_ =>
            {
                _setIndex = 0;
                _setProgressFraction = 0;

                if (Logging.LogVerbosity is LogVerbosity.Full)
                {
                    logger.LogInformation("Acquiring Brickset.com user sets missing in All My Bricks");
                }
            });

            messageHub.Subscribe<BricksetToAllMyBricksAcquiringSetsEnd>(message =>
            {
                _setCount = message.Count;
                _setIndex = 0;

                logger.LogInformation($"Acquired {message.Count} user sets from Brickset.com missing in All My Bricks");
            });

            messageHub.Subscribe<BricksetToAllMyBricksEnd>(_ =>
            {
                _setIndex = 0;
                _setProgressFraction = 0;

                if (Logging.LogVerbosity is LogVerbosity.Full)
                {
                    logger.LogInformation("Finished adding Brickset.com user sets missing in All My Bricks");
                }
            });

            messageHub.Subscribe<UserSynchronizerException>(message => logger.LogError(message.Exception, $"Synchronizing {message.UserType} User '{message.Username}' Exception"));

            messageHub.Subscribe<UserSynchronizerEnd>(message =>
            {
                _setIndex = 0;
                _setProgressFraction = 0;

                if (Logging.LogVerbosity is LogVerbosity.Full)
                {
                    logger.LogInformation($"Finished user synchronizer for {message.UserType} user '{message.Username}'");
                }
            });
        }
    }
}
