using System;
using Newtonsoft.Json;

namespace Gridwich.Core.Helpers
{
    /// <summary>
    /// A custom JsonConverter for properties that come as a string.
    /// </summary>
    public class StringTypeConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanRead => true;
        /// <inheritdoc/>
        public override bool CanWrite => true;

        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            _ = reader ?? throw new ArgumentNullException(nameof(reader));

            string json = (string)reader.Value;
            var result = JsonConvert.DeserializeObject(json, objectType);
            return result;
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            _ = serializer ?? throw new ArgumentNullException(nameof(serializer));

            var json = JsonConvert.SerializeObject(value);
            serializer.Serialize(writer, json);
        }
    }
}