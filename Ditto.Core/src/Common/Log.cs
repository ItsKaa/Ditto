using NLog;
using NLog.Conditions;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ditto
{

    /// <summary>
    /// The purpose of this type is to act as a guard between 
    /// the actual parameter list and optional parameter list.
    /// If you need to pass this type as an argument you are using
    /// the wrong overload.
    /// </summary>
    public struct LogWithOptionalParameterList
    {
        // This type has no other purpose.
    }


    public static class Log
    {
        private static ConcurrentDictionary<string, Logger> _loggers = new ConcurrentDictionary<string, Logger>();
        //public static bool LogToFile { get; private set; } = false;
        public static bool Ready { get; private set; } = false;

        static Log()
        {

        }

        public static void Setup(bool logToConsole = true, bool logToFile = false)
        {
            Ready = false;
            try
            {
                var logConfig = new LoggingConfiguration();
                ColoredConsoleTarget consoleTarget = null;

                // Setup console logging
                if (logToConsole)
                {
                    consoleTarget = new ColoredConsoleTarget()
                    {
                        Layout = @"${date:format=HH\:mm\:ss} ${logger}${message}"
                    };

                    // Modify default colours
                    foreach (var level in LogLevel.AllLoggingLevels)
                    {
                        ConsoleOutputColor fgColour = ConsoleOutputColor.NoChange;
                        if (level == LogLevel.Info) fgColour = ConsoleOutputColor.Gray;
                        else if (level == LogLevel.Debug) fgColour = ConsoleOutputColor.Yellow;
                        else if (level == LogLevel.Trace) fgColour = ConsoleOutputColor.DarkYellow;
                        else if (level == LogLevel.Warn) fgColour = ConsoleOutputColor.Magenta;
                        else if (level == LogLevel.Error) fgColour = ConsoleOutputColor.Red;
                        else if (level == LogLevel.Fatal) fgColour = ConsoleOutputColor.Red; //DarkRed;

                        consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule()
                        {
                            Condition = ConditionParser.ParseExpression("level == LogLevel." + level.Name),
                            ForegroundColor = fgColour
                        });
                    }
                }

                // Setup file logging
                FileTarget fileTarget = null;
                FileTarget fileTarget_error = null;
                if (logToFile)
                {
                    fileTarget = new FileTarget()
                    {
                        Layout = @"${longdate} ${logger} | ${uppercase:${level:format}}${message}",
                        FileName = "${basedir}/logs/ditto.log",
                        KeepFileOpen = false,
                        CreateDirs = true,
                        Encoding = Encoding.UTF8,
                    };

                    fileTarget_error = new FileTarget()
                    {
                        Layout = @"${longdate} ${logger} | ${uppercase:${level:format}}${message}",
                        FileName = "${basedir}/logs/ditto_error.log",
                        KeepFileOpen = false,
                        CreateDirs = true,
                        Encoding = Encoding.UTF8,
                    };
                }


                // Add targets & rules
                var minTarget =
                #if DEBUG
                        LogLevel.Trace
                #else
                        LogLevel.Info
                #endif
                        ;
                if (logToConsole)
                {
                    logConfig.AddTarget("Console", consoleTarget);
                    logConfig.LoggingRules.Add(new LoggingRule("*", minTarget, consoleTarget));
                }
                if (logToFile)
                {
                    logConfig.LoggingRules.Add(new LoggingRule("*", LogLevel.Error, fileTarget_error));
                    logConfig.LoggingRules.Add(new LoggingRule("*", minTarget, fileTarget));
                }
                LogManager.Configuration = logConfig;
                Ready = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        

        private static string GetIdentifier(string callMethod)
        {
            return " | "; // (callMethod == ".cctor" ? " | " : ":" + callMethod + " | ");
        }
        private static Logger GetLogger(string filePath)
        {
            // Use the default configuration if we haven't manually set this yet
            if(!Ready)
            {
                Setup();
            }

            var name = Path.GetFileNameWithoutExtension(filePath);
            return _loggers.GetOrAdd(name, LogManager.GetLogger(name));
        }

        public static object[] Args(params object[] args)
        {
            return args;
        }

//================================================================================================================================
// Info
//================================================================================================================================
        public static void Info(string message,
            LogWithOptionalParameterList _ = default(LogWithOptionalParameterList),
            [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => GetLogger(callerFilePath)?.Info(GetIdentifier(callerMemberName) + message);

        public static void Info(string message, Exception exception,
            LogWithOptionalParameterList _ = default(LogWithOptionalParameterList),
            [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Info(message + " | " + exception.ToString() , callerFilePath, callerMemberName);

        public static void Info(Exception exception, string message,
            LogWithOptionalParameterList _ = default(LogWithOptionalParameterList),
            [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Info(message, exception, _, callerFilePath, callerMemberName);

        public static void Info<T>(T value
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Info(value?.ToString(), _, callerMemberName, callerFilePath);

        public static void Info<T1>(string format, T1 arg0
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Info(String.Format(format, new object[] { arg0 }), _, callerMemberName, callerFilePath);

        public static void Info<T1, T2>(string format, T1 arg0, T2 arg1
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Info(String.Format(format, new object[] { arg0, arg1 }), _, callerMemberName, callerFilePath);

        public static void Info<T1, T2, T3>(string format, T1 arg0, T2 arg1, T3 arg2
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Info(String.Format(format, new object[] { arg0, arg1, arg2 }), _, callerMemberName, callerFilePath);

        public static void Info<T1, T2, T3, T4>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Info(String.Format(format, new object[] { arg0, arg1, arg2, arg3 }), _, callerMemberName, callerFilePath);

        public static void Info<T1, T2, T3, T4, T5>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Info(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4 }), _, callerMemberName, callerFilePath);

        public static void Info<T1, T2, T3, T4, T5, T6>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Info(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5 }), _, callerMemberName, callerFilePath);

        public static void Info<T1, T2, T3, T4, T5, T6, T7>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5, T7 arg6
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Info(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6 }), _, callerMemberName, callerFilePath);

        public static void Info<T1, T2, T3, T4, T5, T6, T7, T8>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5, T7 arg6, T8 arg7
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Info(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7 }), _, callerMemberName, callerFilePath);

        public static void Info<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5, T7 arg6, T8 arg7, T9 arg8
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Info(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 }), _, callerMemberName, callerFilePath);

//================================================================================================================================
// Debug
//================================================================================================================================
        public static void Debug(string message
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => GetLogger(callerFilePath)?.Debug(GetIdentifier(callerMemberName) + message);

        public static void Debug(string message, Exception exception
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Debug(message + " | " + exception.ToString(), callerFilePath, callerMemberName);

        public static void Debug(Exception exception, string message
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Debug(message, exception, _, callerFilePath, callerMemberName);

        public static void Debug<T>(T value
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Debug(value?.ToString(), _, callerMemberName, callerFilePath);

        public static void Debug<T1>(string format, T1 arg0
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Debug(String.Format(format, new object[] { arg0 }), _, callerMemberName, callerFilePath);

        public static void Debug<T1, T2>(string format, T1 arg0, T2 arg1
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Debug(String.Format(format, new object[] { arg0, arg1 }), _, callerMemberName, callerFilePath);

        public static void Debug<T1, T2, T3>(string format, T1 arg0, T2 arg1, T3 arg2
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Debug(String.Format(format, new object[] { arg0, arg1, arg2 }), _, callerMemberName, callerFilePath);

        public static void Debug<T1, T2, T3, T4>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Debug(String.Format(format, new object[] { arg0, arg1, arg2, arg3 }), _, callerMemberName, callerFilePath);

        public static void Debug<T1, T2, T3, T4, T5>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Debug(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4 }), _, callerMemberName, callerFilePath);

        public static void Debug<T1, T2, T3, T4, T5, T6>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Debug(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5 }), _, callerMemberName, callerFilePath);

        public static void Debug<T1, T2, T3, T4, T5, T6, T7>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5, T7 arg6
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Debug(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6 }), _, callerMemberName, callerFilePath);

        public static void Debug<T1, T2, T3, T4, T5, T6, T7, T8>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5, T7 arg6, T8 arg7
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Debug(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7 }), _, callerMemberName, callerFilePath);

        public static void Debug<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5, T7 arg6, T8 arg7, T9 arg8
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Debug(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 }), _, callerMemberName, callerFilePath);

