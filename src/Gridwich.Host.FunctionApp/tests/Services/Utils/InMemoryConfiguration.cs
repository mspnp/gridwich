using System;
using System.Collections.Generic;
using System.IO;

using Gridwich.Core.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace Gridwich.Host.FunctionAppTests.Services.Utils
{
    /// <summary>
    /// For testing dependency injection (DI) configuration correctness -- an IConfiguration
    /// class suitable for substitution for the normal Gridwich one.  This one:<ul>
    /// <li>is loaded from a JSON file (like Gridwich.Host.FunctionApp/src/sample.local.settings.json)
    /// <li>supports additional name/value customization - additions/overrides/deletions
    /// </ul>
    /// </summary>
    /// <remarks>
    /// This class is needed for the ServiceConfigurationTests in order to accomodate the
    /// Dependency Injection's need to activate service instances when processing a
    /// GetServices<T> call.  With the default IConfiguration, some unrelated (i.e. not
    /// being tested) classes, like the TeleStream provider will fall over due to a lack
    /// of any value for TELESTREAMCLOUD_API_KEY (for instance), which breaks the EventHandler-related
    /// test (in ServiceConfigurationTests), which are using the DI machinery.
    ///
    /// This class is used to plug into a SettingsProvider and substitute that into the
    /// DI registrations, keeping classes like TeleStream happy enough to initialize,
    /// permitting the testing of the EventHandler-related tests to execute.
    /// </remarks>
    internal class InMemoryConfiguration : IConfiguration
    {
        /// <summary>
        /// The key/value (string/string) pairs of values for the IConfiguration to present.
        /// </summary>
        private Dictionary<string, string> _variables = new Dictionary<string, string>(100);

        /// <summary>
        /// Private constructor -- nothing to do, but restrict to static ConfigFromJSONSettingsFile for creation.
        /// </summary>
        private InMemoryConfiguration()
        {
            // nothing to do.
        }

        /// <summary>
        /// Load a JSON settings file (see remarks for format) and optional adjustments into an
        /// IConfiguration instance.
        /// </summary>
        /// <param name="jsonSettingsFileName">The path to a JSON file (see remarks for format) whose
        /// proerties should be loaded as the key/value pairs of the IConfiguration.</param>
        /// <typeparam name="overrides">An optional dictionary of adjustments to be made to the set of
        /// name/value pairs loaded from the JSON file.  For each key in this overrides dictionary,
        /// if the value is non-null, it is added to (or updated in) the set of pairs loaded from the JSON file.
        /// If the value is null, the corresponding key will be deleted, if present.</typeparam>
        /// <returns>
        /// An IConfiguration instance, with the values loaded as described above.
        /// </returns>
        /// <remarks>
        /// The format of the JSON file must be that the properties for the name/value pair must appear
        /// as a top-level property named "Values". e.g.
        ///      { "Values": { "key1": "value1", "AnotherKey": "anotherValue", ... } }
        /// Any top level properties other than "Values" are ignored.  See
        /// src/Gridwich.Host.FunctionApp/src/sample.local.settings.json for an example.
        /// </remarks>
        public static IConfiguration ConfigFromJSONSettingsFile(string jsonSettingsFileName, IDictionary<string, string> overrides = null)
        {
            if (string.IsNullOrWhiteSpace(jsonSettingsFileName))
            {
                throw new ArgumentException($"Empty or null {nameof(jsonSettingsFileName)} JSON file location", nameof(jsonSettingsFileName));
            }

            var dict = new Dictionary<string, string>(50);

            // Pull in the JSON file.  This may throw a number of System.IO exceptions if there's a problem reading/locating
            // the named file.  Best to let them propogate as it's indicative of a test setup issue that should be
            // immediately addressed.
            var jsonText = File.ReadAllText(jsonSettingsFileName);
            // Same here, just Newtonsoft exceptions possible.
            var jo = JObject.Parse(jsonText);

            // the top level key under which all the properties are stored in the json file.
            const string values = "Values";

            var jov = jo[values];

            // load each of the properties under Values.
            foreach (var item in jov.Children())
            {
                JProperty prop = item.ToObject<JProperty>();
                var name = prop.Name;
                var val = prop.Value.ToString();
                dict[name] = val;
            }

            // Process overrides on top of those loaded from the JSON
            if (overrides != null)
            {
                foreach (var item in overrides)
                {
                    var name = item.Key;
                    var val = item.Value;

                    if (val == null)
                    {
                        // remove the key if it's present.
                        if (dict.ContainsKey(name))
                        {
                            dict.Remove(name);
                        }
                    }
                    else
                    {
                        dict[name] = val;
                    }
                }
            }

            // Load up an instance & we're done.
            var cfg = new InMemoryConfiguration();
            cfg.AddVariables(dict, clearFirst: false);

            return cfg;
        }

        /// <summary>
        /// Forget all keys.
        /// </summary>
        private void ClearVariables()
        {
            _variables.Clear();
        }

        /// <summary>
        /// Load up name/value pairs, optionally clearing out any current content before additions.
        /// </summary>
        private void AddVariables(IDictionary<string, string> dict, bool clearFirst = false)
        {
            if (clearFirst)
            {
                ClearVariables();
            }

            foreach (var dItem in dict)
            {
                _variables[dItem.Key] = dItem.Value;
            }
        }

        #region IConfiguration Implementation

        /// <summary>
        /// Part of IContext.
        /// Return a configuration section with the specified key as the Value property.
        /// If the key does not exist, return a configuration section with a Value
        /// property of null.
        /// </summary>
        /// <remarks>
        /// This method is at the bottom of the call-chain when application code calls
        /// methods like GetValue "on" the IConfiguration.  The quotes around on
        /// reflect the fact that GetValue is really a .NET extension method which
        /// ultimately calls GetSection here.  This is why there is no overt GetValue
        /// method implementation herein.
        /// </remarks>
        public IConfigurationSection GetSection(string key)
        {
            var theValue = this[key];
            var result = new TestConfigurationSection(theValue);
            return result;
        }

        /// <summary>Part of IConfiguration</summary>
        public string this[string key]
        {
            get
            {
                if (_variables.ContainsKey(key))
                {
                    return _variables[key];
                }
                return null; // ConfigurationSection/Root return null, so playing along.
            }
            set
            {
                _variables[key] = value;
            }
        }

        /// <summary>Part of IConfiguration</summary>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return new List<IConfigurationSection>();
        }

        /// <summary>Part of IConfiguration</summary>
        public IChangeToken GetReloadToken()
        {
            return null;
        }
        #endregion /* IConfiguration */
    }

    /// <summary>
    /// A unit-test stand-in class for the HostEnvironment provided by Azure Functions.
    /// It doesn't do a lot, but it will suffice in the dependency injection (DI) list when
    /// some class pulls IHostingEnvironment during unit tests.  ServiceConfigurationTests
    /// uses this class to ensure that the DI list contains at least one IHostingEnvironment
    /// during it's unit testing.
    /// </summary>
    public class DummyHostEnvironment : IHostingEnvironment
    {
        public DummyHostEnvironment()
        {
            EnvironmentName = "Development";
            ApplicationName = "Gridwich";
            WebRootPath = "/a/b/c";
            WebRootFileProvider = new NullFileProvider();
            ContentRootFileProvider = new NullFileProvider();
            ContentRootPath = "/";
        }

        #region IHostingEnvironment
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
        public string ContentRootPath { get; set; }
        #endregion /* IHostingEnvironment */
    }
}