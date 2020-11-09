using System.Collections.Generic;
using Gridwich.Core.DTO;
using Gridwich.SagaParticipants.Encode;
using Newtonsoft.Json.Linq;

namespace Gridwich.SagaParticipants.Encode.MediaServicesV3Tests
{
    public static class MediaServicesV3TestData
    {
        // Good Reference Data

        public static string GoodWorkflowJobName => "expectedJobId";
        public static string GoodInputAccountName => "gridwichtestin01sasb";
        public static string GoodInputContainerName => "input1";
        public static string GoodInputFileName => "bbb.mp4";
        public static string GoodInputsInputItem => $"https://{GoodInputAccountName}.blob.core.windows.net/{GoodInputContainerName}/{GoodInputFileName}";
        public static string GoodOutputAccountName => "gridwichtestout01sasb";
        public static string GoodOutputContainerName => "output1";
        public static string GoodOutputContainer => $"https://{GoodOutputAccountName}.blob.core.windows.net/{GoodOutputContainerName}/";
        public static string GoodTransformName => "AdaptiveStreaming";
        public static JObject GoodOperationContext => new JObject()
        {
            new JProperty("expectedKey", "expectedValue"),
            new JProperty("expectedId", 42),
        };
        public static ServiceOperationResultEncodeDispatched ServiceOperationResultEncodeDispatched_Is_Expected => new ServiceOperationResultEncodeDispatched(
            workflowJobName: GoodWorkflowJobName,
            encoderContext: null,
            GoodOperationContext);
        public static RequestMediaServicesV3EncodeCreateDTO RequestMediaServicesV3EncodeCreateDTO_Is_Expected => new RequestMediaServicesV3EncodeCreateDTO()
        {
            Inputs = new List<InputItem>()
            {
                new InputItem() { BlobUri = GoodInputsInputItem },
            },
            OutputContainer = GoodOutputContainer,
            TransformName = GoodTransformName,
            OperationContext = GoodOperationContext,
        };

        // Inputs:

        public static RequestMediaServicesV3EncodeCreateDTO InputsUri_Is_NotAUri1 => new RequestMediaServicesV3EncodeCreateDTO()
        {
            Inputs = new List<InputItem>()
            {
                new InputItem() { BlobUri = "this is not a Uri" },
            },
            OutputContainer = GoodOutputContainer,
            TransformName = GoodTransformName,
            OperationContext = GoodOperationContext,
        };
        public static RequestMediaServicesV3EncodeCreateDTO InputsUri_Is_NotAUri2 => new RequestMediaServicesV3EncodeCreateDTO()
        {
            Inputs = new List<InputItem>()
            {
                new InputItem() { BlobUri = GoodInputsInputItem },
                new InputItem() { BlobUri = "this is not a Uri" },
            },
            OutputContainer = GoodOutputContainer,
            TransformName = GoodTransformName,
            OperationContext = GoodOperationContext,
        };
        public static RequestMediaServicesV3EncodeCreateDTO InputsUri_Is_Null => new RequestMediaServicesV3EncodeCreateDTO()
        {
            Inputs = new List<InputItem>()
            {
                null,
            },
            OutputContainer = GoodOutputContainer,
            TransformName = GoodTransformName,
            OperationContext = GoodOperationContext,
        };
        public static RequestMediaServicesV3EncodeCreateDTO Inputs_Is_Empty => new RequestMediaServicesV3EncodeCreateDTO()
        {
            Inputs = new List<InputItem>()
            {
                // No Uris
            },
            OutputContainer = GoodOutputContainer,
            TransformName = GoodTransformName,
            OperationContext = GoodOperationContext,
        };
        public static RequestMediaServicesV3EncodeCreateDTO Inputs_Is_Null => new RequestMediaServicesV3EncodeCreateDTO()
        {
            Inputs = null,
            OutputContainer = GoodOutputContainer,
            TransformName = GoodTransformName,
            OperationContext = GoodOperationContext,
        };

        // OutputContainer

