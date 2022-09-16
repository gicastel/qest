using System.Data;

namespace TestBase
{
    public class TestCommand
    {
        public string CommandText { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
    }
}
