using System;

namespace Gridwich.SagaParticipants.Analysis.MediaInfo.Services
{
    /// <summary>
    /// MediaInfoService library interface. Mainly used for mocks.
    /// </summary>
    public interface IMediaInfoService : IDisposable
    {
        /// <summary>Opens the buffer initialize.</summary>
        /// <param name="fileSize">Size of the file.</param>
        /// <param name="fileOffset">The file offset.</param>
        /// <returns>The pointer to the buffer.</returns>
        public IntPtr OpenBufferInit(long fileSize, long fileOffset);

        /// <summary>Opens the buffer continue.</summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <returns>The pointer to the buffer.</returns>
        public IntPtr OpenBufferContinue(IntPtr buffer, IntPtr bufferSize);

        /// <summary>Opens the buffer continue go to get.</summary>
        /// <returns>The pointer to the buffer.</returns>
        public long OpenBufferContinueGoToGet();

        /// <summary>Opens the buffer finalize.</summary>
        /// <returns>The pointer to the buffer.</returns>
        public IntPtr OpenBufferFinalize();

        /// <summary>Gets options value by the specified option name.</summary>
        /// <param name="optionName">The option.</param>
        /// <returns>The value of the option</returns>
        public string GetOption(string optionName);

        /// <summary>Sets value to specified option name.</summary>
        /// <param name="optionName">The option name.</param>
        /// <param name="value">The option value.</param>
        /// <returns>The value for the option.</returns>
        public string GetOption(string optionName, string value);

        /// <summary>Informs media stream data.</summary>
        /// <returns>The value for the option.</returns>
        public string GetInform();
    }
}