        public static RequestMediaServicesV3EncodeCreateDTO OutputContainer_Is_IsNotUri => new RequestMediaServicesV3EncodeCreateDTO()
        {
            Inputs = new List<InputItem>()
            {
                new InputItem() { BlobUri = GoodInputsInputItem },
            },
            OutputContainer = "this is not a uri",
            TransformName = GoodTransformName,
            OperationContext = GoodOperationContext,
        };
        public static string UnexpectedOutputAccountName => "unexpectedaccount";
        public static RequestMediaServicesV3EncodeCreateDTO OutputContainer_Is_UnexpectedAccount => new RequestMediaServicesV3EncodeCreateDTO()
        {
            Inputs = new List<InputItem>()
            {
                new InputItem() { BlobUri = GoodInputsInputItem },
            },
            OutputContainer = $"https://{UnexpectedOutputAccountName}.blob.core.windows.net/{GoodOutputContainerName}/",
            TransformName = GoodTransformName,
            OperationContext = GoodOperationContext,
        };
        public static RequestMediaServicesV3EncodeCreateDTO OutputContainer_Is_StringEmpty => new RequestMediaServicesV3EncodeCreateDTO()
        {
            Inputs = new List<InputItem>()
            {
                new InputItem() { BlobUri = GoodInputsInputItem },
            },
            OutputContainer = string.Empty,
            TransformName = GoodTransformName,
            OperationContext = GoodOperationContext,
        };
        public static RequestMediaServicesV3EncodeCreateDTO OutputContainer_Is_Whitespace => new RequestMediaServicesV3EncodeCreateDTO()
        {
            Inputs = new List<InputItem>()
            {
                new InputItem() { BlobUri = GoodInputsInputItem },
            },
            OutputContainer = "       ",
            TransformName = GoodTransformName,
            OperationContext = GoodOperationContext,
        };
        public static RequestMediaServicesV3EncodeCreateDTO OutputContainer_Is_Null => new RequestMediaServicesV3EncodeCreateDTO()
        {
            Inputs = new List<InputItem>()
            {
                new InputItem() { BlobUri = GoodInputsInputItem },
            },
            OutputContainer = null,
            TransformName = GoodTransformName,
            OperationContext = GoodOperationContext,
        };

        // TransformName

        public static RequestMediaServicesV3EncodeCreateDTO TransformName_Is_Unexpected => new RequestMediaServicesV3EncodeCreateDTO()
        {
            Inputs = new List<InputItem>()
            {
                new InputItem() { BlobUri = GoodInputsInputItem },
            },
            OutputContainer = GoodOutputContainer,
            TransformName = "unexpectedTransformName",
            OperationContext = GoodOperationContext,
        };
        public static RequestMediaServicesV3EncodeCreateDTO TransformName_Is_StringEmpty => new RequestMediaServicesV3EncodeCreateDTO()
        {
            Inputs = new List<InputItem>()
            {
                new InputItem() { BlobUri = GoodInputsInputItem },
            },
            OutputContainer = GoodOutputContainer,
            TransformName = string.Empty,
            OperationContext = GoodOperationContext,
        };
        public static RequestMediaServicesV3EncodeCreateDTO TransformName_Is_Null => new RequestMediaServicesV3EncodeCreateDTO()
        {
            Inputs = new List<InputItem>()
            {
                new InputItem() { BlobUri = GoodInputsInputItem },
            },
            OutputContainer = GoodOutputContainer,
            TransformName = null,
            OperationContext = GoodOperationContext,
        };
        public static RequestMediaServicesV3EncodeCreateDTO TransformName_Is_Whitespace => new RequestMediaServicesV3EncodeCreateDTO()
        {
            Inputs = new List<InputItem>()
            {
                new InputItem() { BlobUri = GoodInputsInputItem },
            },
            OutputContainer = GoodOutputContainer,
            TransformName = "     ",
            OperationContext = GoodOperationContext,
        };
        public static RequestMediaServicesV3EncodeCreateDTO OperationContext_Is_Null => new RequestMediaServicesV3EncodeCreateDTO()
        {
            Inputs = new List<InputItem>()
            {
                new InputItem() { BlobUri = GoodInputsInputItem },
            },
            OutputContainer = GoodOutputContainer,
            TransformName = GoodTransformName,
            OperationContext = null,
        };
    }
}
