using System;
namespace Gridwich.SagaParticipants.Encode.MediaServicesV2.Presets
{
    /// <summary>
    /// Interface is used to implement the Media Services V2 Preset.
    /// </summary>
    public interface IMediaServicesPreset
    {
        /// <summary>
        /// Defines interface method for returning a preset payload by preset name.
        /// </summary>
        /// <param name="presetName">Preset name to get preset payload for.</param>
        /// <param name="timeCode">A timecode to use for creating Thumbnails.</param>
        /// <returns>A preset payload as a string.</returns>
        string GetPresetForPresetName(string presetName, TimeSpan? timeCode);
    }
}
