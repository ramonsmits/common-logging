#region License

/*
 * Copyright © 2002-2009 the original author or authors.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *      http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using Common.Logging.Simple;
using Common.Logging.Configuration;

namespace Common.Logging
{
	/// <summary>
	/// Use the LogManager's <see cref="GetLogger(string)"/> or <see cref="GetLogger(System.Type)"/> 
	/// methods to obtain <see cref="ILog"/> instances for logging.
	/// </summary>
	/// <remarks>
	/// For configuring the underlying log system using application configuration, see the example 
	/// at <see cref="ConfigurationSectionHandler"/>. 
	/// For configuring programmatically, see the example section below.
	/// </remarks>
	/// <example>
	/// The example below shows the typical use of LogManager to obtain a reference to a logger
	/// and log an exception:
	/// <code>
	/// 
	/// ILog log = LogManager.GetLogger(this.GetType());
	/// ...
	/// try 
	/// { 
	///   /* .... */ 
	/// }
	/// catch(Exception ex)
	/// {
	///   log.ErrorFormat("Hi {0}", ex, "dude");
	/// }
	/// 
	/// </code>
	/// The example below shows programmatic configuration of the underlying log system:
	/// <code>
	/// 
	/// // create properties
	/// NameValueCollection properties = new NameValueCollection();
	/// properties[&quot;showDateTime&quot;] = &quot;true&quot;;
	/// 
	/// // set Adapter
	/// Common.Logging.LogManager.Adapter = new 
	/// Common.Logging.Simple.ConsoleOutLoggerFactoryAdapter(properties);
	/// 
	/// </code>
	/// </example>
	/// <seealso cref="ILog"/>
	/// <seealso cref="Adapter"/>
	/// <seealso cref="ILoggerFactoryAdapter"/>
	/// <seealso cref="ConfigurationSectionHandler"/>
	/// <author>Gilles Bayon</author>
	public static class LogManager
	{
		/// <summary>
		/// The name of the default configuration section to read settings from.
		/// </summary>
		/// <remarks>
		/// You can always change the source of your configuration settings by setting another <see cref="IConfigurationReader"/> instance
		/// on <see cref="ConfigurationReader"/>.
		/// </remarks>
		public static readonly string COMMON_LOGGING_SECTION = "common/logging";

		private static IConfigurationReader _configurationReader;
		private static readonly List<ILoggerFactoryAdapter> _adapters = new List<ILoggerFactoryAdapter>();
		private static readonly object _loadLock = new object();

		/// <summary>
		/// Performs static 1-time init of LogManager by calling <see cref="Reset()"/>
		/// </summary>
		static LogManager()
		{
			Reset();
		}

		/// <summary>
		/// Reset the <see cref="Common.Logging" /> infrastructure to its default settings. This means, that configuration settings
		/// will be re-read from section <c>&lt;common/logging&gt;</c> of your <c>app.config</c>.
		/// </summary>
		/// <remarks>
		/// This is mainly used for unit testing, you wouldn't normally use this in your applications.<br/>
		/// <b>Note:</b><see cref="ILog"/> instances already handed out from this LogManager are not(!) affected. 
		/// Resetting LogManager only affects new instances being handed out.
		/// </remarks>
		public static void Reset()
		{
			Reset(new DefaultConfigurationReader());
		}

		/// <summary>
		/// Reset the <see cref="Common.Logging" /> infrastructure to its default settings. This means, that configuration settings
		/// will be re-read from section <c>&lt;common/logging&gt;</c> of your <c>app.config</c>.
		/// </summary>
		/// <remarks>
		/// This is mainly used for unit testing, you wouldn't normally use this in your applications.<br/>
		/// <b>Note:</b><see cref="ILog"/> instances already handed out from this LogManager are not(!) affected. 
		/// Resetting LogManager only affects new instances being handed out.
		/// </remarks>
		/// <param name="reader">
		/// the <see cref="IConfigurationReader"/> instance to obtain settings for 
		/// re-initializing the LogManager.
		/// </param>
		public static void Reset(IConfigurationReader reader)
		{
			lock (_loadLock)
			{
				if (reader == null)
				{
					throw new ArgumentNullException("reader");
				}
				_configurationReader = reader;
				_adapters.Clear();
			}
		}

		/// <summary>
		/// Gets the configuration reader used to initialize the LogManager.
		/// </summary>
		/// <remarks>Primarily used for testing purposes but maybe useful to obtain configuration
		/// information from some place other than the .NET application configuration file.</remarks>
		/// <value>The configuration reader.</value>
		public static IConfigurationReader ConfigurationReader
		{
			get
			{
				return _configurationReader;
			}
		}

		static ILoggerFactoryAdapter GetAdapter()
		{

			if (Adapters.Count > 2)
			{
				throw new InvalidOperationException("Multiple factories are registered. Please use Adapters property.");
			}

			return Adapters[0];
		}

		static void SetAdapter(ILoggerFactoryAdapter value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("Adapter");
			}

			lock (_loadLock)
			{
				if (_adapters.Count > 1)
				{
					throw new InvalidOperationException("Multiple factories are registered. Please use Adapters property.");
				}
				_adapters.Clear();
				_adapters.Add(value);
			}
		}
		/// <summary>
		/// Gets or sets the adapter.
		/// </summary>
		/// <value>The adapter.</value>
		//[Obsolete("Please use Adapters property.")]
		public static ILoggerFactoryAdapter Adapter
		{
			get
			{
				return GetAdapter();
			}
			set
			{
				SetAdapter(value);
			}
		}

		/// <summary>
		/// Gets the adapters.
		/// </summary>
		public static IList<ILoggerFactoryAdapter> Adapters
		{
			get
			{
				if (_adapters.Count == 0)
				{
					lock (_loadLock)
					{
						if (_adapters.Count == 0)
						{
							_adapters.AddRange(BuildLoggerFactoryAdapters());
						}
					}
				}

				return _adapters;
			}
		}

		/// <summary>
		/// Registers the adapter.
		/// </summary>
		/// <param name="adapter">The adapter.</param>
		public static void RegisterAdapter(ILoggerFactoryAdapter adapter)
		{
			if (adapter == null)
			{
				throw new ArgumentNullException("Adapter");
			}

			lock (_loadLock)
			{
				_adapters.Add(adapter);
			}
		}

		/// <summary>
		/// Gets the logger by calling <see cref="ILoggerFactoryAdapter.GetLogger(Type)"/>
		/// on the currently configured <see cref="Adapter"/> using the type of the calling class.
		/// </summary>
		/// <remarks>
		/// This method needs to inspect the <see cref="StackTrace"/> in order to determine the calling 
		/// class. This of course comes with a performance penalty, thus you shouldn't call it too
		/// often in your application.
		/// </remarks>
		/// <seealso cref="GetLogger(Type)"/>
		/// <returns>the logger instance obtained from the current <see cref="Adapter"/></returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static ILog GetCurrentClassLogger()
		{
			StackFrame frame = new StackFrame(1, false);
			ILoggerFactoryAdapter adapter = GetAdapter();
			MethodBase method = frame.GetMethod();
			Type declaringType = method.DeclaringType;
			return adapter.GetLogger(declaringType);
		}

		/// <summary>
		/// Gets the logger by calling <see cref="ILoggerFactoryAdapter.GetLogger(Type)"/>
		/// on the currently configured <see cref="Adapter"/> using the specified type.
		/// </summary>
		/// <returns>the logger instance obtained from the current <see cref="Adapter"/></returns>
		public static ILog GetLogger<T>()
		{
			return GetLogger(typeof(T));
		}

		/// <summary>
		/// Gets the logger by calling <see cref="ILoggerFactoryAdapter.GetLogger(Type)"/>
		/// on the currently configured <see cref="Adapter"/> using the specified type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>the logger instance obtained from the current <see cref="Adapter"/></returns>
		public static ILog GetLogger(Type type)
		{
			if (_adapters.Count == 2)
			{
				var loggers = new List<ILog>(_adapters.Count);
				foreach (var a in _adapters) loggers.Add(a.GetLogger(type));
				return new MultiLogger(loggers);
			}
			else
			{
				return GetAdapter().GetLogger(type);
			}
		}


		/// <summary>
		/// Gets the logger by calling <see cref="ILoggerFactoryAdapter.GetLogger(string)"/>
		/// on the currently configured <see cref="Adapter"/> using the specified name.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>the logger instance obtained from the current <see cref="Adapter"/></returns>
		public static ILog GetLogger(string name)
		{
			if (_adapters.Count == 2)
			{
				var loggers = new List<ILog>(_adapters.Count);
				foreach (var a in _adapters) loggers.Add(a.GetLogger(name));
				return new MultiLogger(loggers);
			}
			else
			{
				return GetAdapter().GetLogger(name);
			}
		}


		/// <summary>
		/// Builds the logger factory adapter.
		/// </summary>
		/// <returns>a factory adapter instance. Is never <c>null</c>.</returns>
		private static List<ILoggerFactoryAdapter> BuildLoggerFactoryAdapters()
		{
			object sectionResult = null;

			ArgUtils.Guard(delegate
			{
				sectionResult = ConfigurationReader.GetSection(COMMON_LOGGING_SECTION);
			}
					, "Failed obtaining configuration for Common.Logging from configuration section 'common/logging'.");

			// configuration reader returned <null>
			if (sectionResult == null)
			{
				string message = (ConfigurationReader.GetType() == typeof(DefaultConfigurationReader))
														 ? string.Format("no configuration section <{0}> found - suppressing logging output", COMMON_LOGGING_SECTION)
														 : string.Format("Custom ConfigurationReader '{0}' returned <null> - suppressing logging output", ConfigurationReader.GetType().FullName);
				Trace.WriteLine(message);
				return new List<ILoggerFactoryAdapter>()
				{
					new NoOpLoggerFactoryAdapter()
				};
			}

			// ready to use ILoggerFactoryAdapter?
			if (sectionResult is ILoggerFactoryAdapter)
			{
				Trace.WriteLine(string.Format("Using ILoggerFactoryAdapter returned from custom ConfigurationReader '{0}'", ConfigurationReader.GetType().FullName));
				return new List<ILoggerFactoryAdapter>()
				{
					(ILoggerFactoryAdapter)sectionResult
				};
			}

			// ensure what's left is a LogSetting instance
			ArgUtils.Guard(delegate
			{
				ArgUtils.AssertIsAssignable<LogSetting>("sectionResult", sectionResult.GetType());
			}
										 , "ConfigurationReader {0} returned unknown settings instance of type {1}"
										 , ConfigurationReader.GetType().FullName, sectionResult.GetType().FullName);

			List<ILoggerFactoryAdapter> adapters = null;
			ArgUtils.Guard(delegate
			{
				adapters = BuildLoggerFactoryAdaptersFromLogSettings((LogSetting)sectionResult);
			}
					, "Failed creating LoggerFactoryAdapter from settings");

			return adapters;
		}

		/// <summary>
		/// Builds a <see cref="ILoggerFactoryAdapter"/> instance from the given <see cref="LogSetting"/>
		/// using <see cref="Activator"/>.
		/// </summary>
		/// <param name="setting"></param>
		/// <returns>the <see cref="ILoggerFactoryAdapter"/> instance. Is never <c>null</c></returns>
		private static List<ILoggerFactoryAdapter> BuildLoggerFactoryAdaptersFromLogSettings(LogSetting setting)
		{
			ArgUtils.AssertNotNull("setting", setting);
			// already ensured by LogSetting
			//            AssertArgIsAssignable<ILoggerFactoryAdapter>("setting.FactoryAdapterType", setting.FactoryAdapterType
			//                                , "Specified FactoryAdapter does not implement {0}.  Check implementation of class {1}"
			//                                , typeof(ILoggerFactoryAdapter).FullName
			//                                , setting.FactoryAdapterType.AssemblyQualifiedName);

			List<ILoggerFactoryAdapter> adapters = new List<ILoggerFactoryAdapter>();

			foreach (LogSetting.Entry settingEntry in setting.Entries)
			{
				ILoggerFactoryAdapter adapter = null;

				ArgUtils.Guard(delegate
				{
					if (settingEntry.Properties != null
							&& settingEntry.Properties.Count > 0)
					{
						object[] args = { settingEntry.Properties };

						adapter = (ILoggerFactoryAdapter)Activator.CreateInstance(settingEntry.FactoryAdapterType, args);
					}
					else
					{
						adapter = (ILoggerFactoryAdapter)Activator.CreateInstance(settingEntry.FactoryAdapterType);
					}
				}
								, "Unable to create instance of type {0}. Possible explanation is lack of zero arg and single arg NameValueCollection constructors"
								, settingEntry.FactoryAdapterType.FullName
				);

				// make sure
				ArgUtils.AssertNotNull("adapter", adapter, "Activator.CreateInstance() returned <null>");
				adapters.Add(adapter);
			}

			return adapters;
		}
	}
}