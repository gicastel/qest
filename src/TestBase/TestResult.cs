using System.Data;

namespace TestBase
{

    public class ResultGroup
    {
        public List<ResultSet>? ResultSets { get; set; }
        public List<OutputParameter>? OutputParameters { get; set; }
        public int? ReturnCode { get; set; }

        public ResultGroup() { }
    }

    public class ResultSet
    {
        public string? Name { get; set; }
        public List<Column>? Columns { get; set; }
        public int? RowNumber { get; set; }
        public ResultSet() { }
    }

    public class OutputParameter
    {
        public string Name { get; set; }
        public SqlDbType Type { get; set; }
        public object Value { get; set; }
        public OutputParameter() { }
    }

    public class Column
    {
        public string Name { get; set;}
        public SqlDbType Type { get; set; }

        public Column(string colName, SqlDbType colType)
        {
            Name = colName;
            Type = colType;
        }

        public Column() { }
    }

}
