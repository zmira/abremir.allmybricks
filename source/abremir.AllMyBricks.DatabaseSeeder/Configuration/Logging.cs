using System;
using System.IO;
using System.Reflection;
using abremir.AllMyBricks.DatabaseSeeder.Enumerations;
using abremir.AllMyBricks.DatabaseSeeder.Loggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;

namespace abremir.AllMyBricks.DatabaseSeeder.Configuration
{
    public static class Logging
    {
        public static ILoggerFactory Factory { get; private set; } = new LoggerFactory();

        public static ILogger CreateLogger<T>() => Factory.CreateLogger<T>();

        public static LogVerbosity LogVerbosity { get; set; }
        public static LogDestination LogDestination { get; set; }

        public static IServiceCollection AddLoggingServices(this IServiceCollection services)
        {
            return services
                .AddTransient<ILoggerFactory>((_) => Logging.Factory)
                .AddScoped<IDatabaseSeederLogger, SetSanitizerLogger>()
                .AddScoped<IDatabaseSeederLogger, SetSanitizeServiceLogger>()
                .AddScoped<IDatabaseSeederLogger, SetSynchronizationServiceLogger>()
                .AddScoped<IDatabaseSeederLogger, SetSynchronizerLogger>()
                .AddScoped<IDatabaseSeederLogger, SubthemeSynchronizerLogger>()
                .AddScoped<IDatabaseSeederLogger, ThemeSanitizerLogger>()
                .AddScoped<IDatabaseSeederLogger, ThemeSynchronizerLogger>()
                .AddScoped<IDatabaseSeederLogger, ThumbnailSynchronizerLogger>()
                .AddScoped<IDatabaseSeederLogger, UserSynchronizationServiceLogger>()
                .AddScoped<IDatabaseSeederLogger, UserSynchronizerLogger>();
        }

        public static void Configure(IHost host, LogDestination logDestination, LogVerbosity logVerbosity)
        {
            LogVerbosity = logVerbosity;

            if (LogVerbosity is LogVerbosity.None)
            {
                return;
            }

            LogDestination = logDestination;

            SetupConsoleLogging();
            SetupFileLogging();

            // force all loggers to be instantiated
            _ = host.Services.GetServices<IDatabaseSeederLogger>();
        }

        private static void SetupFileLogging()
        {
            if (LogDestination is not LogDestination.File
                && LogDestination is not LogDestination.All)
            {
                return;
            }

            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var logFile = Path.Combine(folderPath, $"{DateTime.Now:yyyyMMdd}_{Assembly.GetExecutingAssembly().GetName().Name}.log");

            Factory.AddProvider(new FileLoggerProvider(logFile, new FileLoggerOptions
            {
                MaxRollingFiles = 5,
                FileSizeLimitBytes = 5 * 1024 * 1024
            }));
        }

        private static void SetupConsoleLogging()
        {
            if (LogDestination is not LogDestination.Console
                && LogDestination is not LogDestination.All)
            {
                return;
            }

            Factory = LoggerFactory.Create(builder => builder.AddConsole());
        }
    }
}
