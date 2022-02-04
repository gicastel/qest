using System.Data;

namespace TestBase
{

    public class ResultGroup
    {
        public List<ResultSet>? ResultSets { get; set; }
        public List<OutputParameter>? OutputParameters { get; set; }
        public ReturnCode? ReturnCode { get; set; }

        public ResultGroup() { }
    }

    public class ResultSet
    {
        public string? Name { get; set; }
        public List<ResultSetField>? ResultsetDefinition { get; set; }
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

    public class ReturnCode
    {
        public string Name { get; set; }
        public int Value { get; set; }

        public ReturnCode() { }
    }


    public class ResultSetField
    {
        public string FieldName { get; set;}
        public SqlDbType FieldType { get; set; }

        public ResultSetField(string fieldName, SqlDbType fieldType)
        {
            FieldName = fieldName;
            FieldType = fieldType;
        }

        public ResultSetField() { }
    }

}