//================================================================================================================================
// Trace
//================================================================================================================================
        public static void Trace(string message
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => GetLogger(callerFilePath)?.Trace(GetIdentifier(callerMemberName) + message);

        public static void Trace(string message, Exception exception
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Trace(message + " | " + exception.ToString(), callerFilePath, callerMemberName);

        public static void Trace(Exception exception, string message
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Trace(message, exception, _, callerFilePath, callerMemberName);

        public static void Trace<T>(T value
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Trace(value?.ToString(), _, callerMemberName, callerFilePath);

        public static void Trace<T1>(string format, T1 arg0
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Trace(String.Format(format, new object[] { arg0 }), _, callerMemberName, callerFilePath);

        public static void Trace<T1, T2>(string format, T1 arg0, T2 arg1
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Trace(String.Format(format, new object[] { arg0, arg1 }), _, callerMemberName, callerFilePath);

        public static void Trace<T1, T2, T3>(string format, T1 arg0, T2 arg1, T3 arg2
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Trace(String.Format(format, new object[] { arg0, arg1, arg2 }), _, callerMemberName, callerFilePath);

        public static void Trace<T1, T2, T3, T4>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Trace(String.Format(format, new object[] { arg0, arg1, arg2, arg3 }), _, callerMemberName, callerFilePath);

        public static void Trace<T1, T2, T3, T4, T5>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Trace(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4 }), _, callerMemberName, callerFilePath);

        public static void Trace<T1, T2, T3, T4, T5, T6>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Trace(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5 }), _, callerMemberName, callerFilePath);

        public static void Trace<T1, T2, T3, T4, T5, T6, T7>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5, T7 arg6
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Trace(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6 }), _, callerMemberName, callerFilePath);

        public static void Trace<T1, T2, T3, T4, T5, T6, T7, T8>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5, T7 arg6, T8 arg7
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Trace(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7 }), _, callerMemberName, callerFilePath);

        public static void Trace<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5, T7 arg6, T8 arg7, T9 arg8
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Trace(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 }), _, callerMemberName, callerFilePath);
        
