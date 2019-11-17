using Discord.Audio;
using Ditto.Data;
using Ditto.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Music.Data
{
    public class AudioStreamer : BaseClass, IDisposable
    {
        private ProcessStartInfo YoutubeDLStartInfo { get; set; }
        private ProcessStartInfo FFmpegStartInfo { get; set; }
        private Process YoutubeDLProcess { get; set; }
        private Process FFmpegProcess { get; set; }

        private CancellationTokenSource CancellationTokenSource { get; set; }
        public bool Running { get; private set; } = false;
        public bool Paused { get; private set; } = false;

        public const double MaxVolume = 125;
        public const double MinVolume = 0.1;
        private double _volume = 100.0;
        private bool _disposed = false;
        public TimeSpan TimeElapsed { get; private set; } = TimeSpan.Zero;

        public double Volume
        {
            get => _volume;
            set
            {
                if (double.IsNaN(value))
                    value = MinVolume;
                if(value < MinVolume)
                {
                    value = MinVolume;
                }
                if (value > MaxVolume)
                    value = MaxVolume;
                _volume = value;
            }
        }

        public ProcessStartInfo CopyStartInfo(ProcessStartInfo original)
        {
            var startInfo = new ProcessStartInfo()
            {
                Arguments = original.Arguments,
                CreateNoWindow = original.CreateNoWindow,
                //Environment = original.Environment,
                //EnvironmentVariables = original.EnvironmentVariables,
                ErrorDialog = original.ErrorDialog,
                ErrorDialogParentHandle = original.ErrorDialogParentHandle,
                FileName = original.FileName,
                RedirectStandardError = original.RedirectStandardError,
                RedirectStandardInput = original.RedirectStandardInput,
                RedirectStandardOutput = original.RedirectStandardOutput,
                StandardErrorEncoding = original.StandardErrorEncoding,
                StandardOutputEncoding = original.StandardOutputEncoding,
                //UserName = original.UserName,
                UseShellExecute = original.UseShellExecute,
                Verb = original.Verb,
                //Verbs = original.Verbs,
                WindowStyle = original.WindowStyle,
                WorkingDirectory = original.WorkingDirectory,
            };
            if(IsWindows())
            {
                startInfo.UserName = original.UserName;
                startInfo.Password = original.Password;
                startInfo.PasswordInClearText = original.PasswordInClearText;
                startInfo.LoadUserProfile = original.LoadUserProfile;
                startInfo.Domain = original.Domain;
            }
            return startInfo;
        }

        public AudioStreamer(string youtubeDlPath, string ffmpegPath, string youtubeDlArgs, string ffmpegArgs)
        {
            CancellationTokenSource = new CancellationTokenSource();
            YoutubeDLStartInfo = new ProcessStartInfo()
            {
                FileName = youtubeDlPath,
                Arguments = youtubeDlArgs + " -o -",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            FFmpegStartInfo = new ProcessStartInfo()
            {
                FileName = ffmpegPath,
                Arguments = "-i - " + ffmpegArgs + " pipe:1",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
        }

        public async Task StreamAsync(string url, AudioOutStream audioOutStream)
        {
            if(Running)
            {
                CancellationTokenSource.Cancel();
            }
            if (CancellationTokenSource.IsCancellationRequested)
            {
                CancellationTokenSource = new CancellationTokenSource();
            }
            Running = true;

            KillAndDisposeProcesses();
            YoutubeDLProcess = new Process()
            {
                StartInfo = CopyStartInfo(YoutubeDLStartInfo)
            };

#if DEBUG
            YoutubeDLProcess.ErrorDataReceived += (o, e) =>
            {
                Log.Error(e.Data);
            };
#endif

            YoutubeDLProcess.StartInfo.Arguments += $" {url}";
            YoutubeDLProcess.Start();
            YoutubeDLProcess.BeginErrorReadLine();
            TimeElapsed = TimeSpan.Zero;
            
            FFmpegProcess = new Process()
            {
                StartInfo = CopyStartInfo(FFmpegStartInfo)
            };
            FFmpegProcess.ErrorDataReceived += (o, e) =>
            {
                var _ = Task.Run(() =>
                {
                    if (e.Data != null)
                    {
                        var match = Globals.RegularExpression.FFmpegErrorData.Match(e.Data);
                        if (match.Success)
                        {
                            string[] tsArr = match.Groups["time"].Value.Split(new char[] { ':', '.' });
                            TimeElapsed = new TimeSpan(0, Convert.ToInt32(tsArr[0]), Convert.ToInt32(tsArr[1]), Convert.ToInt32(tsArr[2]), Convert.ToInt32(tsArr[3]) * 10);
                        }
#if DEBUG
                        else
                        {
                            Log.Error(e.Data);
                        }
#endif
                    };
                });
            };
            FFmpegProcess.Start();
            FFmpegProcess.BeginErrorReadLine();

            // Write YoutubeDL-Out to FFmpeg-In
            var mapStreamsTask = RedirectStream(
                YoutubeDLProcess.StandardOutput.BaseStream,
                FFmpegProcess.StandardInput.BaseStream
            );

            // Write FFmpeg-Out to Audio-Out using a buffer.
            using (var audioBuffer = new AudioBuffer(
                FFmpegProcess.StandardOutput.BaseStream,
                audioOutStream
            ))
            {
                audioBuffer.ProcessBuffer += (inBuffer) =>
                {
                    //return new Memory<byte>(AdjustVolume(inBuffer.Value.Span.ToArray(), Volume));
                    return AdjustVolume(inBuffer, this.Volume / 100.0);
                };

                audioBuffer.Start(CancellationTokenSource.Token);
                await audioBuffer.WaitAsync().ConfigureAwait(false);
            }

            KillAndDisposeProcesses();
            await mapStreamsTask.ConfigureAwait(false);
            Running = false;
        }

        public void Pause(bool pause)
        {
            Paused = pause;
        }

        
        private Task RedirectStream(Stream inStream, Stream outStream, int bufferSize = 1024, Func<int, byte[], Task<byte[]>> adjustBuffer = null)
        {
            return Task.Run(async () =>
            {
                using (inStream)
                using (outStream)
                {
                    var buffer = new byte[bufferSize];
                    while (Running && !CancellationTokenSource.IsCancellationRequested)
                    {
                        while (Paused)
                        {
                            await Task.Delay(100).ConfigureAwait(false);
                        }

                        try
                        {
                            if (inStream.CanRead)
                            {
                                var readCount = await inStream.ReadAsync(buffer, 0, buffer.Length, CancellationTokenSource.Token).ConfigureAwait(false);
                                if (readCount > 0)
                                {
                                    if (outStream.CanWrite)
                                    {
                                        if (adjustBuffer != null)
                                        {
                                            buffer = await adjustBuffer(readCount, buffer).ConfigureAwait(false);
                                        }
                                        await outStream.WriteAsync(buffer, 0, readCount, CancellationTokenSource.Token).ConfigureAwait(false);
                                    }
                                }
                                else
                                {
                                    // EOF reached
                                    break;
                                }
                            }
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception ex)
                        {
                            if (CancellationTokenSource?.IsCancellationRequested == false)
                            {
                                //Log.Error(ex);
                                throw ex;
                            }
                        }
                    }
                }
            }, CancellationTokenSource.Token);
        }
        private Task RedirectStream(Stream inStream, Stream outStream, Func<int, byte[], Task<byte[]>> adjustBuffer)
            => RedirectStream(inStream, outStream, 1024, adjustBuffer);

        public void StopStreaming()
        {
            try { CancellationTokenSource?.Cancel(); } catch { }
        }

        private static unsafe byte[] AdjustVolume(byte[] audioSamples, double volume)
        {
            if (Math.Abs(volume - 1.0) < 0.0001)
                return audioSamples;

            // 16-bit precision
            var volumeMultiplier = (int)Math.Round(volume * ushort.MaxValue);
            fixed (byte* srcBytes = audioSamples)
            {
                var src = (short*)srcBytes;
                for (var i = 0; i < audioSamples.Length/2; i++, src++)
                {
                    *src = (short)(((*src) * volumeMultiplier) >> 16);
                }
            }
            return audioSamples;
        }
        
        private static unsafe Memory<byte> AdjustVolume(Memory<byte> audioSamplesMemory, double volume)
        {
            if (Math.Abs(volume - 1.0) < 0.0001)
                return audioSamplesMemory;

            // 16-bit precision
            var volumeMultiplier = (int)Math.Round(volume * ushort.MaxValue);
            using (var pin = audioSamplesMemory.Pin())
            {
                byte* srcBytes = (byte*)pin.Pointer;
                var src = (short*)srcBytes;
                for (var i = 0; i < audioSamplesMemory.Length / 2; i++, src++)
                {
                    *src = (short)(((*src) * volumeMultiplier) >> 16);
                }
            }
            return audioSamplesMemory;
        }

        private void KillAndDisposeProcesses()
        {
            try
            {
                FFmpegProcess?.Kill();
                FFmpegProcess?.Dispose();
            }
            catch { }
            try
            {
                YoutubeDLProcess?.Kill();
                YoutubeDLProcess?.Dispose();
            }
            catch { }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    StopStreaming();
                    KillAndDisposeProcesses();
                }
                _disposed = true;
            }
        }
    }
}
