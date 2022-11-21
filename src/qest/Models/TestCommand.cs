using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace qest.Models
{
    public class TestCommand
    {
        public string CommandText { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }

        [YamlIgnore]
        public List<string> ActualParameters { get; set; }
        [YamlIgnore]
        public bool Result => ResultException is null;
        [YamlIgnore]
        public Exception ResultException { get; set; }
    }
}