//================================================================================================================================
// Warn
//================================================================================================================================
        public static void Warn(string message
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => GetLogger(callerFilePath)?.Warn(GetIdentifier(callerMemberName) + message);

        public static void Warn(string message, Exception exception
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Warn(message + " | " + exception.ToString() , callerFilePath, callerMemberName);

        public static void Warn(Exception exception, string message
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Warn(message, exception, _, callerFilePath, callerMemberName);

        public static void Warn<T>(T value
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Warn(value?.ToString(), _, callerMemberName, callerFilePath);

        public static void Warn<T1>(string format, T1 arg0
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Warn(String.Format(format, new object[] { arg0 }), _, callerMemberName, callerFilePath);

        public static void Warn<T1, T2>(string format, T1 arg0, T2 arg1
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Warn(String.Format(format, new object[] { arg0, arg1 }), _, callerMemberName, callerFilePath);

        public static void Warn<T1, T2, T3>(string format, T1 arg0, T2 arg1, T3 arg2
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Warn(String.Format(format, new object[] { arg0, arg1, arg2 }), _, callerMemberName, callerFilePath);

        public static void Warn<T1, T2, T3, T4>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Warn(String.Format(format, new object[] { arg0, arg1, arg2, arg3 }), _, callerMemberName, callerFilePath);

        public static void Warn<T1, T2, T3, T4, T5>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Warn(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4 }), _, callerMemberName, callerFilePath);

        public static void Warn<T1, T2, T3, T4, T5, T6>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Warn(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5 }), _, callerMemberName, callerFilePath);

        public static void Warn<T1, T2, T3, T4, T5, T6, T7>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5, T7 arg6
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Warn(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6 }), _, callerMemberName, callerFilePath);

        public static void Warn<T1, T2, T3, T4, T5, T6, T7, T8>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5, T7 arg6, T8 arg7
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Warn(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7 }), _, callerMemberName, callerFilePath);

        public static void Warn<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5, T7 arg6, T8 arg7, T9 arg8
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Warn(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 }), _, callerMemberName, callerFilePath);
        
