using Gridwich.Core.Helpers;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

[assembly: FunctionsStartup(typeof(Gridwich.Host.FunctionApp.Startup))]
namespace Gridwich.Host.FunctionApp
{
#pragma warning disable CA1812 // Internal class is never instantiated, make static

    /// <summary>
    /// This Azure Function project uses dependency injection.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Startup : FunctionsStartup
    {
        /// <summary>
        /// Use the Configure function to inject all required services into the ServiceCollection.
        /// Note: Service instances cannot be used in this function, as the hosting application is not fully loaded.
        /// </summary>
        /// <param name="builder">builder.</param>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder?.Services.AddGridwichServices();
            JsonHelpers.SetupJsonSerialization();
        }
    }
#pragma warning restore CA1812 // Internal class is never instantiated, make static
}