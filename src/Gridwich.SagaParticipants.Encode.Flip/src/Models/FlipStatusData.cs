using System.Diagnostics.CodeAnalysis;
using Gridwich.Core.Constants;
using Gridwich.Core.DTO;
using Gridwich.Core.Helpers;
using Gridwich.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.Flip.Models
{
#pragma warning disable SA1600 // Elements should be documented
    public class FlipStatusData : IFlipStatus, IExternalEventData
    {
        [JsonProperty("video_id", Required = Required.Always)]
        public string VideoId { get; set; }
        [JsonProperty("original_filename", Required = Required.Always)]
        public string OriginalFilename { get; set; }
        [JsonConverter(typeof(StringTypeConverter))]
        [JsonProperty("video_payload", Required = Required.Always)]
        public FlipPayload VideoPayload { get; set; }
        [JsonProperty("event", Required = Required.Always)]
        public string EventName { get; set; }
        [JsonProperty("service", Required = Required.Always)]
        public string ServiceName { get; set; }
        public virtual ResponseEncodeStatusBaseDTO ToGridwichEncodeData()
        {
            return null;
        }

        public JObject GetOperationContext()
        {
            return VideoPayload?.OperationContext;
        }
    }

    /// <summary>
    /// Interface used to define Flip status classes.
    /// </summary>
    public interface IFlipStatus
    {
        /// <summary>
        /// Converter that changes inbound Flip status data to Requestor format.
        /// </summary>
        /// <returns>A Gridwich ResponseEncodeStatusBaseDTO</returns>
        ResponseEncodeStatusBaseDTO ToGridwichEncodeData();
    }

    public class FlipVideoStatusData : FlipStatusData, IFlipStatus
    {
        [JsonProperty("encoding_ids", Required = Required.Always)]
        [property: SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "As per rule description, not applicable to Data transfer objects")]
        public string[] EncodingIds { get; set; }
        [JsonProperty("factory_id", Required = Required.Always)]
        public string FactoryId { get; set; }
    }

    public class FlipVideoCreatedData : FlipVideoStatusData
    {
        public override ResponseEncodeStatusBaseDTO ToGridwichEncodeData()
        {
            var encodeStatus = new ResponseEncodeScheduledDTO(CustomEventTypes.ResponseEncodeFlipScheduled)
            {
                OperationContext = GetOperationContext()
            };
            return encodeStatus;
        }
    }

    public class FlipVideoEncodedData : FlipVideoStatusData
    {
        public override ResponseEncodeStatusBaseDTO ToGridwichEncodeData()
        {
            var encodeStatus = new ResponseEncodeSuccessDTO(CustomEventTypes.ResponseEncodeFlipSuccess)
            {
                OperationContext = GetOperationContext()
            };
            return encodeStatus;
        }
    }
    public class FlipEncodingBaseData : FlipStatusData
    {
        [JsonProperty("encoding_id", Required = Required.Always)]
        public string EncodingId { get; set; }
        public override ResponseEncodeStatusBaseDTO ToGridwichEncodeData()
        {
            return base.ToGridwichEncodeData();
        }
    }

    public class FlipEncodingProgressData : FlipEncodingBaseData
    {
        [JsonProperty("progress", Required = Required.Always)]
        public int Progress { get; set; }
        [JsonProperty("disable_retry", Required = Required.Always)]
        public bool DisableRetry { get; set; }
        public override ResponseEncodeStatusBaseDTO ToGridwichEncodeData()
        {
            var encodeStatusProcessing = new ResponseEncodeProcessingDTO(CustomEventTypes.ResponseEncodeFlipProcessing)
            {
                CurrentStatus = "Processing",   // TODO: There seems to be no other changing status from Telestream.
                PercentComplete = Progress,
                OperationContext = GetOperationContext()
            };
            return encodeStatusProcessing;
        }
    }
    public class FlipEncodingCompleteData : FlipEncodingBaseData
    {
        [JsonProperty("encoding_status", Required = Required.Always)]
        public string EncodingStatus { get; set; }
        public override ResponseEncodeStatusBaseDTO ToGridwichEncodeData()
        {
            if (EncodingStatus == "success")
            {
                var encodeStatusSuccess = new ResponseEncodeSuccessDTO(CustomEventTypes.ResponseEncodeFlipSuccess)
                {
                    OperationContext = GetOperationContext(),
                    OutputContainer = VideoPayload.OutputContainer
                };
                return encodeStatusSuccess;
            }
            else
            {
                return new ResponseEncodeFailureDTO();
            }
        }
    }
#pragma warning restore SA1600 // Elements should be documented
}
