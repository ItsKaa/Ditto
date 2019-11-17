using Ditto.Extensions;
using System;
using System.Collections.Concurrent;
using System.IO;
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

        public bool Running { get; private set; }
        public Stream InStream { get; private set; }
        public Stream OutStream { get; private set; }
        /// <summary>
        /// The queue that holds the buffered chunks from the input stream and written to the output stream.
        /// </summary>
        private ConcurrentQueue<Memory<byte>> Queue { get; set; }
        /// <summary>
        /// The amount of chunks (in bytes) the buffer reads from the input stream.
        /// Defaults to 1024.
        /// </summary>
        public ushort BufferReadSize { get; set; }
        /// <summary>
        /// The amount of allowed buffered chunks (in bytes) stored while writing to the output stream.
        /// Defaults to 50 MiB
        /// </summary>
        public uint BufferLimit { get; set; }
        /// <summary>
        /// The function that is invoked right before writing an audio buffer to the output stream.
        /// </summary>
        public Func<Memory<byte>, Memory<byte>> ProcessBuffer;

        public AudioBuffer(Stream inStream, Stream outStream, ushort bufferSize = 0, ushort bufferLimit = 0)
        {
            InStream = inStream;
            OutStream = outStream;
            Running = false;
            Queue = new ConcurrentQueue<Memory<byte>>();

            BufferReadSize = bufferSize > 0 ? bufferSize : (ushort)1.KiB();
            BufferLimit = bufferLimit > 0 ? bufferLimit : (uint)50.MiB();
        }

        public void Start(CancellationToken cancellationToken)
        {
            Stop();
            Running = true;
            if (cancellationToken == CancellationToken.None)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                cancellationToken = _cancellationTokenSource.Token;
            }

            _readTask = Task.Run(async () =>
            {
                while (Running)
                {
                    try
                    {
                        if (InStream.CanRead)
                        {
                            Memory<byte> buffer = new Memory<byte>(new byte[BufferReadSize]);
                            int bytesRead = await InStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                            if (bytesRead > 0)
                            {
                                buffer = buffer.Slice(0, bytesRead);
                                while (Running && (Queue.Count * BufferReadSize) > BufferLimit)
                                {
                                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                                }
                                Queue.Enqueue(buffer);
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
            }, cancellationToken);

            _writeTask = Task.Run(async () =>
            {
                while (Running)
                {
                    try
                    {
                        if (Queue.TryDequeue(out var buffer))
                        {
                            if (OutStream.CanWrite)
                            {
                                // We're required to process the chunks in the write task, it's far slower which allows us to change the volume while it's still playing.
                                if (ProcessBuffer != null)
                                {
                                    buffer = ProcessBuffer.Invoke(buffer);
                                }

                                await OutStream.WriteAsync(buffer, cancellationToken);
                            }
                            else
                            {
                                Log.Error("AudioBuffer: Could not write to OutStream.");
                                await Task.Delay(25).ConfigureAwait(false);
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
            }, cancellationToken);
        }

        public void Stop()
        {
            Running = false;
            _cancellationTokenSource?.Cancel();
            Queue.Clear();
        }

        public async Task WaitAsync()
        {
            await _readTask.ConfigureAwait(false);
            await _writeTask.ConfigureAwait(false);
        }

        private bool _disposed = true;
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
                    try
                    {
                        _readTask?.Dispose();
                        _writeTask?.Dispose();
                    }
                    catch { }
                }
                _disposed = true;
            }
        }
    }
}