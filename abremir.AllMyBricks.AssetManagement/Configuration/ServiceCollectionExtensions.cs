﻿using abremir.AllMyBricks.AssetManagement.Implementations;
using abremir.AllMyBricks.AssetManagement.Interfaces;
using abremir.AllMyBricks.AssetManagement.Services;
using abremir.AllMyBricks.Platform.Configuration;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace abremir.AllMyBricks.AssetManagement.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAssetManagementServices(this IServiceCollection services)
        {
            Guard.IsNotNull(services);

            return services
                .AddTransient<IAssetCompression, AssetCompression>()
                .AddTransient<IAssetUncompression, AssetUncompression>()
                .AddTransient<IAssetManagementService, AssetManagementService>()
                .AddTransient<IFileStream, FileStreamImplementation>()
                .AddTransient<ITarWriter, TarWriterImplementation>()
                .AddTransient<IReaderFactory, ReaderFactoryImplementation>()
                .AddPlatformIoServices();
        }
    }
}
