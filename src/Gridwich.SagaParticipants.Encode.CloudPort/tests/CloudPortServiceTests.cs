using Gridwich.Core.DTO;
using Gridwich.Core.Helpers;
using Gridwich.Core.Interfaces;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Encode.CloudPort.Services;
using Gridwich.SagaParticipants.Encode.Exceptions;
using Gridwich.SagaParticipants.Encode.TelestreamCloud;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Telestream.Cloud.Stores.Model;
using Telestream.Cloud.VantageCloudPort.Api;
using Telestream.Cloud.VantageCloudPort.Model;
using Xunit;

namespace Gridwich.SagaParticipants.Encode.CloudPortTests
{
    public class CloudPortServiceTests
    {
        private static readonly string OpContext = "{'key1':'value1', 'key2' : 2}".Replace("'", "\"");

        private static readonly RequestCloudPortEncodeCreateDTO GoodData2 = new RequestCloudPortEncodeCreateDTO()
        {
            Inputs = new InputItem[] { new InputItem() { BlobUri = "https://flipmedia.blob.core.windows.net/flipsource/bbb.mp4" } },
            WorkflowName = "TestWorkflow2",
            OutputContainer = "https://yaaya1",
            OperationContext = JObject.Parse(OpContext)
        };


        private static readonly RequestCloudPortEncodeCreateDTO BadData2 = new RequestCloudPortEncodeCreateDTO()
        {
            Inputs = new InputItem[] { new InputItem() { BlobUri = "https://flipmedia.blob.core.windows.net/flipsource/bbb.mp4" } },
            WorkflowName = "Workflow does not exist",
            OutputContainer = "https://yaaya2",
            OperationContext = JObject.Parse(OpContext)
        };

        private static readonly IStorageService _storageService = Mock.Of<IStorageService>();
        private static readonly ISettingsProvider _settingsProvider = Mock.Of<ISettingsProvider>();
        private static readonly ITelestreamCloudClientProvider _telestreamCloudClientProvider = Mock.Of<ITelestreamCloudClientProvider>(x => x.CloudPortApi == Mock.Of<IVantageCloudPortApi>());
        private static readonly ITelestreamCloudStorageProvider _telestreamCloudStorageProvider = Mock.Of<ITelestreamCloudStorageProvider>();
        private static readonly Uri _validSasUri = new Uri("https://flipmedia.blob.core.windows.net/flipsource/bbb.mp4?sv=wholebunchajunkhere");

        // Define an array of test data to send to unit tests, with expected result matching that data.
        public static IEnumerable<object[]> EncoderData
        {
            get
            {
                JsonHelpers.SetupJsonSerialization();
                return new[]
                {
                    // Good data.  This test should run with no exceptions.
                    new object[] { JsonHelpers.DeserializeFromString<RequestCloudPortEncodeCreateDTO>(JsonConvert.SerializeObject(GoodData2)), true, null, _validSasUri },
                    // Bad data.  Non-existant workflow specified.
                    new object[] { JsonHelpers.DeserializeFromString<RequestCloudPortEncodeCreateDTO>(JsonConvert.SerializeObject(BadData2)), true, typeof(GridwichCloudPortWorkflowDoesNotExistException), _validSasUri },
                    // Bad data, should throw. Null argument passed to method.
                    new object[] { null, true, typeof(ArgumentNullException), null },
                    // Bad data, should throw.  Unable to generate Sas Url.
                    new object[] { JsonHelpers.DeserializeFromString<RequestCloudPortEncodeCreateDTO>(JsonConvert.SerializeObject(BadData2)), true, typeof(GridwichCloudPortSASException), null },
                    // Bad data, should throw.  Blob does not exist
                    new object[] { JsonHelpers.DeserializeFromString<RequestCloudPortEncodeCreateDTO>(JsonConvert.SerializeObject(BadData2)), false, typeof(GridwichCloudPortMissingInputException), null },
                };
            }
        }

        [Theory]
        [MemberData(nameof(EncoderData))]
        public async void CloudPortTestWithGoodAndBadData(RequestCloudPortEncodeCreateDTO encodeCreateData, bool blobShouldExist, Type shouldThrowThis, Uri sasUri)
        {
            // Arrange
            var wfj = new WorkflowJob();

            Mock.Get(_telestreamCloudClientProvider.CloudPortApi)
                .Setup(x => x.CreateWorkflowJobAsync(It.IsAny<string>(), It.IsAny<WorkflowJob>()))
                .ReturnsAsync(wfj);

            var wf1 = new Workflow(name: "TestWorkflow2")
            {
                Input = new WorkflowInput
                {
                    Sources = new Dictionary<string, VantageNickName>() { { "any value", new VantageNickName() } },
                    Variables = new Dictionary<string, VantageVariable>(),
                }
            };
            wf1.Input.Variables.Add("var1", new VantageVariable("0", "0"));

            var wf2 = new Workflow();
            WorkflowsCollection wfc = new WorkflowsCollection()
            {
                Workflows = new List<Workflow>() { wf1, wf2 }
            };

            Mock.Get(_telestreamCloudClientProvider.CloudPortApi)
            .Setup(x => x.ListWorkflowsAsync(null, null, null))
            .ReturnsAsync(wfc);

            Mock.Get(_telestreamCloudClientProvider.CloudPortApi)
            .Setup(x => x.GetWorkflowAsync(It.IsAny<string>()))
            .ReturnsAsync(wf1);

            var sr = new StorageReference();
            wf1.Input.StorageReferences = new Dictionary<string, StorageReference>() { { "DavesStorageReference", sr } };

            Mock.Get(_storageService)
                .Setup(x => x.GetBlobExistsAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(blobShouldExist);

            Mock.Get(_storageService)
                .Setup(x => x.GetSasUrlForBlob(It.IsAny<Uri>(), It.IsAny<TimeSpan>(), It.IsAny<StorageClientProviderContext>()))
                .Returns(sasUri?.ToString());

            Mock.Get(_settingsProvider)
                .Setup(x => x.GetAppSettingsValue(It.IsAny<string>()))
                .Returns("telestreamCloudApiKey");

            Mock.Get(_telestreamCloudStorageProvider)
                .Setup(x => x.GetStoreByNameAsync(It.IsAny<Uri>()))
                .ReturnsAsync(new Store());

            // Act
            var cloudPortService = new CloudPortService(_storageService, _telestreamCloudClientProvider, _telestreamCloudStorageProvider);
            var ex = await Record.ExceptionAsync(async () => await cloudPortService.EncodeCreateAsync(encodeCreateData).ConfigureAwait(false)).ConfigureAwait(false);

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
