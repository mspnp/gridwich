using System;
using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Gridwich.SagaParticipants.Storage.AzureStorage.Interfaces;
using Gridwich.SagaParticipants.Storage.AzureStorage.Services;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Gridwich.SagaParticipants.Storage.AzureStorage
{
    /// <summary>
    /// This defines an extension method for the Encode Service to add any DI related dependencies without
    /// having to touch startup.cs.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class AzureStorageManagementExtensions
    {
        /// <summary>
        /// Adds Azure Management.
        /// </summary>
        /// <param name="services">IServiceCollection object.</param>
        public static void AddAzureStorageManagement(this IServiceCollection services)
        {
            services.AddSingleton(typeof(Microsoft.Azure.Management.Fluent.IAzure), sp =>
            {
                // If we find tenant and subscription in environment variables, configure accordingly
                if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(@"AZURE_TENANT_ID"))
                    && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(@"AZURE_SUBSCRIPTION_ID")))
                {
                    var tokenCred = sp.GetService<TokenCredential>();
                    var armToken = tokenCred.GetToken(new TokenRequestContext(scopes: new[] { "https://management.azure.com/.default" }, parentRequestId: null), default).Token;
                    var armCreds = new Microsoft.Rest.TokenCredentials(armToken);

                    var graphToken = tokenCred.GetToken(new TokenRequestContext(scopes: new[] { "https://graph.windows.net/.default" }, parentRequestId: null), default).Token;
                    var graphCreds = new Microsoft.Rest.TokenCredentials(graphToken);

                    var credentials = new AzureCredentials(armCreds, graphCreds, Environment.GetEnvironmentVariable(@"AZURE_TENANT_ID"), AzureEnvironment.AzureGlobalCloud);

                    return Microsoft.Azure.Management.Fluent.Azure
                        .Authenticate(credentials)
                        .WithSubscription(Environment.GetEnvironmentVariable(@"AZURE_SUBSCRIPTION_ID"));
                }
                else
                {
                    var credentials = SdkContext.AzureCredentialsFactory
                        .FromSystemAssignedManagedServiceIdentity(MSIResourceType.AppService, AzureEnvironment.AzureGlobalCloud);
                    return Microsoft.Azure.Management.Fluent.Azure
                    .Authenticate(credentials)
                    .WithDefaultSubscription();
                }
            });

            services.AddSingleton<IAzureStorageManagement, AzureStorageManagement>();
        }
    }
}
