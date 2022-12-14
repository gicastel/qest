using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using YamlDotNet.Serialization;

namespace qest.Models
{
    public class TextArray
    {
        public ScriptType Type { get; set; }
        public List<string> Values { get; set; }
        
        /// <summary>
        /// Read every item as a whole in the collection
        /// </summary>
        /// <returns>A single element of the array</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public IEnumerable<string> ReadValue()
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

        /// <summary>
        /// Reads every item in the collection line by line
        /// </summary>
        /// <returns>A single line of the collection</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public IEnumerable<string> ReadLine()
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
                            while (sr.Peek() >= 0)
                            {
                                string data = sr.ReadLine();
                                yield return data;
                            }
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

    public class Scripts : List<TextArray>
    {
        [YamlIgnore]
        public List<string>? ActualScripts { get; set; }
        [YamlIgnore]
        public bool Result => ResultException is null;
        [YamlIgnore]
        public Exception? ResultException { get; set; }
    }
}
