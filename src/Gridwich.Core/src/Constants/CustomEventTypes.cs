namespace Gridwich.Core.Constants
{
    /// <summary>
    /// Defines the EventGridEvent.EventType of the various DTOs.
    /// </summary>
    public static class CustomEventTypes
    {
        /// <summary>All request events have a subject starting with this string</summary>
        public const string RequestCommonPrefix = "request.";

        /// <summary>All response events have a subject starting with this string</summary>
        public const string ResponseCommonPrefix = "response.";

        /// <summary>response.acknowledge</summary>
        public const string ResponseAcknowledge = ResponseCommonPrefix + "acknowledge";

        /// <summary>
        /// response.failure
        /// </summary>
        public const string ResponseFailure = ResponseCommonPrefix + "failure";

        /// <summary>
        /// request.blob.metadata.create
        /// </summary>
        public const string RequestCreateMetadata = RequestCommonPrefix + "blob.metadata.create";

        /// <summary>
        /// response.blob.metadata.success
        /// </summary>
        public const string ResponseMetadataCreated = ResponseCommonPrefix + "blob.metadata.success";

        /// <summary>
        /// request.blob.analysis.create
        /// </summary>
        public const string RequestBlobAnalysisCreate = RequestCommonPrefix + "blob.analysis.create";

        /// <summary>
        /// response.blob.analysis.success
        /// </summary>
        public const string ResponseBlobAnalysisSuccess = ResponseCommonPrefix + "blob.analysis.success";

        /// <summary>
        /// request.blob.copy
        /// </summary>
        public const string RequestBlobCopy = RequestCommonPrefix + "blob.copy";

        /// <summary>
        /// response.blob.copy.scheduled
        /// </summary>
        public const string ResponseBlobCopyScheduled = ResponseCommonPrefix + "blob.copy.scheduled";

        /// <summary>
        /// response.blob.created.success
        /// </summary>
        public const string ResponseBlobCreatedSuccess = ResponseCommonPrefix + "blob.created.success";

        /// <summary>
        /// request.blob.delete
        /// </summary>
        public const string RequestBlobDelete = RequestCommonPrefix + "blob.delete";

        /// <summary>
        /// response.blob.delete.scheduled
        /// </summary>
        public const string ResponseBlobDeleteScheduled = ResponseCommonPrefix + "blob.delete.scheduled";

        /// <summary>
        /// response.blob.delete.success
        /// </summary>
        public const string ResponseBlobDeleteSuccess = ResponseCommonPrefix + "blob.delete.success";

        /// <summary>
        /// request.blob.sas-url.create
        /// </summary>
        public const string RequestBlobSasUrlCreate = RequestCommonPrefix + "blob.sas-url.create";

        /// <summary>
        /// response.blob.sas-url.success
        /// </summary>
        public const string ResponseBlobSasUrlSuccess = ResponseCommonPrefix + "blob.sas-url.success";

        /// <summary>
        /// request.encode.create
        /// </summary>
        public const string RequestEncodeCreate = RequestCommonPrefix + "encode.create";

        /// <summary>
        /// request.encode.flip.create
        /// </summary>
        public const string RequestEncodeFlipCreate = RequestCommonPrefix + "encode.flip.create";

        /// <summary>
        /// response.encode.flip.dispatched
        /// </summary>
        public const string ResponseEncodeFlipDispatched = ResponseCommonPrefix + "encode.flip.dispatched";

        /// <summary>
        /// response.encode.flip.scheduled
        /// </summary>
        public const string ResponseEncodeFlipScheduled = ResponseCommonPrefix + "encode.flip.scheduled";

        /// <summary>
        /// response.encode.flip.processing
        /// </summary>
        public const string ResponseEncodeFlipProcessing = ResponseCommonPrefix + "encode.flip.processing";

        /// <summary>
        /// response.encode.flip.success
        /// </summary>
        public const string ResponseEncodeFlipSuccess = ResponseCommonPrefix + "encode.flip.success";

        /// <summary>
        /// request.encode.flip.create
        /// </summary>
        public const string RequestEncodeCloudPortCreate = RequestCommonPrefix + "encode.cloudport.create";

        /// <summary>
        /// response.encode.cloudport.dispatched
        /// </summary>
        public const string ResponseEncodeCloudPortDispatched = ResponseCommonPrefix + "encode.cloudport.dispatched";

        /// <summary>
        /// response.encode.cloudport.scheduled
        /// </summary>
        public const string ResponseEncodeCloudPortScheduled = ResponseCommonPrefix + "encode.cloudport.scheduled";

        /// <summary>
        /// response.encode.cloudport.processing
        /// </summary>
        public const string ResponseEncodeCloudPortProcessing = ResponseCommonPrefix + "encode.cloudport.processing";

        /// <summary>
        /// response.encode.cloudport.success
        /// </summary>
        public const string ResponseEncodeCloudportSuccess = ResponseCommonPrefix + "encode.cloudport.success";

        /// <summary>
        /// request.encode.mediaservicesv2.create
        /// </summary>
        public const string RequestEncodeMediaServicesV2Create = RequestCommonPrefix + "encode.mediaservicesv2.create";

        /// <summary>
        /// response.encode.mediaservicesv2.dispatched
        /// </summary>
        public const string ResponseEncodeMediaServicesV2Dispatched = ResponseCommonPrefix + "encode.mediaservicesv2.dispatched";

        /// <summary>
        /// response.encode.mediaservicesv2.scheduled
        /// </summary>
        public const string ResponseEncodeMediaServicesV2Scheduled = ResponseCommonPrefix + "encode.mediaservicesv2.scheduled";

        /// <summary>
        /// response.encode.mediaservicesv2.processing
        /// </summary>
        public const string ResponseEncodeMediaServicesV2Processing = ResponseCommonPrefix + "encode.mediaservicesv2.processing";

        /// <summary>
        /// response.encode.mediaservicesv2.success
        /// </summary>
        public const string ResponseEncodeMediaServicesV2Success = ResponseCommonPrefix + "encode.mediaservicesv2.success";

        /// <summary>
        /// response.encode.mediaservicesv2.canceled
        /// </summary>
        public const string ResponseEncodeMediaServicesV2Canceled = ResponseCommonPrefix + "encode.mediaservicesv2.canceled";

        /// <summary>
        /// response.encode.mediaservicesv2.status.progress
        /// </summary>
        public const string ResponseEncodeMediaServicesV2StatusProgress = ResponseCommonPrefix + "encode.mediaservicesv2.status.progress";

        /// <summary>
        /// response.encode.mediaservicesv2.status.unknown
        /// </summary>
        public const string ResponseEncodeMediaServicesV2UnknownStatus = ResponseCommonPrefix + "encode.mediaservicesv2.status.unknown";

        /// <summary>
        /// response.encode.mediaservicesv2.status.unknown
        /// Used for "mapping" an AMSv2 webhook into an event we can handle with EventGrid
        /// </summary>
        public const string ResponseEncodeMediaServicesV2TranslateCallback = ResponseCommonPrefix + "encode.mediaservicesv2.translate.callback";

        /// <summary>
        /// request.encode.mediaservicesv3.create
        /// </summary>
        public const string RequestEncodeMediaServicesV3Create = RequestCommonPrefix + "encode.mediaservicesv3.create";

        /// <summary>
        /// response.encode.mediaservicesv3.dispatched
        /// </summary>
        public const string ResponseEncodeMediaServicesV3Dispatched = ResponseCommonPrefix + "encode.mediaservicesv3.dispatched";

        /// <summary>
        /// response.encode.mediaservicesv3.scheduled
        /// </summary>
        public const string ResponseEncodeMediaServicesV3Scheduled = ResponseCommonPrefix + "encode.mediaservicesv3.scheduled";

        /// <summary>
        /// response.encode.mediaservicesv3.processing
        /// </summary>
        public const string ResponseEncodeMediaServicesV3Processing = ResponseCommonPrefix + "encode.mediaservicesv3.processing";

        /// <summary>
        /// response.encode.mediaservicesv3.success
        /// </summary>
        public const string ResponseEncodeMediaServicesV3Success = ResponseCommonPrefix + "encode.mediaservicesv3.success";

        /// <summary>
        /// request.blob.tier.change
        /// </summary>
        public const string RequestBlobTierChange = RequestCommonPrefix + "blob.tier.change";

        /// <summary>
        /// response.blob.tier.success
        /// </summary>
        public const string ResponseBlobTierChanged = ResponseCommonPrefix + "blob.tier.success";

        /// <summary>
        /// request.blob.container.create
        /// </summary>
        public const string RequestBlobContainerCreate = RequestCommonPrefix + "blob.container.create";

        /// <summary>
        /// response.blob.container.create.success
        /// </summary>
        public const string ResponseBlobContainerSuccess = ResponseCommonPrefix + "blob.container.create.success";

        /// <summary>
        /// request.blob.container.delete
        /// </summary>
        public const string RequestBlobContainerDelete = RequestCommonPrefix + "blob.container.delete";

        /// <summary>
        /// response.blob.container.delete.success
        /// </summary>
        public const string ResponseBlobContainerDeleteSuccess = ResponseCommonPrefix + "blob.container.delete.success";

        /// <summary>
        /// request.blob.container.access.change
        /// </summary>
        public const string RequestBlobContainerAccessChange = RequestCommonPrefix + "blob.container.access.change";

        /// <summary>
        /// response.blob.container.access.change.success
        /// </summary>
        public const string ResponseBlobContainerAccessChangeSuccess = ResponseCommonPrefix + "blob.container.access.change.success";


        /// <summary>
        /// request.mediaservices.locator.create
        /// </summary>
        public const string RequestMediaservicesLocatorCreate = RequestCommonPrefix + "mediaservices.locator.create";

        /// <summary>
        /// response.mediaservices.locator.create.success
        /// </summary>
        public const string ResponseMediaservicesLocatorCreateSuccess = ResponseCommonPrefix + "mediaservices.locator.create.success";

        /// <summary>
        /// request.mediaservices.locator.delete
        /// </summary>
        public const string RequestMediaservicesLocatorDelete = RequestCommonPrefix + "mediaservices.locator.delete";

        /// <summary>
        /// response.mediaservices.locator.delete.success
        /// </summary>
        public const string ResponseMediaservicesLocatorDeleteSuccess = ResponseCommonPrefix + "mediaservices.locator.delete.success";

        /// <summary>
        /// request.switchkey.storage
        /// </summary>
        public const string RequestSwitchStorageKey = @RequestCommonPrefix + "switchkey.storage";

        /// <summary>
        /// response.switchkey.storage.success
        /// </summary>
        public const string ResponseStorageKeySwitched = @ResponseCommonPrefix + "switchkey.storage.success";

        /// <summary>
        /// request.rollkey.storage
        /// </summary>
        public const string RequestRollStorageKey = @RequestCommonPrefix + "rollkey.storage";

        /// <summary>
        /// response.rollkey.storage.success
        /// </summary>
        public const string ResponseStorageKeyRolled = @ResponseCommonPrefix + "rollkey.storage.success";
    }
}
