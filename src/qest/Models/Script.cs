﻿using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace qest.Models
{
    public class Script
    {
        public ScriptType Type { get; set; }
        public List<string> Values { get; set; }

        public string Compact(Dictionary<string, object>? variables)
        {
            if (Values == null)
                throw new ArgumentNullException(nameof(Values));

            switch (this.Type)
            {
                case ScriptType.Inline:
                    return string.Join(";", Values).ReplaceVars(variables);

                case ScriptType.File:
                    List<string> list = new List<string>();
                    foreach (var item in Values)
                    {
                        if (File.Exists(item))
                        {
                            using var sr = new StreamReader(item);
                            string data = sr.ReadToEnd();
                            list.Add(data);
                        }
                        else
                            throw new FileNotFoundException(null, item);
                    }
                    return string.Join(";", list).ReplaceVars(variables);
                default:
                    throw new ArgumentException(nameof(Type));
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
        public bool Result => ResultException is null;
        [YamlIgnore]
        public Exception? ResultException { get; set; }
    }
}
