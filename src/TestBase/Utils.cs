using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBase
{
    internal static class Utils
    {
        internal static Type MapType(SqlDbType type) => type switch
        {
            SqlDbType.Int => typeof(int),
            SqlDbType.NVarChar => typeof(string),
            SqlDbType.DateTime => typeof(DateTime),
            SqlDbType.DateTime2 => typeof(DateTime),
            SqlDbType.Date => typeof(DateTime),
            SqlDbType.DateTimeOffset => typeof(DateTime),
            SqlDbType.Bit => typeof(bool),
            SqlDbType.SmallInt => typeof(short),
            SqlDbType.Time => typeof(TimeSpan),
            SqlDbType.Float => typeof(Double),
            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Type not expected: {type}"),
        };
    }
}