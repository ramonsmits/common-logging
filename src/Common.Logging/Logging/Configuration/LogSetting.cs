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
using System.Collections.Specialized;

namespace Common.Logging.Configuration
{
    /// <summary>
    /// Container used to hold configuration information from config file.
    /// </summary>
    /// <author>Gilles Bayon</author>
    public class LogSetting
    {
			/// <summary>
			/// Setting entries
			/// </summary>
			public readonly List<Entry> Entries;

			/// <summary>
			/// Config entry
			/// </summary>
			public class Entry
			{
				/// <summary>
				/// The <see cref="ILoggerFactoryAdapter" /> type that will be used for creating <see cref="ILog" />
				/// instances.
				/// </summary>
				public readonly Type FactoryAdapterType;

				/// <summary>
				/// Additional user supplied properties that are passed to the <see cref="FactoryAdapterType" />'s constructor.
				/// </summary>
				public readonly NameValueCollection Properties;

				/// <summary>
				/// Initializes a new instance of the <see cref="Entry"/> class.
				/// </summary>
				/// <param name="factoryAdapterType">Type of the factory adapter.</param>
				/// <param name="properties">The properties.</param>
				public Entry(Type factoryAdapterType, NameValueCollection properties)
				{
					ArgUtils.AssertNotNull("factoryAdapterType", factoryAdapterType);
					ArgUtils.AssertIsAssignable<ILoggerFactoryAdapter>("factoryAdapterType", factoryAdapterType
							, "Type {0} does not implement {1}", FactoryAdapterType.AssemblyQualifiedName, typeof(ILoggerFactoryAdapter).FullName);

					FactoryAdapterType = factoryAdapterType;
					Properties = properties;
				}
			}

			/// <summary>
			/// Initializes a new instance of the <see cref="LogSetting"/> class.
			/// </summary>
			/// <param name="entries">The entries.</param>
        public LogSetting(List<Entry> entries)
        {
					Entries = entries;
        }
    }
}