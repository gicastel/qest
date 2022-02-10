using System.Data;

namespace TestBase
{
    public class TestCommand
    {
        public string CommandText { get; set; }
        public List<TestCommandParameter>? Parameters { get; set; }
    }

    public class TestCommandParameter
    {
        public string Name { get; set; }
        public SqlDbType Type { get; set; }
        public object? Value { get; set; }
    }
}
