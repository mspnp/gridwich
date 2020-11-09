using System.Collections.Generic;
using Azure;
using Azure.Storage.Blobs;

namespace Gridwich.SagaParticipants.Storage.AzureStorageTests.Models
{
    /// <summary>
    /// Test class used to mock out values returned by the GetBlobsAsync operation of the
    /// <see cref="BlobContainerClient"/>. Needed because all Azure classes have private members.
    /// </summary>
    /// <typeparam name="T">The type of elements in the async pageable.</typeparam>
    public class TestAsyncPageable<T> : AsyncPageable<T>
    {
        private IReadOnlyList<T> Values { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestAsyncPageable{T}"/> class.
        /// </summary>
        /// <param name="values">The underlying list for this pageable.</param>
        public TestAsyncPageable(IReadOnlyList<T> values)
        {
            Values = values;
        }

        /// <inheritdoc/>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public override async IAsyncEnumerable<Page<T>> AsPages(string continuationToken = null, int? pageSizeHint = null)
        {
            yield return Page<T>.FromValues(Values, null, null);
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}