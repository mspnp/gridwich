using Gridwich.Core.Constants;
using Newtonsoft.Json;
using System;

namespace Gridwich.Core.Helpers
{
    /// <summary>
    /// A custom JsonConverter for the <see cref="BlobAccessTier"/>. Without this, values don't get correctly serialized.
    /// </summary>
    public class AccessTierConverter : JsonConverter<BlobAccessTier>
    {
        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, BlobAccessTier value, JsonSerializer serializer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            _ = value ?? throw new ArgumentNullException(nameof(value));
            _ = serializer ?? throw new ArgumentNullException(nameof(serializer));

            serializer.Serialize(writer, value.ToString());
        }

        /// <inheritdoc/>
        public override BlobAccessTier ReadJson(JsonReader reader, Type objectType, BlobAccessTier existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            _ = reader ?? throw new ArgumentNullException(nameof(reader));
            _ = objectType ?? throw new ArgumentNullException(nameof(objectType));
            _ = serializer ?? throw new ArgumentNullException(nameof(serializer));


            if (!(serializer.Deserialize(reader) is string deserialize))
            {
                throw new JsonSerializationException($"{nameof(BlobAccessTier)} value cannot be null.");
            }

            return BlobAccessTier.Lookup(deserialize);
        }
    }
}