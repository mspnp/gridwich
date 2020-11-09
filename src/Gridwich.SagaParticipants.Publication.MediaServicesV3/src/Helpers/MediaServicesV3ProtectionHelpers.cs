using System;
using System.Globalization;
using Microsoft.Azure.Management.Media.Models;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.Helpers
{
    /// <summary>
    /// Class used to contain basic helper methods for Media Services V3
    /// </summary>
    public static class MediaServicesV3ProtectionHelpers
    {
        /// <summary>
        /// Method used to validate a parameter.  This should be moved to Gridwich.Core.
        /// </summary>
        /// <param name="paramValue">Parameter value.</param>
        /// <param name="paramName">Name of parameter.</param>
        public static void CheckArgumentNotNullOrEmpty(string paramValue, string paramName)
        {
            if (paramValue == null)
            {
                throw new ArgumentNullException(paramName);
            }
            else if (string.IsNullOrWhiteSpace(paramValue))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0} parameter is empty or white space.", paramName));
            }
        }
    }
}