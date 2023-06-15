using Ditto.Data;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Bot
{
    partial class Program : BaseClass
    {
        private interface IOSProgram : IDisposable
        {
        }

        private class LinuxProgram : IOSProgram
        {
            public LinuxProgram()
            {
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    TryExitAsync().Wait();
                    Thread.Sleep(1000);
                    Environment.Exit(0);
                };
            }

            public void Dispose()
            {
            }
        }

        private class Win32Program : IOSProgram
        {
            public Win32Program()
            {
                SetConsoleCtrlHandler((type) =>
                {
                    if (type != CtrlTypes.CTRL_BREAK_EVENT)
                    {
                        TryExitAsync().GetAwaiter().GetResult();
                        Thread.Sleep(1000);
                    }
                    return true;
                }, true);
            }
            
            public void Dispose()
            {
            }

            [DllImport("kernel32.dll")]
            private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);

            // Delegate type to be used as the Handler Routine for SCCH
            private delegate Boolean ConsoleCtrlDelegate(CtrlTypes CtrlType);

            // Enumerated type for the control messages sent to the handler routine
            private enum CtrlTypes : uint
            {
                CTRL_C_EVENT = 0,
                CTRL_BREAK_EVENT,
                CTRL_CLOSE_EVENT,
                CTRL_LOGOFF_EVENT = 5,
                CTRL_SHUTDOWN_EVENT
            }
        }
    }
    

    partial class Program
    {
        private static bool _running = true;
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        public static Func<Task> Exit;
        
        private static async Task TryExitAsync()
        {
            if (_running)
            {
                _running = false;
                await Exit?.Invoke();
            }
        }

        static void Main(string[] args)
        {
            IOSProgram osProgram;
            if(IsWindows())
            {
                osProgram = new Win32Program();
            }
            else
            {
                osProgram = new LinuxProgram();
            }

            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                TryExitAsync().Wait();
                Thread.Sleep(1000);
            };

            try
            {
                new Ditto().RunAndBlockAsync(_cancellationTokenSource.Token).GetAwaiter().GetResult();
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
        }

        public static void Close()
        {
            try { _cancellationTokenSource.Cancel(); } catch { }
        }
    }
}