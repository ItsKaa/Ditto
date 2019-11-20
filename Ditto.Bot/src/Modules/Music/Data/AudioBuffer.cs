using Ditto.Extensions;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Music.Data
{
    /// <summary>
    /// Audio buffering system that handles the input and output streams.
    /// </summary>
    public class AudioBuffer : IDisposable
    {
        private Task _readTask, _writeTask;
        private CancellationTokenSource _cancellationTokenSource;

        private bool ReadFinished { get; set; }
        public bool Running { get; private set; }
        public Stream InStream { get; private set; }
        public Stream OutStream { get; private set; }
        /// <summary>
        /// The queue that holds the buffered chunks from the input stream and written to the output stream.
        /// </summary>
        private ConcurrentQueue<AudioChunk> Queue { get; set; }
        /// <summary>
        /// The amount of chunks (in bytes) the buffer reads from the input stream.
        /// </summary>
        public ushort BufferReadSize { get; set; }
        /// <summary>
        /// The amount of allowed buffered chunks (in megabytes) stored while writing to the output stream.
        /// </summary>
        public uint BufferLimit { get; set; }
        /// <summary>
        /// The function that is invoked right before writing an audio buffer to the output stream.
        /// </summary>
        public Func<byte[], byte[]> ProcessBuffer;

        public AudioBuffer(Stream inStream, Stream outStream, ushort bufferSize = 1024, uint bufferLimit = 50)
        {
            InStream = inStream;
            OutStream = outStream;
            Running = false;
            Queue = new ConcurrentQueue<AudioChunk>();

            BufferReadSize = bufferSize > 0 ? bufferSize : (ushort)1.KiB();
            BufferLimit = bufferLimit > 0 ? bufferLimit : (uint)50.MiB();
        }

        public void Start()
        {
            Stop();
            Running = true;
            _cancellationTokenSource = new CancellationTokenSource();

            ReadFinished = false;
            _readTask = Task.Run(async () =>
            {
                while (Running && _cancellationTokenSource?.IsCancellationRequested == false)
                {
                    try
                    {
                        if (InStream.CanRead)
                        {
                            AudioChunk chunk = new AudioChunk(BufferReadSize);

                            int bytesRead = await InStream.ReadAsync(chunk.Memory, _cancellationTokenSource.Token).ConfigureAwait(false);
                            if (bytesRead > 0)
                            {
                                chunk.Length = bytesRead;

                                // Wait for the dequeue, comparing in megabytes.
                                while (Running && (Queue.Count * (BufferReadSize * 0.000001)) > BufferLimit)
                                {
                                    await Task.Delay(100, _cancellationTokenSource?.Token ?? CancellationToken.None).ConfigureAwait(false);
                                }
                                Queue.Enqueue(chunk);
                            }
                            else
                            {
                                Log.Debug("AudioBuffer: Read EOF");
                                break;
                            }
                        }
                        else
                        {
                            Log.Error("AudioBuffer: Could not read InStream.");
                            await Task.Delay(25).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"AudioBuffer | {ex}");
                    }
                }

                ReadFinished = true;

            }, _cancellationTokenSource.Token);

            _writeTask = Task.Run(async () =>
            {
                while (Running && _cancellationTokenSource?.IsCancellationRequested == false)
                {
                    if (ReadFinished && Queue.IsEmpty)
                        break;

                    try
                    {
                        if (Queue.TryDequeue(out var audioChunk))
                        {
                            using (audioChunk)
                            {
                                if (OutStream.CanWrite)
                                {
                                    // We're required to process the chunks in the write task, it's far slower which allows us to change the volume while it's still playing.
                                    if (ProcessBuffer != null)
                                    {
                                        var bytes = audioChunk.Memory;
                                        if (audioChunk.Length != audioChunk.Memory.Length)
                                        {
                                            bytes = audioChunk.Memory.Part(0, audioChunk.Length, false).ToArray();
                                        }
                                        var processedBytes = ProcessBuffer?.Invoke(bytes);
                                        processedBytes.CopyTo(audioChunk.Memory, 0);
                                        audioChunk.Length = processedBytes.Length;
                                    }

                                    await OutStream.WriteAsync(audioChunk.Memory, 0, audioChunk.Length, _cancellationTokenSource.Token);
                                }
                                else
                                {
                                    Log.Error("AudioBuffer: Could not write to OutStream.");
                                    await Task.Delay(25).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"AudioBuffer | {ex.Message}");
                        Log.Debug($"AudioBuffer | {ex}");
                        await Task.Delay(25).ConfigureAwait(false);
                    }
                }
            }, _cancellationTokenSource.Token);
        }

        public void Stop()
        {
            try { _cancellationTokenSource?.Cancel(); } catch { }
            Running = false;
            ReadFinished = true;
            Queue.Clear();
        }

        public async Task WaitAsync()
        {
            await _readTask.ConfigureAwait(false);
            await _writeTask.ConfigureAwait(false);
        }

        private bool _disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Stop();
                }
                _disposed = true;
            }
        }
    }
}