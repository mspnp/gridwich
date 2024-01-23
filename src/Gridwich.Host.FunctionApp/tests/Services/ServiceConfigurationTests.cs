using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Host.FunctionApp;
using Gridwich.Host.FunctionApp.Services;
using Gridwich.SagaParticipants.Storage.AzureStorage.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Xunit;

namespace Gridwich.Host.FunctionAppTests.Services
{
    [ExcludeFromCodeCoverage]
    public class ServiceConfigurationTests
    {
        // <summary>
        // This method sets up a Dependency-injection ServiceCollection that is
        // nearly identical to production Gridwich, but with necessary changes, such
        //  as removing the MSI authentication, needed to allow unit testing.
        // </summary>
        // <remarks>
        // Note: While it may seem like a lot of specialized setup, this stems from
        // the Dependency Injection needing to resolve dependencies and ensure that
        // they are also instantiated before the dependent.  For example, the
        // BlobCopy handler expects to be given a StorageService instance at a
        // constructor argument.  The code below is to work-around to the needed
        // dependent services to be instantiated, while inserting Mocks for singleton
        // services like Gridwich.Services.AddAzureStorageManagement which
        // will fall over in a test environment due to (in this case) a hard MSI dependency
        // and using a factory-based DI entry configuration when an instance-based
        // DI definition is needed.`
        //
        // Regardless, the end result below is a sufficient environment to let the
        // tests examine instances of configured EventGridHandlers as needed.
        public static ServiceCollection SetUpBaseServiceCollection()
        {
            // Start with a base-level ("empty") service collection.
            var sc = new ServiceCollection();

            // Values to push out to add to or override values from the local JSON file.
            var overrides = new Dictionary<string, string>()
            {
               { "MSI_ENDPOINT", "https://some.bizarre.site.com" },
               { "MSI_SECRET", "ABCDE" },
               { "TELESTREAMCLOUD_API_KEY", "XYZABC" }
            };

            const string localJSONFile = "../src/sample.local.settings.json";
            var localJSONFileAbsolutePath = TestHelpers.GetPathRelativeToTests(localJSONFile);

            IConfiguration cfg = Utils.InMemoryConfiguration.ConfigFromJSONSettingsFile(localJSONFileAbsolutePath, overrides);

            // Add in all of the usual Gridwich services (as used in production start-up)
            sc.AddGridwichServices();

            sc.AddSingleton<IHostingEnvironment, Utils.DummyHostEnvironment>();

            // Patch in the AzureStorageManagement instance and remove the MSI authentication component it uses.
            for (int i = 0; i < sc.Count; i++)
            {
                var item = sc[i];
                if (item.ServiceType == typeof(IConfiguration) || item.ServiceType == typeof(ISettingsProvider))
                {
                    sc.RemoveAt(i--);
                }
            }

            var sp = new SettingsProvider(cfg);
            sc.AddSingleton<IConfiguration>(cfg);
            sc.AddSingleton<ISettingsProvider>(sp);

            // Create a mock for AzureStorageManagement to always give a response for GetAccountKeyAsync.
            // Necessary because the constructors on the Telestream-related providers grab storage account
            // credentials, which requires authentication, which can't happen during unit test execution.
            var storageMgmtMock = new Mock<IAzureStorageManagement>();
            storageMgmtMock
                .Setup(x => x.GetAccountKey(It.IsAny<string>()))
                .Returns("An+Incorrect+But+Validly+Formatted+Base64+Storage+Key+Value///////////////////////////==");

            // Patch in the AzureStorageManagement instance and remove the MSI authentication component it uses.
            bool haveAppended = false;
            for (int i = 0; i < sc.Count; i++)
            {
                var item = sc[i];
                if (item.ServiceType == typeof(IAzureStorageManagement))
                {
                    // and we're not just running into the replacement entry we previously added
                    if (!object.ReferenceEquals(storageMgmtMock.Object, item.ImplementationInstance))
                    {
                        // replace it with the Mock
                        sc.RemoveAt(i--);
                        if (!haveAppended)
                        {
                            // Add ours to the end of the ServiceCollection list.  Since it's
                            // a Singleton, it only matters that we eliminated all other
                            // serviceDescriptors for IAzureStorageManagement from the list.
                            sc.AddSingleton<IAzureStorageManagement>(storageMgmtMock.Object);
                            haveAppended = true;
                        }
                    }
                }
                else if (item.ServiceType == typeof(Microsoft.Azure.Management.Fluent.IAzure))
                {
                    // remove the MSI authentication class accessed by the non-mocked AzureStorageManagment.
                    sc.RemoveAt(i--);
                }
            }

            return sc;
        }

