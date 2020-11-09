using System;
using System.Collections.Generic;
using System.Text;

namespace Gridwich.SagaParticipants.Publication.MediaServicesV3.Models
{
    /// <summary>
    /// Class to manage track index and name for the creation of the asset filters.
    /// </summary>
    public class TrackInfo
    {
        /// <summary>
        /// Gets or sets the ID of the track.
        /// </summary>
        public int TrackID { get; set; }

        /// <summary>
        /// Gets or sets the track name.
        /// </summary>
        public string TrackName { get; set; }

        /// <summary>
        /// Gets or sets the track type.
        /// Expected values: Audio, Video
        /// </summary>
        public string TrackType { get; set; }
    }
}
