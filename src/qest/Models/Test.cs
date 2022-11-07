using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace qest.Models
{
    public class Test
    {
        public string Name { get; set; }
        public Scripts? Before { get; set; }
        public List<TestStep> Steps { get; set; }
        public Scripts? After { get; set; }
        public Dictionary<string, object>? Variables { get; set; }

        [YamlIgnore]
        public Exception? ResultException { get; set; }
    }
}
