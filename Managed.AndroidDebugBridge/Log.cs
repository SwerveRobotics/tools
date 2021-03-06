﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Org.SwerveRobotics.Tools.ManagedADB {
	/// <summary>
	/// 
	/// </summary>
	public sealed class Log {

		public static ILogOutput            LogOutput      { get; set; }
        private static LogLevel.LogLevelInfo g_thresholdLevel;
        public static LogLevel.LogLevelInfo ThresholdLevel
            {
            get
                {
                return g_thresholdLevel;
                }
            set
                {
                g_thresholdLevel = value;
                }
            }

        private static char[] SpaceLine = new char[72];
		private static readonly char[] HEXDIGIT = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

        static Log()
        // Static initializer
            {
            /* prep for hex dump */
            int i = SpaceLine.Length - 1;
            while (i >= 0)
                SpaceLine[i--] = ' ';
            SpaceLine[0] = SpaceLine[1] = SpaceLine[2] = SpaceLine[3] = '0';
            SpaceLine[4] = '-';
            ThresholdLevel = DdmPreferences.LogLevel;
            }

        private Log()
            {
            }

        /// <summary>
        /// 
        /// </summary>
        sealed class Config {
			/// <summary>
			/// Log Verbose
			/// </summary>
			public const bool LOGV = true;
			/// <summary>
			/// Log Debug
			/// </summary>
			public const bool LOGD = true;
		};

		/// <summary>
		/// Outputs a Verbose level message.
		/// </summary>
		/// <param name="tag">The tag associated with the message.</param>
		/// <param name="message">The message to output</param>
		public static void v (string tag, string message ) {
			WriteLine ( LogLevel.Verbose, tag, message );
		}

		/// <summary>
		/// Outputs a Verbose level message.
		/// </summary>
		/// <param name="tag">The tag associated with the message.</param>
		/// <param name="format">The message to output format string.</param>
		/// <param name="args">The values for the format message</param>
		public static void v (string tag, string format, params object[] args ) {
			WriteLine ( LogLevel.Verbose, tag, string.Format ( format, args ) );
		}

		/// <summary>
		/// Outputs a Debug level message.
		/// </summary>
		/// <param name="tag">The tag associated with the message.</param>
		/// <param name="message">The message to output</param>
		public static void d (string tag, string message ) {
			WriteLine ( LogLevel.Debug, tag, message );
		}

		/// <summary>
		/// Outputs a Debug level message.
		/// </summary>
		/// <param name="tag">The tag associated with the message.</param>
		/// <param name="format">The message to output format string.</param>
		/// <param name="args">The values for the format message</param>
		public static void d (string tag, string format, params object[] args ) {
			WriteLine ( LogLevel.Debug, tag, string.Format ( format, args ) );
		}

		/// <summary>
		/// Outputs a Info level message.
		/// </summary>
		/// <param name="tag">The tag associated with the message.</param>
		/// <param name="message">The message to output.</param>
		public static void i (string tag, string message ) {
			WriteLine ( LogLevel.Info, tag, message );
		}

		/// <summary>
		/// Outputs a Info level message.
		/// </summary>
		/// <param name="tag">The tag associated with the message.</param>
		/// <param name="format">The message to output format string.</param>
		/// <param name="args">The values for the format message</param>
		public static void i (string tag, string format, params object[] args ) {
			WriteLine ( LogLevel.Info, tag, string.Format ( format, args ) );
		}

		/// <summary>
		/// Outputs a Warn level message.
		/// </summary>
		/// <param name="tag">The tag associated with the message.</param>
		/// <param name="message">The message to output.</param>
		public static void w (string tag, string message ) {
			WriteLine ( LogLevel.Warn, tag, message );
		}

		/// <summary>
		/// Outputs a Warn level message.
		/// </summary>
		/// <param name="tag">The tag associated with the message.</param>
		/// <param name="format">The message to output format string.</param>
		/// <param name="args">The values for the format message</param>
		public static void w (string tag, string format, params object[] args ) {
			WriteLine ( LogLevel.Warn, tag, string.Format ( format, args ) );
		}

		/// <summary>
		/// Outputs a Warn level message.
		/// </summary>
		/// <param name="tag">The tag associated with the message.</param>
		/// <param name="exception">The exception to warn</param>
		public static void w (string tag, Exception exception ) {
			if ( exception != null ) {
				w ( tag, exception.ToString ( ) );
			}
		}

		/// <summary>
		/// Outputs a Warn level message.
		/// </summary>
		/// <param name="tag">The tag associated with the message.</param>
		/// <param name="message">The message to output.</param>
		/// <param name="exception">The exception to warn</param>
		public static void w (string tag, string message, Exception exception ) {
			w ( tag, "{0}\n{1}", message, exception );
		}

		/// <summary>
		/// Outputs a Error level message.
		/// </summary>
		/// <param name="tag">The tag associated with the message.</param>
		/// <param name="message">The message to output.</param>
		/// <gist id="f4fa3525f899e5461d4e" />
		public static void e (string tag, string message ) {
			WriteLine ( LogLevel.Error, tag, message );
		}

		/// <summary>
		/// Outputs a Error level message.
		/// </summary>
		/// <param name="tag">The tag associated with the message.</param>
		/// <param name="format">The message to output format string.</param>
		/// <param name="args">The values for the format message</param>
		/// <gist id="16a731d7e4f074fca809" />
		public static void e (string tag, string format, params object[] args ) {
			WriteLine ( LogLevel.Error, tag, string.Format ( format, args ) );
		}

		/// <summary>
		/// Outputs a Error level message.
		/// </summary>
		/// <param name="tag">The tag associated with the message.</param>
		/// <param name="exception">The exception to warn</param>
		/// <gist id="4e0438f59a00d57af4ef"/>
		public static void e (string tag, Exception exception ) {
			if ( exception != null ) {
				e ( tag, exception.ToString ( ) );
			}
		}

		/// <summary>
		/// Outputs a Error level message.
		/// </summary>
		/// <param name="tag">The tag associated with the message.</param>
		/// <param name="message">The message to output.</param>
		/// <param name="exception">The exception to warn</param>
		/// <gist id="90b6664f4dd84da50b27" />
		public static void e (string tag, string message, Exception exception ) {
			e ( tag, "{0}\n{1}", message, exception );
		}


		/// <summary>
		/// Outputs a log message and attempts to display it in a dialog.
		/// </summary>
		/// <param name="logLevel">The log level</param>
		/// <param name="tag">The tag associated with the message.</param>
		/// <param name="message">The message to output.</param>
		public static void LogAndDisplay ( LogLevel.LogLevelInfo logLevel, string tag, string message ) {
			if ( LogOutput != null ) {
				LogOutput.WriteAndPromptLog ( logLevel, tag, message );
			} else {
				WriteLine ( logLevel, tag, message );
			}
		}


		/// <summary>
		/// Dump the entire contents of a byte array with DEBUG priority.
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="level"></param>
		/// <param name="data"></param>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <remarks>
		/// Local addition.  Output looks like:
		/// 1230- 00 11 22 33 44 55 66 77 88 99 aa bb cc dd ee ff  0123456789abcdef
		/// Uses no string concatenation; creates one String object per line.
		/// </remarks>
		internal static void HexDump (string tag, LogLevel.LogLevelInfo level, byte[] data, int offset, int length ) {

			int kHexOffset = 6;
			int kAscOffset = 55;
			char[] line = new char[SpaceLine.Length];
			int addr, baseAddr, count;
			int i, ch;
			bool needErase = true;

			//Log.w(tag, "HEX DUMP: off=" + offset + ", length=" + length);

			baseAddr = 0;
			while ( length != 0 ) {
				if ( length > 16 ) {
					// full line
					count = 16;
				} else {
					// partial line; re-copy blanks to clear end
					count = length;
					needErase = true;
				}

				if ( needErase ) {
					Array.Copy ( SpaceLine, 0, line, 0, SpaceLine.Length );
					needErase = false;
				}

				// output the address (currently limited to 4 hex digits)
				addr = baseAddr;
				addr &= 0xffff;
				ch = 3;
				while ( addr != 0 ) {
					line[ch] = HEXDIGIT[addr & 0x0f];
					ch--;
					addr >>= 4;
				}

				// output hex digits and ASCII chars
				ch = kHexOffset;
				for ( i = 0; i < count; i++ ) {
					byte val = data[offset + i];

					line[ch++] = HEXDIGIT[( val >> 4 ) & 0x0f];
					line[ch++] = HEXDIGIT[val & 0x0f];
					ch++;

					if ( val >= 0x20 && val < 0x7f )
						line[kAscOffset + i] = (char)val;
					else
						line[kAscOffset + i] = '.';
				}

				WriteLine ( level, tag, new string( line ) );

				// advance to next chunk of data
				length -= count;
				offset += count;
				baseAddr += count;
			}

		}


		/// <summary>
		/// Dump the entire contents of a byte array with DEBUG priority.
		/// </summary>
		/// <param name="data"></param>
		internal static void HexDump ( byte[] data ) {
			HexDump ( "ddms", LogLevel.Debug, data, 0, data.Length );
		}

        /// <summary>
        /// prints to stdout; could write to a log window
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="tag"></param>
        /// <param name="message"></param>
        private static void WriteLine(LogLevel.LogLevelInfo logLevel, string tag, string message)
            {
            if (ThresholdLevel.Priority <= logLevel.Priority)
                {
                if (LogOutput != null)
                    LogOutput.Write(logLevel, tag, message);
                else
                    WriteUnchecked(logLevel, tag, message);

                WriteDebugUnchecked(logLevel, tag, message);
                }
            }


        public static void WriteUnchecked(LogLevel.LogLevelInfo logLevel, string tag, string message)
            {
            Console.WriteLine(GetLogFormatString(logLevel, tag, message));
            }

        public static void WriteDebugUnchecked(LogLevel.LogLevelInfo logLevel, string tag, string message)
            {
            System.Diagnostics.Trace.Write($"{Util.TraceTag}| {GetLogFormatString(logLevel, tag, message)}");
            }

        public static string GetLogFormatString(LogLevel.LogLevelInfo logLevel, string tag, string message)
            {
            long msec = DateTime.Now.ToUnixEpoch();
            return $"{(msec/60000)%60:00}:{(msec/1000)%60:00} {logLevel.Letter}/{tag}: {message}";
            }
        }
    }
