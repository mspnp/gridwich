using System;
using System.Collections.Generic;
using Gridwich.Core.Helpers;
namespace Gridwich.SagaParticipants.Encode.MediaServicesV2.Presets
{
    /// <summary>
    /// Concrete class that contains the implementation to return preset payloads for AMS V2.
    /// </summary>
    public class MediaServicesPreset : IMediaServicesPreset
    {
        /// <inheritdoc cref="IMediaServicesPreset"/>
        private const string ThumbnailStartToken = "THUMBNAILSETTING_START";
        private const string SpriteOnlySetting =
            "{'Codecs':[" +
            "{'SyncMode':'Auto','JpgLayers':[{'Quality':90,'Type':'JpgLayer','Width':'10%','Height':'10%'}],'SpriteColumn':10,'Start':'00:00:01','Step':'1%','Range':'100%','Type':'JpgImage'}" +
            "]," +
            "'Outputs':[" +
            "{'FileName':'{Basename}_{Index}_Sprite{Extension}','Format':{'Type':'JpgFormat'}}" +
            "],'Version':1.0}";
        private const string SpriteAndThumbnailSetting =
            "{'Codecs':[" +
            "{'SyncMode':'Auto','JpgLayers':[{'Quality':90,'Type':'JpgLayer','Width':'10%','Height':'10%'}],'SpriteColumn':10,'Start':'00:00:01','Step':'1%','Range':'100%','Type':'JpgImage'}," +
            "{'SyncMode':'Auto','PngLayers':[{'Type':'PngLayer','Width':'1920','Height':'960'}],'Start':'" + ThumbnailStartToken + "','Range':'1','Type':'PngImage'}" +
            "]," +
            "'Outputs':[" +
            "{'FileName':'{Basename}_{Index}_Sprite{Extension}','Format':{'Type':'JpgFormat'}}," +
            "{'FileName':'{Basename}_{Index}_Thumbnail{Extension}','Format':{'Type':'PngFormat'}}" +
            "],'Version':1.0}";
        private const string ThumbnailOnlySetting =
            "{'Codecs':[" +
            "{'SyncMode':'Auto','PngLayers':[{'Type':'PngLayer','Width':'1920','Height':'960'}],'Start':'" + ThumbnailStartToken + "','Range':'1','Type':'PngImage'}" +
            "]," +
            "'Outputs':[" +
            "{'FileName':'{Basename}_{Index}_Thumbnail{Extension}','Format':{'Type':'PngFormat'}}" +
            "],'Version':1.0}";

        private readonly Dictionary<string, string> presetDictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            { nameof(SpriteOnlySetting), SpriteOnlySetting },
            { nameof(SpriteAndThumbnailSetting), SpriteAndThumbnailSetting },
            { nameof(ThumbnailOnlySetting), ThumbnailOnlySetting }
        };

        /// <summary>
        /// Given a preset name, looks up its definition in the presetDictionary.
        /// </summary>
        /// <param name="presetName">The name of the preset to retrieve.</param>
        /// <param name="timeCodeTimeSpan">Timecode for thumbnail.</param>
        /// <returns>Returns the preset of the preset name.</returns>
        public string GetPresetForPresetName(string presetName, TimeSpan? timeCodeTimeSpan)
        {
            _ = presetName ?? throw new System.ArgumentException($@"{nameof(presetName)} is invalid", nameof(presetName));
            if (presetDictionary.TryGetValue(presetName, out string presetValue))
            {
                var replaceWith = timeCodeTimeSpan?.ToString() ?? "{Best}";
                presetValue = presetValue.Replace(ThumbnailStartToken, replaceWith, System.StringComparison.InvariantCulture);
                return presetValue;
            }
            return presetName;
        }
    }
}
