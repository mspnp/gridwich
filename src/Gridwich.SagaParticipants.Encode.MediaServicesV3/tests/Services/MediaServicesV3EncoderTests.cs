using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Azure.Storage.Blobs;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Interfaces;
using Gridwich.Core.MediaServicesV3.Exceptions;
using Gridwich.Core.Models;
using Gridwich.SagaParticipants.Encode;
using Gridwich.SagaParticipants.Encode.Exceptions;
using Gridwich.SagaParticipants.Encode.MediaServicesV3;
using Moq;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV3Tests
{
    /// <summary>
    /// Media Services V3 encoder class tests.
    /// </summary>
    public class MediaServicesV3EncoderTests
    {
        private static readonly IObjectLogger<MediaServicesV3Encoder> Log = Mock.Of<IObjectLogger<MediaServicesV3Encoder>>();
        private static readonly IStorageService StorageService = Mock.Of<IStorageService>();
        private static readonly IMediaServicesV3EncodeService AmsV3Services = Mock.Of<IMediaServicesV3EncodeService>();
        private readonly ITestOutputHelper output;

        /// <summary>
        /// Gets the encoding data.
        /// It's an array of test data to send to unit tests, with expected result matching that data.
        /// </summary>
        public static IEnumerable<object[]> EncoderData
        {
            get
            {
                return new[]
                {
                    new object[]
                    {
                        MediaServicesV3TestData.RequestMediaServicesV3EncodeCreateDTO_Is_Expected,
                        MediaServicesV3TestData.ServiceOperationResultEncodeDispatched_Is_Expected,
                        null,
                    },

                    new object[] { MediaServicesV3TestData.InputsUri_Is_NotAUri1, null, typeof(GridwichEncodeCreateDataException) },
                    new object[] { MediaServicesV3TestData.InputsUri_Is_NotAUri2, null, typeof(GridwichEncodeCreateDataException) },
                    new object[] { MediaServicesV3TestData.InputsUri_Is_Null, null, typeof(GridwichEncodeCreateDataException) },
                    new object[] { MediaServicesV3TestData.Inputs_Is_Empty, null, typeof(GridwichEncodeCreateDataException) },
                    new object[] { MediaServicesV3TestData.Inputs_Is_Null, null, typeof(GridwichEncodeCreateDataException) },

                    new object[] { MediaServicesV3TestData.OutputContainer_Is_IsNotUri, null, typeof(GridwichEncodeInvalidOutputContainerException) },
                    new object[] { MediaServicesV3TestData.OutputContainer_Is_Null, null, typeof(GridwichEncodeInvalidOutputContainerException) },
                    new object[] { MediaServicesV3TestData.OutputContainer_Is_StringEmpty, null, typeof(GridwichEncodeInvalidOutputContainerException) },
                    new object[] { MediaServicesV3TestData.OutputContainer_Is_Whitespace, null, typeof(GridwichEncodeInvalidOutputContainerException) },

                    // Note: This is business logic for the EncodeService:
                    // new object[] { MediaServicesV3TestData.OutputContainer_Is_UnexpectedAccount, null, typeof(GridwichEncodeInvalidOutputContainerException) },

                    // Note: This is business logic for the TransformService:
                    // new object[] { MediaServicesV3TestData.TransformName_Is_Unexpected, null, typeof(GridwichEncodeCreateDataException) },

                    new object[] { MediaServicesV3TestData.TransformName_Is_Null, null, typeof(GridwichEncodeCreateDataException) },
                    new object[] { MediaServicesV3TestData.TransformName_Is_StringEmpty, null, typeof(GridwichEncodeCreateDataException) },
                    new object[] { MediaServicesV3TestData.TransformName_Is_Whitespace, null, typeof(GridwichEncodeCreateDataException) },

                    new object[] { MediaServicesV3TestData.OperationContext_Is_Null, null, typeof(GridwichEncodeCreateJobException) },
                };
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServicesV3EncoderTests"/> class.
        /// </summary>
        /// <param name="output">Allows WriteLine in xunit tests.</param>
        public MediaServicesV3EncoderTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Test the encoding when sending null JSON payload.
        /// </summary>
        [Fact]
        public async void MediaServicesEncoderEncodeNullTest()
        {
            // Arrange
            RequestMediaServicesV3EncodeCreateDTO encodeCreateDTO = null;

            // Assert
            var amsEncoder = new MediaServicesV3Encoder(Log, StorageService, AmsV3Services);
            await Xunit.Assert.ThrowsAsync<ArgumentNullException>(async () => await amsEncoder.EncodeCreateAsync(encodeCreateDTO).ConfigureAwait(false)).ConfigureAwait(false);
        }


        /// <summary>
        /// Test a number of good and bad input cases.
        /// </summary>
        /// <param name="encodeCreateDTO">input data.</param>
        /// <param name="expectedEncodeDispatched">expected output.</param>
        /// <param name="expectedExceptionType">expected exception type.</param>
        [Theory]
        [MemberData(nameof(EncoderData))]
        public async void EncodeCreateAsync_Is_As_ExpectedAsync(
            RequestMediaServicesV3EncodeCreateDTO encodeCreateDTO,
            ServiceOperationResultEncodeDispatched expectedEncodeDispatched,
            Type expectedExceptionType)
        {
            // Arrange
            string expectedOutputAssetName = GetExpectedOutputAssetName(encodeCreateDTO);
            output.WriteLine($"Using expectedOutputAssetName: {expectedOutputAssetName}");

            // Arrange Mocks
            Mock.Get(AmsV3Services)
                .Setup(x => x.CreateTransformIfNotExistByNameAsync(It.IsAny<string>(), It.IsAny<JObject>()));

            Mock.Get(StorageService)
                .Setup(x => x.GetBlobExistsAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(true);

            Mock.Get(AmsV3Services)
                .Setup(x => x.CreateJobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeBasedEncodeDTO>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<JObject>()));

            Mock.Get(AmsV3Services)
                .Setup(x => x.CreateOrUpdateAssetForContainerAsync(It.IsAny<IEnumerable<Uri>>()))
                .ReturnsAsync(expectedOutputAssetName);

            // Act
            var amsEncoder = new MediaServicesV3Encoder(Log, StorageService, AmsV3Services);
            ServiceOperationResultEncodeDispatched encodeDispatched = null;
            var exception = await Record.ExceptionAsync(async () =>
            {
                encodeDispatched = await amsEncoder.EncodeCreateAsync(encodeCreateDTO).ConfigureAwait(true);
            }).ConfigureAwait(true);

            // Assert
            if (expectedExceptionType == null)
            {
                // Success cases
                exception.ShouldBeNull();
            }
            else
            {
                // Failure cases
                exception.ShouldBeOfType(expectedExceptionType);
            }

            if (expectedEncodeDispatched != null)
            {
                // Success cases
                if (MediaServicesV3TestData.ServiceOperationResultEncodeDispatched_Is_Expected.WorkflowJobName == null)
                {
                    encodeDispatched.WorkflowJobName.ShouldBeNull();
                }
                else
                {
                    encodeDispatched.WorkflowJobName.StartsWith(expectedOutputAssetName);
                }

                if (MediaServicesV3TestData.ServiceOperationResultEncodeDispatched_Is_Expected.EncoderContext == null)
                {
                    encodeDispatched.EncoderContext.ShouldBeNull();
                }
                else
                {
                    encodeDispatched.EncoderContext.ShouldBe(MediaServicesV3TestData.ServiceOperationResultEncodeDispatched_Is_Expected.EncoderContext);
                }

                if (MediaServicesV3TestData.ServiceOperationResultEncodeDispatched_Is_Expected.OperationContext == null)
                {
                    encodeDispatched.OperationContext.ShouldBeNull();
                }
                else
                {
                    encodeDispatched.OperationContext.ShouldBe(MediaServicesV3TestData.ServiceOperationResultEncodeDispatched_Is_Expected.OperationContext);
                }
            }
            else
            {
                // Failure cases
                encodeDispatched.ShouldBeNull();
            }
        }

        private static string GetExpectedOutputAssetName(RequestMediaServicesV3EncodeCreateDTO encodeCreateDTO)
        {
            var expectedAssetName = $"{MediaServicesV3TestData.GoodOutputAccountName}-{MediaServicesV3TestData.GoodOutputContainer}";
            if (Uri.TryCreate(encodeCreateDTO?.OutputContainer, UriKind.Absolute, out Uri outputContainerUri))
            {
                var bub = new BlobUriBuilder(outputContainerUri);
                expectedAssetName = $"{bub.AccountName}-{bub.BlobContainerName}";
            }

            return expectedAssetName;
        }

        /// <summary>
        /// Test the MediaServicesV3Encoder.EncodeAsync throws when connecting to
        /// media services throws GridwichMediaServicesV3ConnectivityException in CreateTransformIfNotExistByNameAsync.
        /// </summary>
        [Fact]
        public async void EncoderRethrowsFromCreateTransformIfNotExistByNameAsync()
        {
            // Arrange Mocks
            // Note LogEventIds used do not affect this test.
            Mock.Get(AmsV3Services)
                .Setup(x => x.CreateTransformIfNotExistByNameAsync(It.IsAny<string>(), It.IsAny<JObject>()))
                .ThrowsAsync(new GridwichMediaServicesV3ConnectivityException(
                    LogEventIds.MediaServicesV3ConnectionError.Name,
                    LogEventIds.MediaServicesV3ConnectionError));


            Mock.Get(AmsV3Services)
                .Setup(x => x.CreateJobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeBasedEncodeDTO>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<JObject>()));

            Mock.Get(AmsV3Services)
                .Setup(x => x.CreateOrUpdateAssetForContainerAsync(It.IsAny<IEnumerable<Uri>>()))
                .ReturnsAsync("assetname");

            // Act
            var amsEncoder = new MediaServicesV3Encoder(Log, StorageService, AmsV3Services);
            ServiceOperationResultEncodeDispatched encodeDispatched = null;
            var exception = await Record.ExceptionAsync(async () =>
            {
                encodeDispatched = await amsEncoder.EncodeCreateAsync(MediaServicesV3TestData.RequestMediaServicesV3EncodeCreateDTO_Is_Expected).ConfigureAwait(true);
            }).ConfigureAwait(true);

            // Assert
            exception.ShouldNotBeNull();
            exception.ShouldBeOfType<GridwichMediaServicesV3CreateTransformException>();
            exception.InnerException.ShouldNotBeNull();
            exception.InnerException.ShouldBeOfType<GridwichMediaServicesV3ConnectivityException>();
        }

        /// <summary>
        /// Test the MediaServicesV3Encoder.EncodeAsync throws when connecting to
        /// media services throws GridwichMediaServicesV3ConnectivityException in CreateJobAsync.
        /// </summary>
        [Fact]
        public async void EncoderLogsAndThrowsIfInputBlobsDoNotExist()
        {
            // Arrange Mocks
            Mock.Get(AmsV3Services)
                .Setup(x => x.CreateTransformIfNotExistByNameAsync(It.IsAny<string>(), It.IsAny<JObject>()));

            var mockUriToAppearNonexistent = new Uri(MediaServicesV3TestData.RequestMediaServicesV3EncodeCreateDTO_Is_Expected.Inputs.First().BlobUri);
            Mock.Get(StorageService)
                .Setup(x => x.GetBlobExistsAsync(mockUriToAppearNonexistent, It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(false);

            // Act
            var amsEncoder = new MediaServicesV3Encoder(Log, StorageService, AmsV3Services);
            ServiceOperationResultEncodeDispatched encodeDispatched = null;
            var exception = await Record.ExceptionAsync(async () =>
            {
                encodeDispatched = await amsEncoder.EncodeCreateAsync(MediaServicesV3TestData.RequestMediaServicesV3EncodeCreateDTO_Is_Expected).ConfigureAwait(true);
            }).ConfigureAwait(true);

            // Assert
            exception.ShouldNotBeNull();
            exception.ShouldBeOfType<GridwichMediaServicesV3Exception>();
            Mock.Get(Log).Verify(x => x.LogEventObject(LogEventIds.MediaServicesV3AttemptToUseNonexistentBlob,
                It.IsAny<object>()), Times.Once, "The exception should be logged.");
        }

        /// <summary>
        /// Test the MediaServicesV3Encoder.EncodeAsync throws when connecting to
        /// media services throws GridwichMediaServicesV3ConnectivityException in CreateJobAsync.
        /// </summary>
        [Fact]
        public async void EncoderRethrowsFromCreateJobAsync()
        {
            // Arrange Mocks
            Mock.Get(AmsV3Services)
                .Setup(x => x.CreateTransformIfNotExistByNameAsync(It.IsAny<string>(), It.IsAny<JObject>()));

            Mock.Get(StorageService)
                .Setup(x => x.GetBlobExistsAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(true);

            // Note LogEventIds used do not affect this test.
            Mock.Get(AmsV3Services)
                .Setup(x => x.CreateJobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeBasedEncodeDTO>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<JObject>()))
                .ThrowsAsync(new GridwichMediaServicesV3ConnectivityException(
                    LogEventIds.MediaServicesV3ConnectionAdalError.Name,
                    LogEventIds.MediaServicesV3ConnectionAdalError));

            Mock.Get(AmsV3Services)
                .Setup(x => x.CreateOrUpdateAssetForContainerAsync(It.IsAny<IEnumerable<Uri>>()))
                .ReturnsAsync("assetname");

            // Act
            var amsEncoder = new MediaServicesV3Encoder(Log, StorageService, AmsV3Services);
            ServiceOperationResultEncodeDispatched encodeDispatched = null;
            var exception = await Record.ExceptionAsync(async () =>
            {
                encodeDispatched = await amsEncoder.EncodeCreateAsync(MediaServicesV3TestData.RequestMediaServicesV3EncodeCreateDTO_Is_Expected).ConfigureAwait(true);
            }).ConfigureAwait(true);

            // Assert
            exception.ShouldNotBeNull();
            exception.ShouldBeOfType<GridwichEncodeCreateJobException>();
            exception.InnerException.ShouldNotBeNull();
            exception.InnerException.ShouldBeOfType<GridwichMediaServicesV3ConnectivityException>();
        }

        /// <summary>
        /// Test the MediaServicesV3Encoder.EncodeAsync throws when connecting to
        /// media services throws GridwichMediaServicesV3ConnectivityException in CreateOrUpdateAssetForContainerAsync.
        /// </summary>
        [Fact]
        public async void EncoderRethrowsFromCreateOrUpdateAssetForContainerAsync()
        {
            // Arrange Mocks
            Mock.Get(AmsV3Services)
                .Setup(x => x.CreateTransformIfNotExistByNameAsync(It.IsAny<string>(), It.IsAny<JObject>()));

            Mock.Get(StorageService)
                .Setup(x => x.GetBlobExistsAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(true);

            Mock.Get(AmsV3Services)
                .Setup(x => x.CreateJobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeBasedEncodeDTO>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<JObject>()));

            // Note LogEventIds used do not affect this test.
            Mock.Get(AmsV3Services)
                .Setup(x => x.CreateOrUpdateAssetForContainerAsync(It.IsAny<IEnumerable<Uri>>()))
                .ThrowsAsync(new GridwichMediaServicesV3ConnectivityException(
                    LogEventIds.MediaServicesV3ConnectionAdalError.Name,
                    LogEventIds.MediaServicesV3ConnectionAdalError));

            // Act
            var amsEncoder = new MediaServicesV3Encoder(Log, StorageService, AmsV3Services);
            ServiceOperationResultEncodeDispatched encodeDispatched = null;
            var exception = await Record.ExceptionAsync(async () =>
            {
                encodeDispatched = await amsEncoder.EncodeCreateAsync(MediaServicesV3TestData.RequestMediaServicesV3EncodeCreateDTO_Is_Expected).ConfigureAwait(true);
            }).ConfigureAwait(true);

            // Assert
            exception.ShouldNotBeNull();
            exception.ShouldBeOfType<GridwichMediaServicesV3CreateAssetException>();
            exception.InnerException.ShouldNotBeNull();
            exception.InnerException.ShouldBeOfType<GridwichMediaServicesV3ConnectivityException>();
        }

        /// <summary>
        /// Test the encoding error when getting a transform throws an error.
        /// </summary>
        [Fact]
        public async void MediaServicesV3EncoderErrorGetTransformTest()
        {
            // Arrange Mocks
            Mock.Get(AmsV3Services)
                .Setup(x => x.CreateTransformIfNotExistByNameAsync(It.IsAny<string>(), It.IsAny<JObject>()))
                .Throws(new Exception());

            Mock.Get(AmsV3Services)
                .Setup(x => x.CreateJobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeBasedEncodeDTO>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<JObject>()));

            Mock.Get(AmsV3Services)
                .Setup(x => x.CreateOrUpdateAssetForContainerAsync(It.IsAny<IEnumerable<Uri>>()))
                .ReturnsAsync("assetname");

            // Act
            var amsEncoder = new MediaServicesV3Encoder(Log, StorageService, AmsV3Services);
            ServiceOperationResultEncodeDispatched encodeDispatched = null;
            var exception = await Record.ExceptionAsync(async () =>
            {
                encodeDispatched = await amsEncoder.EncodeCreateAsync(MediaServicesV3TestData.RequestMediaServicesV3EncodeCreateDTO_Is_Expected).ConfigureAwait(true);
            }).ConfigureAwait(true);

            // Assert
            exception.ShouldNotBeNull();
            exception.ShouldBeOfType<GridwichMediaServicesV3CreateTransformException>();
            exception.InnerException.ShouldNotBeNull();
            exception.InnerException.ShouldBeOfType<Exception>();
        }

        /// <summary>
        /// Test the encoding error when creating an asset throws an error.
        /// </summary>
        [Fact]
        public async void MediaServicesEncoderErrorCreateAssetTest()
        {
            // Arrange Mocks
            Mock.Get(AmsV3Services)
                .Setup(x => x.CreateTransformIfNotExistByNameAsync(It.IsAny<string>(), It.IsAny<JObject>()));

            Mock.Get(StorageService)
                .Setup(x => x.GetBlobExistsAsync(It.IsAny<Uri>(), It.IsAny<StorageClientProviderContext>()))
                .ReturnsAsync(true);

            Mock.Get(AmsV3Services)
                .Setup(x => x.CreateJobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeBasedEncodeDTO>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<JObject>()));

            Mock.Get(AmsV3Services)
                .Setup(x => x.CreateOrUpdateAssetForContainerAsync(It.IsAny<IEnumerable<Uri>>()))
                .Throws(new Exception());

            // Act
            var amsEncoder = new MediaServicesV3Encoder(Log, StorageService, AmsV3Services);
            ServiceOperationResultEncodeDispatched encodeDispatched = null;
            var exception = await Record.ExceptionAsync(async () =>
            {
                encodeDispatched = await amsEncoder.EncodeCreateAsync(MediaServicesV3TestData.RequestMediaServicesV3EncodeCreateDTO_Is_Expected).ConfigureAwait(true);
            }).ConfigureAwait(true);

            // Assert
            exception.ShouldNotBeNull();
            exception.ShouldBeOfType<GridwichMediaServicesV3CreateAssetException>();
            exception.InnerException.ShouldNotBeNull();
            exception.InnerException.ShouldBeOfType<Exception>();
        }
    }
}