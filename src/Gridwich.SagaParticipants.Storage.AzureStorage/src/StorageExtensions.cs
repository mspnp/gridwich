using Azure.Core;
using Gridwich.Core.Interfaces;
using Gridwich.SagaParticipants.Storage.AzureStorage.EventGridHandlers;
using Gridwich.SagaParticipants.Storage.AzureStorage.Interfaces;
using Gridwich.SagaParticipants.Storage.AzureStorage.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Gridwich.SagaParticipants.Storage.AzureStorage
{
    /// <summary>
    /// Extension methods to inject dependencies into IServiceCollection DI container.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class StorageExtensions
    {
        /// <summary>
        /// Adds all dependencies of the EventGridServices
        /// </summary>
        /// <param name="services">IServiceCollection services.</param>
        public static void AddStorageEventGridServices(this IServiceCollection services)
        {
            services.AddTransient<IEventGridHandler, ChangeBlobTierHandler>();
            services.AddTransient<IEventGridHandler, BlobCreatedHandler>();
            services.AddTransient<IEventGridHandler, BlobDeletedHandler>();
            services.AddTransient<IEventGridHandler, CreateMetadataHandler>();
            // Removed until needed, for security reasons:
            // services.AddTransient<IEventGridHandler, BlobSasUrlCreateHandler>();
            services.AddTransient<IEventGridHandler, BlobCopyHandler>();
            services.AddTransient<IEventGridHandler, ContainerCreateHandler>();
            services.AddTransient<IEventGridHandler, ContainerDeleteHandler>();
            services.AddTransient<IEventGridHandler, ContainerAccessChangeHandler>();
            services.AddTransient<IEventGridHandler, BlobDeleteHandler>();
        }

        /// <summary>
        /// Adds all dependencies of the StorageService.
        /// </summary>
        /// <param name="services">IServiceCollection services.</param>
        public static void AddStorageService(this IServiceCollection services)
        {
            // https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet
            // Using DefaultAzureCredential allows for local dev by setting environment variables for the current user, provided said user
            // has the necessary credentials to perform the operations the MSI of the Function app needs in order to do its work. Including
            // interactive credentials will allow browser-based login when developing locally.
            services.AddSingleton<TokenCredential>(sp => new Azure.Identity.DefaultAzureCredential(includeInteractiveCredentials: true));

            services.AddSingleton<IBlobBaseClientProvider, BlobBaseClientProvider>();
            services.AddSingleton<IBlobContainerClientProvider, BlobContainerClientProvider>();
            services.AddTransient<IStorageService, StorageService>();
        }
    }
}
