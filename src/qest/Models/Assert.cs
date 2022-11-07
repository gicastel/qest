using System;
using System.Data;
using YamlDotNet.Serialization;

namespace qest.Models
{
    public class Assert
    {
        public string SqlQuery { get; set; }
        public qestType ScalarType { get; set; }
        public object ScalarValue { get; set; }

        [YamlIgnore]
        public object? Result { get; set; }
        [YamlIgnore]
        public Exception? ResultException { get; set; }
    }
}
