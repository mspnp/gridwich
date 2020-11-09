using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Azure.Storage.Blobs.Models;
using Gridwich.Core.Constants;
using Gridwich.Core.Interfaces;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;

namespace Gridwich.Core.Helpers
{
    /// <summary>
    /// Solution extensions
    /// </summary>
    public static class GridwichExtensions
    {
        /// <summary>
        /// Helper function to make Telestream Cloud Api easier.
        /// Many collection return nulls, this makes null checking easier on ForEach.
        /// </summary>
        /// <typeparam name="T">The type of object.</typeparam>
        /// <param name="source">Source object.</param>
        /// <returns>An empty Enumerable if the source was null.</returns>
        public static IEnumerable<T> OrEmptyIfNull<T>(this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }

        /// <summary>
        /// Decorates an EventGridEvent
        /// </summary>
        /// <param name="e">The e.</param>
        /// <param name="decorator">The decorator.</param>
        /// <returns>A new Event Grid event decorated with the metadata from <paramref name="decorator"/></returns>
        public static EventGridEvent DecorateWith(this EventGridEvent e, IEventDecorator decorator)
        {
            _ = e ?? throw new ArgumentNullException(nameof(e));

            _ = decorator ?? throw new ArgumentNullException(nameof(decorator));

            return decorator.Decorate(e);
        }

        /// <summary>
        /// Returns the provided BlobRehydratePriority if not null. <see cref="BlobRehydratePriority.Default"/> otherwise.
        /// </summary>
        /// <param name="priority">The priority to get.</param>
        /// <returns>A valid BlobRehydratePriority</returns>
        public static BlobRehydratePriority GetOrDefault(this BlobRehydratePriority? priority) => priority ?? StorageServiceConstants.DefaultRehydratePriority;

        /// <summary>
        /// Convert BlobRehydratePriority to Azure's RehydratePriority.
        /// </summary>
        /// <param name="priority">The priority to convert.</param>
        /// <returns>A valid RehydratePriority</returns>
        public static RehydratePriority ToAzureEnum(this BlobRehydratePriority priority)
        {
            return (RehydratePriority)(int)priority;
        }

        /// <summary>
        /// Convert BlobAccessTier to Azure's AccessTier.
        /// </summary>
        /// <param name="tier">The tier to convert.</param>
        /// <returns>A valid AccessTier</returns>
        public static AccessTier ToAzureEnum(this BlobAccessTier tier)
        {
            foreach (var pair in BlobAccessTier.ValidTiers)
            {
                if (pair.Key == tier)
                {
                    return pair.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Convert ContainerAccessType to Azure's PublicAccessType.
        /// </summary>
        /// <param name="accessType">The accessType to convert.</param>
        /// <returns>A valid PublicAccessType</returns>
        public static PublicAccessType ToAzureEnum(this ContainerAccessType accessType)
        {
            return (PublicAccessType)(int)accessType;
        }
    }
}