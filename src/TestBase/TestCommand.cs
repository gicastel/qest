using System.Data;

namespace TestBase
{
    public class TestCommand
    {
        public string CommandText { get; set; }
        public List<TestCommandParameter> Parameters { get; set; }

        public TestCommand(string commandText)
        {
            CommandText = commandText;
        }

        public TestCommand() { }
    }

    public class TestCommandParameter
    {
        public string Name { get; set; }
        public SqlDbType Type { get; set; }
        public object? Value { get; set; }

        public TestCommandParameter (string name, SqlDbType type, object? value)
        {
            Name = name;
            Value = value;
            Type = type;
        }

        public TestCommandParameter () { }
    }

    public enum TestCommandParameterDirection
    {
        Input,
        Output,
        ReturnValue
    }
}
