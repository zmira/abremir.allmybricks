using System;
using abremir.AllMyBricks.AssetManagement.Implementations;
using abremir.AllMyBricks.AssetManagement.Interfaces;
using abremir.AllMyBricks.AssetManagement.Services;
using abremir.AllMyBricks.Platform.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace abremir.AllMyBricks.AssetManagement.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAssetManagementServices(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services, nameof(services));

            return services
                .AddTransient<IAssetCompression, AssetCompression>()
                .AddTransient<IAssetExpansion, AssetExpansion>()
                .AddTransient<IAssetManagementService, AssetManagementService>()
                .AddTransient<IFileStream, FileStreamImplementation>()
                .AddTransient<ICompressedTarHandler, CompressedTarHandler>()
                .AddPlatformIoServices();
        }
    }
}
