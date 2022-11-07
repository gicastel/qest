using System;
using System.Collections.Generic;
using System.Data;
using YamlDotNet.Serialization;

namespace qest.Models
{

    public class ResultGroup
    {
        public List<ResultSet>? ResultSets { get; set; }
        public List<OutputParameter>? OutputParameters { get; set; }
        public int? ReturnCode { get; set; }

        [YamlIgnore]
        public int? ReturnCodeResult { get; set; }
        [YamlIgnore]
        public Exception? ReturnCodeResultException { get; set; }
    }

    public class ResultSet
    {
        public string Name { get; set; }
        public List<Column> Columns { get; set; }
        public int? RowNumber { get; set; }

        [YamlIgnore]
        public DataTable? Result { get; set; }
        [YamlIgnore]
        public Exception? ResultException { get; set; }

        public void CreateDataTable()
        {
            Result = new(Name);
            foreach (var col in Columns)
                Result.Columns.Add(new DataColumn(col.Name, Utils.MapQestTypeToInternal(col.Type)));
        }
    }

    public class OutputParameter
    {
        public string Name { get; set; }
        public qestType Type { get; set; }
        public object Value { get; set; }

        [YamlIgnore]
        public object? Result { get; set; }
        [YamlIgnore]
        public Exception? ResultException { get; set; }
    }

    public class Column
    {
        public string Name { get; set;}
        public qestType Type { get; set; }
    }

}
