using System.Collections.Generic;

namespace qest.Models
{
    public class TestCommand
    {
        public string CommandText { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
    }
}
