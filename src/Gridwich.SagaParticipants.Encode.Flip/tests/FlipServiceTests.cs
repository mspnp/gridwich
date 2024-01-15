using Gridwich.Core.DTO;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Encode.Exceptions;
using Gridwich.SagaParticipants.Encode.Flip.Services;
using Gridwich.SagaParticipants.Encode.TelestreamCloud;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Telestream.Cloud.Flip.Api;
using Telestream.Cloud.Flip.Model;
using Telestream.Cloud.Stores.Model;
using Xunit;

namespace Gridwich.SagaParticipants.Encode.FlipTests
{
    public class FlipServiceTests
    {
        private static readonly string OpContext = "{'key1':'value1', 'key2' : 2}".Replace("'", "\"");

        private static readonly RequestFlipEncodeCreateDTO GoodData2 = new RequestFlipEncodeCreateDTO()
        {
            Inputs = new InputItem[] { new InputItem() { BlobUri = "https://flipmedia.blob.core.windows.net/flipsource/bbb.mp4" } },
            FactoryName = "GoodFactory",
            Profiles = "h264",

            OutputContainer = "https://yaaya",
            OperationContext = JObject.Parse(OpContext)
        };

        private static readonly RequestFlipEncodeCreateDTO BadData2 = new RequestFlipEncodeCreateDTO()
        {
            Inputs = new InputItem[] { new InputItem() { BlobUri = "https://flipmedia.blob.core.windows.net/flipsource/bbb.mp4" } },
            FactoryName = "BadFactory",
            Profiles = "h264",
            OutputContainer = "https://yaaya",
            OperationContext = JObject.Parse(OpContext)
        };

        // Define an array of test data to send to unit tests, with expected result matching that data.
        private static readonly Uri _validSasUri = new Uri("https://flipmedia.blob.core.windows.net/flipsource/bbb.mp4?sv=wholebunchajunkhere");

        private static readonly ITelestreamCloudClientProvider _telestreamCloudClientProvider = Mock.Of<ITelestreamCloudClientProvider>(x => x.FlipApi == Mock.Of<IFlipApi>());
        private static readonly ITelestreamCloudStorageProvider _telestreamCloudStorageProvider = Mock.Of<ITelestreamCloudStorageProvider>();
        private static readonly IStorageService _storageService = Mock.Of<IStorageService>();


        public static IEnumerable<object[]> EncoderData
        {
            get
            {
                JsonHelpers.SetupJsonSerialization();
                return new[]
                {
                    // All good data, this one should not throw, but return a valid object.
                    new object[] { JsonHelpers.DeserializeFromString<RequestFlipEncodeCreateDTO>(JsonConvert.SerializeObject(GoodData2)), true, null, _validSasUri },
                    // Bad data input, not a valid Factory.
                    new object[] { JsonHelpers.DeserializeFromString<RequestFlipEncodeCreateDTO>(JsonConvert.SerializeObject(BadData2)), true, typeof(GridwichFlipFactoryDoesNotExistException), _validSasUri },
                    // Bad (null) argument passed to method.
                    new object[] { null, true, typeof(ArgumentNullException), null },
                    // Could not successfully generate SasUrl.
                    new object[] { JsonHelpers.DeserializeFromString<RequestFlipEncodeCreateDTO>(JsonConvert.SerializeObject(BadData2)), true, typeof(GridwichFlipSASException), null },
                    // Bad data, should throw.  Blob does not exist
                    new object[] { JsonHelpers.DeserializeFromString<RequestFlipEncodeCreateDTO>(JsonConvert.SerializeObject(BadData2)), false, typeof(GridwichFlipMissingInputException), null },
                };
            }
        }

        [Theory]
        [MemberData(nameof(EncoderData))]
        public async void FlipTestWithGoodAndBadData(RequestFlipEncodeCreateDTO encodeCreateData, bool blobShouldExist, Type shouldThrowThis, Uri sasUri)
        {
            // Arrange
            var video = new Video();

            var factoryList = new PaginatedFactoryCollection
            {
                Factories = new List<Factory>
                {
                    new Factory() { Name = "GoodFactory" }
                }
            };

            // Arrange Mocks

            Mock.Get(_telestreamCloudClientProvider.FlipApi)
                .Setup(x => x.CreateVideoAsync(It.IsAny<string>(), It.IsAny<CreateVideoBody>()))
                .ReturnsAsync(video);

            Mock.Get(_telestreamCloudClientProvider.FlipApi)
                .Setup(x => x.ListFactoriesAsync(It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(factoryList);

            Mock.Get(_storageService)
                .Setup(x => x.GetBlobExistsAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(blobShouldExist);

            Mock.Get(_storageService)
                .Setup(x => x.GetSasUrlForBlob(It.IsAny<Uri>(), It.IsAny<TimeSpan>(), It.IsAny<StorageClientProviderContext>()))
                .Returns(sasUri?.ToString());

            Mock.Get(_telestreamCloudStorageProvider)
                .Setup(x => x.GetStoreByNameAsync(It.IsAny<Uri>()))
                .ReturnsAsync(new Store());

            // Act
            var flipService = new FlipService(_storageService, _telestreamCloudClientProvider, _telestreamCloudStorageProvider);
            var ex = await Record.ExceptionAsync(async () => await flipService.EncodeCreateAsync(encodeCreateData).ConfigureAwait(false)).ConfigureAwait(false);

            // Assert
            if (shouldThrowThis is null)
            {
                // if there are no throws, test is successful.
                Assert.Null(ex);
            }
            else
            {
                Assert.NotNull(ex);
                Assert.IsType(shouldThrowThis, ex);
            }
        }
    }
}