        /// <summary>
        /// A local DI service collection, populated using the same method
        /// Gridwich uses to do so, then touched up to be able to work in a
        /// unit-test environent
        /// </summary>
        private ServiceCollection _sc;

        /// <summary>
        /// Will be called by the unit test runner once per test, so each test will
        /// be working on it's own copy.
        /// </summary>
        public ServiceConfigurationTests()
        {
            _sc = SetUpBaseServiceCollection();
        }

        // These tests are checking:
        //     1. All IEventGridHandlers are set as Transient (some where scoped).
        //     2. All providers should be Singletons
        //     3. Check for singletons for: IEventGridPublisher, IEventGridDispatcher
        //     4. Check that all EventGridHandler Ids are unique. Initial run found 2 dupes.
        // Stretch:
        //     5. No duplicates in the DI list.

        [Fact]
        /// <summary>
        /// Ensure that all EventGridHandlers are set as Scoped lifetime (i.e. new per request)
        /// </summary>
        public void CheckThatAllEventGridHandlersAreScoped()
        {
            foreach (var sd in _sc)
            {
                var expectedLifetime = ServiceLifetime.Transient;

                if (sd.ServiceType == typeof(IEventGridHandler))
                {
                    if (sd.Lifetime != expectedLifetime)
                    {
                        var msg = $"EventGridHandler '{sd.ImplementationType.FullName}' is registered via Add{sd.Lifetime}, should be registered using Add{expectedLifetime}";
                        sd.Lifetime.ShouldBe(expectedLifetime, msg);
                    }
                }
            }
        }

        [Fact]
        /// <summary>
        /// Ensure that no two EventGridHandlers have the same id
        /// </summary>
        public void CheckThatAllEventGridHandlersHaveUniqueIDs()
        {
            var dict = new Dictionary<string, IEventGridHandler>(30);

            // As the only way to get the Handler IDs is to create
            // an instance, let's let DI do the creation.

            var serviceProvider = _sc.BuildServiceProvider();

            var handlers = _sc.Where(sd => sd.ServiceType == typeof(IEventGridHandler));
            var handlerInstances = serviceProvider.GetServices<IEventGridHandler>();

            handlerInstances.ShouldNotBeNull("No EventGridHandlers registered");

            foreach (IEventGridHandler egh in handlerInstances)
            {
                var handlerId = egh.GetHandlerId().ToUpper(CultureInfo.InvariantCulture);

                if (dict.ContainsKey(handlerId))
                {
                    var existingHandler = dict[handlerId];
                    // Two handlers using the same ID

                    var existingTypeName = existingHandler.GetType().FullName;
                    var newTypeName = egh.GetType().FullName;

                    var msg = $"Both '{existingTypeName}' and '{newTypeName}' are using the same HandlerId '{handlerId}";

                    handlerId.ShouldNotBe(handlerId, msg); // and blow up.
                }
                else
                {
                    dict[handlerId] = egh;   // No conflicts, just memorize for later.
                }
            }
            dict.Clear();
        }

        [Fact] // Ensure StorageService is not a singleton.
        /// <summary>
        /// Ensure that Storage Service is never registered as a singleton.
        /// This is because the cache mechanism used to speed up byte range processing
        /// in GetOrDownloadContentAsync is not amenable to handling multi-threading.
        /// Once that caching is enhanced to work well with multi-threading, it would
        /// be safe to change Storage Servcie to a singleton, but do not do this casually.
        /// </summary>
        public void CheckThatStorageServiceIsNotASingleton()
        {
            // Build the DI registrations.
            _ = _sc.BuildServiceProvider();

            // there should be only a single registration and it should be Transient or Scoped.

            var services = _sc.Where(sd => sd.ServiceType == typeof(IStorageService)).ToArray();

            services.Length.ShouldBe(1, $"Should be a single DI registration of IStorageService but found {services.Length}");

            services[0].Lifetime.ShouldNotBe(ServiceLifetime.Singleton, "IStorageService should not be registered in DI as a singleton");
        }
    }
}