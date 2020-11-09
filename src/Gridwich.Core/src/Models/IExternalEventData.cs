using Newtonsoft.Json.Linq;

namespace Gridwich.Core.Models
{
    public interface IExternalEventData
    {
        JObject GetOperationContext();
    }
}