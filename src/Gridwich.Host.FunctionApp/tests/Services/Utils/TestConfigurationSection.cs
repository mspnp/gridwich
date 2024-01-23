using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Gridwich.Host.FunctionAppTests.Services.Utils
{
    /// <summary>
    /// Test class to mock the IConfiguration call to GetValue<T>. This is because we cannot mock
    /// GetValue directly, so we mock GetSection which returns a IConfigurationSection.
    /// </summary>
    internal class TestConfigurationSection : IConfigurationSection
    {
        public TestConfigurationSection(string value, string key = "")
        {
            Key = key;
            Value = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            var result = new List<IConfigurationSection>();

            var section = JObject.Parse(Value);
            foreach (var element in section)
            {
                result.Add(new TestConfigurationSection((string)element.Value, element.Key));
            }

            return result;
        }

        public IChangeToken GetReloadToken()
        {
            throw new System.NotImplementedException();
        }

        public IConfigurationSection GetSection(string key)
        {
            var section = JObject.Parse(Value);
            var value = section[key];
            return new TestConfigurationSection((string)value, key);
        }

        public string this[string key]
        {
            get =>
                throw new System.NotImplementedException();
            set =>
                throw new System.NotImplementedException();
        }

        public string Key { get; }
        public string Path { get; }
        public string Value { get; set; }
    }
}