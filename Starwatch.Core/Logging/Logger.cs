using System;

namespace Starwatch.Logging
{
    /// <summary>
    /// Called when the logger logs a log.
    /// </summary>
    /// <param name="type">The type of log</param>
    /// <param name="message">The message that was logged</param>
    public delegate void LoggerEvent(Logger.LogType type, string message);

    /// <summary>
    /// The main logging class. All logs must go through this class. It handles indentation with inherentence, streams and storage. Contains simple to use formatting and logging techniques.
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// The type of log.
        /// </summary>
        public enum LogType {
            /// <summary>
            /// A information log
            /// </summary>
            Info = 0,
            /// <summary>
            /// A warning log
            /// </summary>
            Warning = 1,
            /// <summary>
            /// A error log
            /// </summary>
            Error = 2
        }

        /// <summary>
        /// The level that we should print logs at
        /// </summary>
        [System.Flags]
        public enum LogLevel
        {
            /// <summary>
            /// No log.
            /// </summary>
            None = 0,

            /// <summary>
            /// Information logs.
            /// </summary>
            Info = 1,

            /// <summary>
            /// Warning logs.
            /// </summary>
            Warning = 2,

            /// <summary>
            /// Error logs.
            /// </summary>
            Error = 4,

            /// <summary>
            /// All the types.
            /// </summary>
            All = Info | Warning | Error
        }

        /// <summary>
        /// The current mode the logger is in. If the logger has a <see cref="Parent"/>, then the parents mode will be used instead.
        /// </summary>
        public enum LogMode {
            /// <summary>
            /// The logger will not print any messages out to the console or the file.
            /// </summary>
            Silent,

            /// <summary>
            /// The logger will print messages out to the console.
            /// </summary>
            Console,

            /// <summary>
            /// The logger will print messages out to the console and to a file. It will overwrite the contents of the file.
            /// </summary>
            ConsoleFileOverwrite,

            /// <summary>
            /// The logger will print the messages out to the console and to a file. It will append the contents to the file.
            /// </summary>
            ConsoleFileAppend
        }

		/// <summary>
		/// Should the console be colourised?
		/// </summary>
		public static bool Colourise { get; set; } = false;

        /// <summary>
        /// The TAG that is used for identification in the logs.
        /// </summary>
        public string Tag { get; set; }
        
        /// <summary>
        /// The parent of the logger.
        /// </summary>
        public Logger Parent { get; set; }

        /// <summary>
        /// The level of logs to print.
        /// </summary>
        public LogLevel Level { get; set; } = LogLevel.All;
		
        /// <summary>
        /// This event is called whenever the root is writting a log. Only called on root.
        /// </summary>
        public event LoggerEvent OnLog;

        /// <summary>
        /// The whitespace used between the log and the breadcrumb
        /// </summary>
        public string Whitespace { get; set; } = ": ";

		/// <summary>
		/// Should the whitespace be included?
		/// </summary>
		public bool IncludeWhitespace { get; set; } = true;

		/// <summary>
		/// How each tag is formatted in the breadcrumb
		/// </summary>
		public string TagFormat { get; set; } = ".{0}";

		/// <summary>
		/// How each type is formatted in the breadcrumb
		/// </summary>
		public string TypeFormat { get; set; } = "{0} ";

		private Logger() {  }

        /// <summary>
        /// Creates a new instance of a logger.
        /// </summary>
        /// <param name="tag">The identifier the logger will be known as</param>
        /// <param name="name">The actual name for the logger</param>
        /// <param name="parent">The parent of the logger. It is recommened to always follow the parent structure.</param>
        public Logger(string tag, Logger parent = null) : base()
        {
            this.Tag = tag;
            this.Parent = parent;
        }
		
