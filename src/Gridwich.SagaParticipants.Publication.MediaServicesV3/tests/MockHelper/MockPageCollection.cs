using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Rest.Azure;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3Tests.MockHelper
{
    /// <summary>
    /// Class to mock IPage.
    /// </summary>
    /// <typeparam name="T">Object.</typeparam>
    [ExcludeFromCodeCoverage]
    public class MockPageCollection<T> : IPage<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockPageCollection{T}"/> class.
        /// </summary>
        /// <param name="items">Items.</param>
        public MockPageCollection(IList<T> items)
        {
            Items = items;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockPageCollection{T}"/> class.
        /// </summary>
        /// <param name="items">Items.</param>
        /// <param name="nextPageLink">Next page link.</param>
        public MockPageCollection(IList<T> items, string nextPageLink)
        {
            Items = items;
            NextPageLink = nextPageLink;
        }

        /// <summary>
        /// Gets the link to the next page.
        /// </summary>
        public string NextPageLink { get; private set; }

        /// <summary>
        /// Gets list of Items.
        /// </summary>
        public IList<T> Items { get; }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>A an enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return Items == null ? Enumerable.Empty<T>().GetEnumerator() : Items.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>A an enumerator that can be used to iterate through the collection.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
