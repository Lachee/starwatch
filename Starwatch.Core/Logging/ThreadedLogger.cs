using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Starwatch.Logging
{
    /// <summary>
    /// A singleton queue that manages writing log entries to the different logging sources (Enterprise Library Logging) off the executing thread.
    /// This queue ensures that log entries are written in the order that they were executed and that logging is only utilizing one thread (backgroundworker) at any given time.
    /// </summary>
    internal class OutputLogQueue : IDisposable
	{
		private static OutputLogQueue _current;
		public static OutputLogQueue Current { get { return _current; } }

		private object _propertyLock = new object();
		public string Filename { get; } = "Starwatch.log";
		public bool Append { get; } = false;

		private ConcurrentQueue<string> _conqueue;
		private Queue<string> _queue = new Queue<string>();
		private object _queueLock = new object();

		private Thread _thread;
		private StreamWriter _stream;

		private int writeDelay = 250;
		private bool _outputConsole = true;
		private bool _outputStream = true;

		private OutputLogQueue(string filename, bool append) {

			lock (_propertyLock)
			{
				Filename = filename;
				Append = append;
			}

			//Preepare teh queue
			_conqueue = new ConcurrentQueue<string>();

			//configure background worker
			//_worker.WorkerSupportsCancellation = false;
			//_worker.DoWork += new DoWorkEventHandler(OnDoWork);
			_thread = new Thread(ThreadStart);
			_thread.Start();
		}

		public static void Initialize(string filename, bool append)
		{
			if (_current != null) _current.Dispose();
			_current = new OutputLogQueue(filename, append);
		}
        
		internal void WriteLine(string log)
		{
			if (log == null) log = "<NULL>";
			if (_conqueue == null)
                Console.WriteLine("BAD CONQUE: " + log);
			else
				_conqueue.Enqueue(log);

			//lock (_queueLock) _queue.Enqueue(log);
		}

		private void ThreadStart()
		{
            Console.WriteLine("Logger Initiated!");

			//Create the stream
			lock (_propertyLock)
			{
				if (_outputStream)
				{
					_stream = new StreamWriter(Filename, Append);
					_stream.AutoFlush = false;

					//If we are appending, create a linebreak
					CreateStreamHeading();
				}
				else
				{
					_stream = null;
				}
			}


			bool isRunning = true;
			while (isRunning)
			{
				try
				{
					//Iteratively go through each message, dequeing it and logging it.
					string message;
					while (_conqueue.TryDequeue(out message))
					{
						//Stream the message
						if (_stream != null)
						{
							if (_outputConsole) Console.WriteLine(message);
							if (_outputStream) _stream.WriteLine(message);
						}
						else
						{
							if (_outputConsole) Console.WriteLine("{NOT FILE}" + message);
						}
					}

					//Flush the stream
					if (_outputStream && _stream != null)
						_stream.Flush();

					//Just wait some time before we try and dequeue again
					Thread.Sleep(writeDelay);

				}
				catch(ThreadAbortException)
				{
					//we have been aborted, so cancel the abort and break out of this loop
					Thread.ResetAbort();
					isRunning = false;
					break;
				}
				catch (Exception e)
				{
					Console.WriteLine("Unkown Exception in threaded logger! {0}", e.Message);
                    Console.WriteLine(e.StackTrace);
				}
			}

			//Dispose of the stream
			if (_stream != null)
			{
				_stream.Flush();
				_stream.Dispose();
				_stream = null;
			}

			//We are done
		}

		private void CreateStreamHeading()
		{
			if (_stream == null) return;

			_stream.WriteLine();
			_stream.WriteLine("----------------------------------------------------");
			_stream.WriteLine("+ Date: " + DateTime.UtcNow.ToString());
			_stream.WriteLine("----------------------------------------------------");
			_stream.Flush();
		}

		public void Dispose()
		{
			//Dispose the worker
			//_worker.Dispose();

			if (_thread != null)
			{
				_thread.Abort();
				_thread.Join();
				_thread = null;
			}
			
			_current = null;
		}
	}
}