        #region Logging
        /// <summary>
        /// Logs a information message.
        /// </summary>
        /// <param name="message">The format to use or the message itself.</param>
        /// <param name="args">Arguments to format the message with</param>
        public void Log(object message, params object[] args)
        {
            LogParent(FormatMessage(message != null ? message.ToString() : "NULL", args), true, LogType.Info);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The format to use or the message itself.</param>
        /// <param name="args">Arguments to format the message with</param>
        public void LogWarning(object message, params object[] args)
        {
            LogParent(FormatMessage(message != null ? message.ToString() : "NULL", args), true, LogType.Warning);
        }

        /// <summary>
        /// Logs a error message.
        /// </summary>
        /// <param name="message">The format to use or the message itself.</param>
        /// <param name="args">Arguments to format the message with</param>
        public void LogError(object message, params object[] args)
        {
            LogParent(FormatMessage(message != null ? message.ToString() : "NULL", args), true, LogType.Error);
        }
        
		/// <summary>
		/// Logs an exception
		/// </summary>
		/// <param name="exception">The exception</param>
		/// <param name="format">Formatting of the exception message</param>
		public void LogError(Exception exception, string format = "Exception Occured: {0}")
		{
			//Log the message of the exception
			LogError(format, exception.Message);

			//If we are an aggregated exception, log all the sub exceptions
			if (exception is AggregateException)
			{
				AggregateException aggregate = (AggregateException)exception;
				aggregate.Handle((x) =>
				{
					LogError((Exception)x, "Aggregate Exception: {0}");
					return true;
				});
			}
			else
			{
				//We are a normal exception, so just do a normal log
				LogError(exception.StackTrace);
			}
		}

        // Format Helping
        private string FormatMessage(string format, params object[] args)
        {
			if (format == null) return "";

            if (args.Length == 0) return format;
			return string.Format(format, args);
        }

        #endregion

        /// <summary>
        /// Look through the parent structure to the very root and fetch the result.
        /// </summary>
        /// <returns>The root parent that logs the messages.</returns>
        public Logger GetRootParent()
        {
            if (Parent == null) return null;
            if (Parent.Parent == null) return Parent;
            return Parent.GetRootParent();
        }

        private void LogParent(object message, bool first, LogType type)
        {
            //Check if we should mute the current level
            if (type == LogType.Info && (this.Level & LogLevel.Info) != LogLevel.Info) return;
            if (type == LogType.Warning && (this.Level & LogLevel.Warning) != LogLevel.Warning) return;
            if (type == LogType.Error && (this.Level & LogLevel.Error) != LogLevel.Error) return;

            //Prepare the message to append
            string msg = string.Format(TagFormat, Tag) + (first && IncludeWhitespace ? Whitespace : "") + message;

            if (Parent != null)
            {
                //The parent isn't null, so we should past this up to the parent
                Parent.LogParent(msg, false, type);
            }
            else
            {
                //Prepare the type string
                string logtype;
                switch(type)
                {
                    default:
                        logtype = "INFO";
						if (Logger.Colourise)
						{
							Console.BackgroundColor = new ConsoleColor();
							Console.ForegroundColor = ConsoleColor.Gray;
						}
						break;

                    case LogType.Warning:
                        logtype = "WARN";
						if (Logger.Colourise)
						{
							Console.BackgroundColor = new ConsoleColor();
							Console.ForegroundColor = ConsoleColor.Yellow;
						}
						break;

                    case LogType.Error:
                        logtype = "ERR";
						if (Logger.Colourise)
						{
							Console.BackgroundColor = ConsoleColor.Red;
							Console.ForegroundColor = ConsoleColor.White;
						}
						break;
                }

				//Invoke the event and then push the log
				try
				{
					OnLog?.Invoke(type, msg);
				}
				catch (Exception e)
				{
                    LogLoggerException(e, "Invoke");
                }

				//Send the actual output
				LogOutput(FormatMessage(TypeFormat, logtype) + msg);
            }
        }

        private static void LogOutput(string message)
        {
			try
			{
				if (OutputLogQueue.Current == null)
					Console.WriteLine("OUT: " + message);
				else
					OutputLogQueue.Current.WriteLine(message);
			}
			catch (Exception e)
			{
                LogLoggerException(e, "WriteLine");
			}

		}

        private static void LogLoggerException(Exception e, string key)
        {
            Console.WriteLine();
            Console.WriteLine("==============================================================================");

            if (e is AggregateException)
            {
                Console.WriteLine("LOGGER EXCEPTION: An exception occured while trying to invoke a log list. AGGREGATE!");
   
                AggregateException aggregate = (AggregateException)e;
                aggregate.Handle((x) =>
                {
                    Console.WriteLine(x.Message);
                    Console.WriteLine(x.StackTrace);
                    Console.WriteLine(">>>>   ");
                    return true;
                });
            }
            else
            {
                Console.WriteLine("LOGGER EXCEPTION: An exception occured while trying to invoke a log list. ({1}) {0}", e.Message, key);
                Console.WriteLine(e.StackTrace);
            }

            Console.WriteLine("==============================================================================");
            Console.WriteLine();
        }

        internal void SetParent(Logger parent)
        {
            this.Parent = parent;
        }
    }
}
