using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using YamlDotNet.Serialization;

namespace qest.Models
{
    public class Script
    {
        public ScriptType Type { get; set; }
        public List<string> Values { get; set; }

        public IEnumerable<string> GetValues()
        {
            if (Values == null)
                throw new ArgumentNullException(nameof(Values));

            foreach (var value in Values)
            {
                switch (this.Type)
                {
                    case ScriptType.Inline:
                        yield return value;
                        break;
                    case ScriptType.File:

                        if (File.Exists(value))
                        {
                            using var sr = new StreamReader(value);
                            string data = sr.ReadToEnd();
                            yield return data;
                        }
                        else
                            throw new FileNotFoundException(null, value);
                        break;
                    default:
                        throw new ArgumentException(nameof(Type));
                }
            }

        }
    }

    public enum ScriptType
    {
        Inline,
        File
    }

    public class Scripts : List<Script>
    {
        [YamlIgnore]
        public List<string>? ActualScripts { get; set; }
        [YamlIgnore]
        public bool Result => ResultException is null;
        [YamlIgnore]
        public Exception? ResultException { get; set; }
    }
}
