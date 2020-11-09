using System;
using Gridwich.Core.Constants;
using Gridwich.Core.Exceptions;

namespace Gridwich.Services.Core.Exceptions
{
    /// <summary>
    /// Exception for <see cref="GridwichPublicationListPathsException"/>.
    /// </summary>
    public class GridwichTimeParameterException : GridwichException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichTimeParameterException"/> class.
        /// </summary>
        /// <param name="paramName">The invalid paramName.</param>
        /// <param name="seconds">The value in seconds which was out of range.</param>
        /// <param name="message">The base exception message you want to set.</param>
        /// <param name="innerException">The base exception innerException.</param>
        public GridwichTimeParameterException(string paramName, double seconds, string message, Exception innerException)
         : base(message, LogEventIds.TimeParameterOutOfRange, null, innerException)
        {
            SafeAddToData("paramName", paramName);
            SafeAddToData("seconds", seconds);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridwichTimeParameterException"/> class.
        /// </summary>
        /// <param name="paramName">The invalid paramName.</param>
        /// <param name="seconds">The value in seconds which was out of range.</param>
        /// <param name="message">The base exception message you want to set.</param>
        public GridwichTimeParameterException(string paramName, double seconds, string message)
         : base(message, LogEventIds.TimeParameterOutOfRange, null)
        {
            SafeAddToData("paramName", paramName);
            SafeAddToData("seconds", seconds);
        }
    }
}
