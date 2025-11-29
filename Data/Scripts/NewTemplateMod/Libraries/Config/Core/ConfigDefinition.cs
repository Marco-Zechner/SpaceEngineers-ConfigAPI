using System;
using mz.Config.Abstractions;
using mz.Config.Domain;

namespace mz.Config.Core
{
    /// <summary>
    /// Minimal generic definition used internally by ConfigStorage.
    /// TypeName is always typeof(T).Name; SectionName is TypeName.
    /// </summary>
    public sealed class ConfigDefinition<T> : IConfigDefinition
        where T : ConfigBase, new()
    {
        public ConfigDefinition(string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName))
                throw new ArgumentNullException("sectionName");

            TypeName = typeof(T).Name;
            SectionName = sectionName;
        }

        public string TypeName { get; }

        public string SectionName { get; }

        public Type ConfigType => typeof(T);

        public ConfigBase CreateDefaultInstance() => new T();
    }
}
