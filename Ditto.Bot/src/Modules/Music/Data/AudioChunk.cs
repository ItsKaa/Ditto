using System;
using System.Buffers;

namespace Ditto.Bot.Modules.Music.Data
{
    /// <summary>
    /// The internal audio buffer data class.
    /// </summary>
    public class AudioChunk : IDisposable
    {
        private bool _disposed = false;
        public byte[] Memory { get; private set; }
        public int Length { get; set; } = -1;

        internal AudioChunk(uint minLength)
        {
            Memory = ArrayPool<byte>.Shared.Rent((int)minLength);
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                ArrayPool<byte>.Shared.Return(Memory);
                Memory = null;
            }
            _disposed = true;
        }
    }
}