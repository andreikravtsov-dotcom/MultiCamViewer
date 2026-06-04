using Serilog;
using System;

namespace MultiCamViewer
{
    public static class Logger
    {
        /// <summary>
        /// Logs an informational message to the console and file.
        /// </summary>
        /// <param name="message"></param>
        public static void Info(string message)
            => Log.Information(message);

        /// <summary>
        /// Logs an error message along with the exception details to the console and file.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="message"></param>
        public static void Error(Exception ex, string message)
            => Log.Error(ex, message);
    }
}
