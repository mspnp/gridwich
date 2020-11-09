using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Gridwich.Host.FunctionAppTests.Services.Utils
{
    /// <summary>
    /// Test class to mock the IConfiguration call to GetValue<T>. This is because we cannot mock
    /// GetValue directly, so we mock GetSection which returns a IConfigurationSection.
    /// </summary>
    internal class TestConfigurationSection : IConfigurationSection
    {
        public TestConfigurationSection(string value)
        {
            Value = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            throw new System.NotImplementedException();
        }

        public IChangeToken GetReloadToken()
        {
            throw new System.NotImplementedException();
        }

        public IConfigurationSection GetSection(string key)
        {
            throw new System.NotImplementedException();
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