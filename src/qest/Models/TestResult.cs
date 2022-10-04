using System.Collections.Generic;
using System.Data;

namespace qest.Models
{

    public class ResultGroup
    {
        public List<ResultSet>? ResultSets { get; set; }
        public List<OutputParameter>? OutputParameters { get; set; }
        public int? ReturnCode { get; set; }
    }

    public class ResultSet
    {
        public string Name { get; set; }
        public List<Column> Columns { get; set; }
        public int? RowNumber { get; set; }

        public DataTable GetDataTable()
        {
            DataTable dt = new(Name);
            foreach (var col in Columns)
                dt.Columns.Add(new DataColumn(col.Name, Utils.MapType(col.Type)));
            return dt;
        }
    }

    public class OutputParameter
    {
        public string Name { get; set; }
        public SqlDbType Type { get; set; }
        public object Value { get; set; }
    }

    public class Column
    {
        public string Name { get; set;}
        public SqlDbType Type { get; set; }
    }

}
