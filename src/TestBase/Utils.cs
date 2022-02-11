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
            SqlDbType.Bit => typeof(bool),
            SqlDbType.TinyInt => typeof(byte),
            SqlDbType.SmallInt => typeof(short),
            SqlDbType.BigInt => typeof(Int64),
            SqlDbType.Float => typeof(double),
            SqlDbType.Int => typeof(int),
            SqlDbType.NVarChar => typeof(string),
            SqlDbType.DateTime => typeof(DateTime),
            SqlDbType.DateTime2 => typeof(DateTime),
            SqlDbType.Date => typeof(DateTime),
            SqlDbType.DateTimeOffset => typeof(DateTimeOffset),
            SqlDbType.Time => typeof(TimeSpan),
            SqlDbType.Real => typeof(Single),
            SqlDbType.Decimal => typeof(Decimal),
            SqlDbType.Money => typeof(Decimal),

            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Type not expected: {type}"),
        };
    }
}