//================================================================================================================================
// Error
//================================================================================================================================
        public static void Error(string message
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => GetLogger(callerFilePath)?.Error(GetIdentifier(callerMemberName) + message);

        public static void Error(string message, Exception exception
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Error(message + " | " + exception.ToString() , callerFilePath, callerMemberName);

        public static void Error(Exception exception, string message
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Error(message, exception, _, callerFilePath, callerMemberName);

        public static void Error<T>(T value
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Error(value?.ToString(), _, callerMemberName, callerFilePath);

        public static void Error<T1>(string format, T1 arg0
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Error(String.Format(format, new object[] { arg0 }), _, callerMemberName, callerFilePath);

        public static void Error<T1, T2>(string format, T1 arg0, T2 arg1
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Error(String.Format(format, new object[] { arg0, arg1 }), _, callerMemberName, callerFilePath);

        public static void Error<T1, T2, T3>(string format, T1 arg0, T2 arg1, T3 arg2
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Error(String.Format(format, new object[] { arg0, arg1, arg2 }), _, callerMemberName, callerFilePath);

        public static void Error<T1, T2, T3, T4>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Error(String.Format(format, new object[] { arg0, arg1, arg2, arg3 }), _, callerMemberName, callerFilePath);

        public static void Error<T1, T2, T3, T4, T5>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Error(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4 }), _, callerMemberName, callerFilePath);

        public static void Error<T1, T2, T3, T4, T5, T6>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Error(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5 }), _, callerMemberName, callerFilePath);

        public static void Error<T1, T2, T3, T4, T5, T6, T7>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5, T7 arg6
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Error(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6 }), _, callerMemberName, callerFilePath);

        public static void Error<T1, T2, T3, T4, T5, T6, T7, T8>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5, T7 arg6, T8 arg7
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Error(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7 }), _, callerMemberName, callerFilePath);

        public static void Error<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5, T7 arg6, T8 arg7, T9 arg8
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Error(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 }), _, callerMemberName, callerFilePath);

//================================================================================================================================
// Fatal
//================================================================================================================================
        public static void Fatal(string message
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => GetLogger(callerFilePath)?.Fatal(GetIdentifier(callerMemberName) + "[FATAL] " + message);

        public static void Fatal(string message, Exception exception
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Fatal(message + " | " + exception.ToString(), callerFilePath, callerMemberName);

        public static void Fatal(Exception exception, string message
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Fatal(message, exception, _, callerFilePath, callerMemberName);

        public static void Fatal<T>(T value
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Fatal(value?.ToString(), _, callerMemberName, callerFilePath);

        public static void Fatal<T1>(string format, T1 arg0
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Fatal(String.Format(format, new object[] { arg0 }), _, callerMemberName, callerFilePath);

        public static void Fatal<T1, T2>(string format, T1 arg0, T2 arg1
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Fatal(String.Format(format, new object[] { arg0, arg1 }), _, callerMemberName, callerFilePath);

        public static void Fatal<T1, T2, T3>(string format, T1 arg0, T2 arg1, T3 arg2
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Fatal(String.Format(format, new object[] { arg0, arg1, arg2 }), _, callerMemberName, callerFilePath);

        public static void Fatal<T1, T2, T3, T4>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Fatal(String.Format(format, new object[] { arg0, arg1, arg2, arg3 }), _, callerMemberName, callerFilePath);

        public static void Fatal<T1, T2, T3, T4, T5>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Fatal(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4 }), _, callerMemberName, callerFilePath);

        public static void Fatal<T1, T2, T3, T4, T5, T6>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Fatal(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5 }), _, callerMemberName, callerFilePath);

        public static void Fatal<T1, T2, T3, T4, T5, T6, T7>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5, T7 arg6
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Fatal(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6 }), _, callerMemberName, callerFilePath);

        public static void Fatal<T1, T2, T3, T4, T5, T6, T7, T8>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5, T7 arg6, T8 arg7
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Fatal(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7 }), _, callerMemberName, callerFilePath);

        public static void Fatal<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string format, T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4, T6 arg5, T7 arg6, T8 arg7, T9 arg8
            , LogWithOptionalParameterList _ = default(LogWithOptionalParameterList)
            , [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null)
            => Fatal(String.Format(format, new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 }), _, callerMemberName, callerFilePath);


    }
}
