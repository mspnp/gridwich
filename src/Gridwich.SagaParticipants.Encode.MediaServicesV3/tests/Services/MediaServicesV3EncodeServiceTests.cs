using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.Core.MediaServicesV3;
using Gridwich.SagaParticipants.Encode.MediaServicesV3;
using Gridwich.SagaParticipants.Encode.MediaServicesV3.Transforms;
using Gridwich.Services.Core.Exceptions;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Rest;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV3Tests
{
    /// <summary>
    /// Tests for the Media Services V3 Service class implementation.
    /// </summary>
    public class MediaServicesV3EncodeServiceTests
    {
        private const string DefaultStorageId = "/subscriptions/28a75405-95db-4d15-9a7f-ab84003a63aa/resourceGroups/xpouyatdemo/providers/microsoft.storage/storageAccounts/mystorageact";

        private static readonly string GoodData = "{'operationContext' : { 'id' : 'ABC' }, 'inputs' : [{'blobUri' : 'https://mystorageact.blob.core.windows.net/container1/bbb.mp4'}, {'blobUri' : 'https://mystorageact.blob.core.windows.net/container1/bbb2.mp4'}], 'outputContainer' : 'https://mystorageact.blob.core.windows.net/output/', 'encoderSpecificData' : { 'FlipEncoder' : { 'flipFactoryId': 'abc123','flipProfiles' : 'h264', 'parameters': [{'prop1':'value1'}, {'prop2' : 2} ] }, 'FFMPEGEncoder' : { 'someData' : 'blahblah' }, 'mediaServicesV3' : { 'transformName' : 'mytransform' }}}".Replace("'", "\"", StringComparison.InvariantCulture);
        private static readonly string BadSeveralSourceStorageAccounts = "{'operationContext' : { 'id' : 'ABC' }, 'inputs' : [{'blobUri' : 'https://mystorageact.blob.core.windows.net/container1/bbb.mp4'}, {'blobUri' : 'https://mystorageact2.blob.core.windows.net/container1/bbb2.mp4'}], 'outputContainer' : 'https://mystorageact.blob.core.windows.net/output/', 'encoderSpecificData' : { 'FlipEncoder' : { 'flipFactoryId': 'abc123','flipProfiles' : 'h264', 'parameters': [{'prop1':'value1'}, {'prop2' : 2} ] }, 'FFMPEGEncoder' : { 'someData' : 'blahblah' }, 'mediaServicesV3' : { 'transformName' : 'mytransform' }}}".Replace("'", "\"", StringComparison.InvariantCulture);
        private static readonly string BadSeveralSourceContainers = "{'operationContext' : { 'id' : 'ABC' }, 'inputs' : [{'blobUri' : 'https://mystorageact.blob.core.windows.net/container1/bbb.mp4'}, {'blobUri' : 'https://mystorageact.blob.core.windows.net/container2/bbb2.mp4'}], 'outputContainer' : 'https://mystorageact.blob.core.windows.net/output/', 'encoderSpecificData' : { 'FlipEncoder' : { 'flipFactoryId': 'abc123','flipProfiles' : 'h264', 'parameters': [{'prop1':'value1'}, {'prop2' : 2} ] }, 'FFMPEGEncoder' : { 'someData' : 'blahblah' }, 'mediaServicesV3' : { 'transformName' : 'mytransform' }}}".Replace("'", "\"", StringComparison.InvariantCulture);
        private static readonly string BaddNoSource = "{'operationContext' : { 'id' : 'ABC' }, 'inputs' : [], 'outputContainer' : 'https://mystorageact.blob.core.windows.net/output/', 'encoderSpecificData' : { 'FlipEncoder' : { 'flipFactoryId': 'abc123','flipProfiles' : 'h264', 'parameters': [{'prop1':'value1'}, {'prop2' : 2} ] }, 'FFMPEGEncoder' : { 'someData' : 'blahblah' }, 'mediaServicesV3' : { 'transformName' : 'mytransform' }}}".Replace("'", "\"", StringComparison.InvariantCulture);
        private static readonly string BadSourceStorageNotAttached = "{'operationContext' : { 'id' : 'ABC' }, 'inputs' : [{'blobUri' : 'https://mystorageactnotattached.blob.core.windows.net/container1/bbb.mp4'}], 'outputContainer' : 'https://mystorageact.blob.core.windows.net/output/', 'encoderSpecificData' : { 'FlipEncoder' : { 'flipFactoryId': 'abc123','flipProfiles' : 'h264', 'parameters': [{'prop1':'value1'}, {'prop2' : 2} ] }, 'FFMPEGEncoder' : { 'someData' : 'blahblah' }, 'mediaServicesV3' : { 'transformName' : 'mytransform' }}}".Replace("'", "\"", StringComparison.InvariantCulture);

        private static readonly JObject DefaultOperationContext = JObject.Parse("{\"id\":\"ABC\"}");

        private static readonly IMediaServicesV3SdkWrapper AmsV3SdkWrapper = Mock.Of<IMediaServicesV3SdkWrapper>();
        private static readonly IObjectLogger<MediaServicesV3BaseService> Log = Mock.Of<IObjectLogger<MediaServicesV3BaseService>>();

        /// <summary>
        /// Gets an array of test data to send to unit tests, with expected result matching that data.
        /// </summary>
        public static IEnumerable<object[]> OperationsData
        {
            get
            {
                return new[]
                {
                    new object[] { GoodData, true },
                    new object[] { BadSeveralSourceContainers, false },
                    new object[] { BaddNoSource, false },
                    new object[] { BadSeveralSourceStorageAccounts, false },
                    new object[] { BadSourceStorageNotAttached, false },
                };
            }
        }

        /// <summary>
        /// Testing the Media Services V3 Service Create Asset feature.
        /// </summary>
        /// <param name="jData">JSON data.</param>
        /// <param name="expectedValue">Expected result.</param>
        [Theory]
        [MemberData(nameof(OperationsData))]
        public async void MediaServicesV3ServiceCreateAssetTest(string jData, bool expectedValue)
        {
            // Arrange
            RequestEncodeCreateDTO encodeRequestData = JsonConvert.DeserializeObject<RequestEncodeCreateDTO>(jData);
            string assetName = "myassetname";
            var amsAccount = new MediaService(location: "westus", storageAccounts: new List<StorageAccount>() { new StorageAccount() { Id = DefaultStorageId } });
            // TODO
            var amsV3TransformService = Mock.Of<IMediaServicesV3TransformService>();

            // Arrange Mocks
            Mock.Get(AmsV3SdkWrapper)
                .Setup(x => x.AssetCreateOrUpdateAsync(It.IsAny<string>(), It.IsAny<Asset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Asset(name: assetName));

            Mock.Get(AmsV3SdkWrapper)
               .Setup(x => x.AssetGetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Asset(name: assetName));

            Mock.Get(AmsV3SdkWrapper)
                .Setup(x => x.MediaservicesGetAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(amsAccount);

            // Act
            var inputUrls = encodeRequestData.Inputs.ToList().Select(i => new Uri(i.BlobUri)).ToList();
            var amsV3Services = new MediaServicesV3EncodeService(amsV3TransformService, AmsV3SdkWrapper, Log);

            // Assert
            if (expectedValue == false)
            {
                _ = await Xunit.Assert.ThrowsAsync<Exception>(async () => await amsV3Services.CreateOrUpdateAssetForContainerAsync(inputUrls).ConfigureAwait(false)).ConfigureAwait(false);
            }
            else
            {
                var result = await amsV3Services.CreateOrUpdateAssetForContainerAsync(inputUrls).ConfigureAwait(false);
                result.ShouldBeOfType<string>();
                result.ShouldBe(assetName);
            }
        }

        /// <summary>
        /// Testing the Media Services V3 Service Get Existing Transform feature.
        /// </summary>
        [Fact]
        public async void MediaServicesV3ServiceCanGetExistingTransformTest()
        {
            // Arrange
            var storage = new MediaService(location: "uswest", storageAccounts: new List<StorageAccount>() { new StorageAccount() { Id = DefaultStorageId } });
            var tOutputs = new List<TransformOutput>();
            string tNameExisting = "transformwhichexists";
            var amsV3TransformService = Mock.Of<IMediaServicesV3TransformService>();

            // Arrange Mocks
            Mock.Get(AmsV3SdkWrapper)
               .Setup(x => x.TransformGetAsync(tNameExisting, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Transform());

            // Act
            var amsV3Services = new MediaServicesV3EncodeService(amsV3TransformService, AmsV3SdkWrapper, Log);

            // Assert
            var exception = await Record.ExceptionAsync(async () => await amsV3Services.CreateTransformIfNotExistByNameAsync(tNameExisting, DefaultOperationContext).ConfigureAwait(false)).ConfigureAwait(false);
            Xunit.Assert.Null(exception);
            Mock.Get(AmsV3SdkWrapper)
                .Verify(x => x.TransformCreateOrUpdateAsync(
                            It.IsAny<string>(), It.IsAny<TransformOutput[]>(),
                            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Testing the Media Services V3 Service Create Transform feature when not in Azure Media Services but in the Media Services V3 Transform Dictionary.
        /// </summary>
        [Fact]
        public async void MediaServicesV3ServiceCanCreateTransformTest()
        {
            // Arrange
            var storage = new MediaService(location: "westus", storageAccounts: new List<StorageAccount>() { new StorageAccount() { Id = DefaultStorageId } });
            var tOutputs = new List<TransformOutput>();
            string tNameNotExisting = "transformwhichdoesnotexist";
            var amsV3TransformService = Mock.Of<IMediaServicesV3TransformService>();
            MediaServicesV3TransformBase amsV3Transform = new MediaServicesV3PresetTransform(EncoderNamedPreset.AdaptiveStreaming);
            var response = new HttpResponseMessageWrapper(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound), "AMS object not found");

            // Arrange Mocks
            Mock.Get(AmsV3SdkWrapper)
               .Setup(x => x.TransformGetAsync(tNameNotExisting, It.IsAny<CancellationToken>()))
               .Throws(new ErrorResponseException() { Response = response });

            Mock.Get(amsV3TransformService)
                .Setup(x => x.GetTransform(tNameNotExisting))
                .Returns(amsV3Transform);

            // Act
            var amsV3Services = new MediaServicesV3EncodeService(amsV3TransformService, AmsV3SdkWrapper, Log);

            // Assert
            var exception = await Record.ExceptionAsync(async () => await amsV3Services.CreateTransformIfNotExistByNameAsync(tNameNotExisting, DefaultOperationContext).ConfigureAwait(false)).ConfigureAwait(false);
            Xunit.Assert.Null(exception);
            Mock.Get(AmsV3SdkWrapper).Verify(x => x.TransformCreateOrUpdateAsync(tNameNotExisting, It.IsAny<IList<TransformOutput>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Testing the Media Services V3 Service Create Transform feature when the Transform doesn't exist in the Media Services V3 Transform Dictionary.
        /// </summary>
        [Fact]
        public async void MediaServicesV3ServiceCreateTransformThrowsExceptionWhenTransformNotInDictionaryTest()
        {
            // Arrange
            var storage = new MediaService(location: "westus", storageAccounts: new List<StorageAccount>() { new StorageAccount() { Id = DefaultStorageId } });
            var tOutputs = new List<TransformOutput>();
            string tNameNotExisting = "transformwhichdoesnotexist";
            var amsV3TransformService = Mock.Of<IMediaServicesV3TransformService>();
            MediaServicesV3TransformBase nullAmsV3Transform = null;
            var response = new HttpResponseMessageWrapper(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound), "AMS object not found");

            // Arrange Mocks
            Mock.Get(AmsV3SdkWrapper)
               .Setup(x => x.TransformGetAsync(tNameNotExisting, It.IsAny<CancellationToken>()))
               .Throws(new ErrorResponseException() { Response = response });

            Mock.Get(amsV3TransformService)
                .Setup(x => x.GetTransform(tNameNotExisting))
                .Returns(nullAmsV3Transform);

            // Act
            var amsV3Services = new MediaServicesV3EncodeService(amsV3TransformService, AmsV3SdkWrapper, Log);

            // Assert
            var exception = await Record.ExceptionAsync(async () => await amsV3Services.CreateTransformIfNotExistByNameAsync(tNameNotExisting, DefaultOperationContext).ConfigureAwait(false)).ConfigureAwait(false);
            Xunit.Assert.NotNull(exception);
            Mock.Get(AmsV3SdkWrapper).Verify(x => x.TransformCreateOrUpdateAsync(tNameNotExisting, It.IsAny<TransformOutput[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Testing the Media Services V3 Service Create Job feature.
        /// </summary>
        [Fact]
        public async void MediaServicesOperationsCreateJobTest()
        {
            // Arrange
            // TODO
            var amsV3TransformService = Mock.Of<IMediaServicesV3TransformService>();

            // Arrange Mocks
            Mock.Get(AmsV3SdkWrapper)
                .Setup(x => x.JobCreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Job>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Job());

            // Act
            var amsV3Services = new MediaServicesV3EncodeService(amsV3TransformService, AmsV3SdkWrapper, Log);

            // Assert
            var exception = await Record.ExceptionAsync(async () => await amsV3Services.CreateJobAsync("mytransform", "myinputassetname", "myoutputassetname", "myjobname", null, It.IsAny<Dictionary<string, string>>(), DefaultOperationContext).ConfigureAwait(false)).ConfigureAwait(false);
            Assert.Null(exception); // no exception if string are not null

            // exception is string are null
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await amsV3Services.CreateJobAsync(null, null, null, null, null, It.IsAny<Dictionary<string, string>>(), DefaultOperationContext).ConfigureAwait(false)).ConfigureAwait(false);

            // exception is string are empty
            await Assert.ThrowsAsync<ArgumentException>(async () => await amsV3Services.CreateJobAsync(string.Empty, string.Empty, string.Empty, string.Empty, null, It.IsAny<Dictionary<string, string>>(), DefaultOperationContext).ConfigureAwait(false)).ConfigureAwait(false);
        }
    }
}