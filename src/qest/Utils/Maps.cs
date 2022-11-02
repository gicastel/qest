using System;
using System.Data;

namespace qest
{
    internal static partial class Utils
    {
        internal static Type MapType(SqlDbType type) => type switch
        {
            SqlDbType.Bit => typeof(bool),
            SqlDbType.TinyInt => typeof(byte),
            SqlDbType.SmallInt => typeof(short),
            SqlDbType.BigInt => typeof(long),
            SqlDbType.Float => typeof(double),
            SqlDbType.Int => typeof(int),
            SqlDbType.NVarChar => typeof(string),
            SqlDbType.DateTime => typeof(DateTime),
            SqlDbType.DateTime2 => typeof(DateTime),
            SqlDbType.Date => typeof(DateTime),
            SqlDbType.DateTimeOffset => typeof(DateTimeOffset),
            SqlDbType.Time => typeof(TimeSpan),
            SqlDbType.Real => typeof(float),
            SqlDbType.Decimal => typeof(decimal),
            SqlDbType.Money => typeof(decimal),

            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Type not expected: {type}"),
        };

        internal static SqlDbType MapSqlType(string type) => type switch
        {
            "bit" => SqlDbType.Bit,
            "tinyint" => SqlDbType.TinyInt,
            "smallint" => SqlDbType.SmallInt,
            "bigint" => SqlDbType.BigInt,
            "float" => SqlDbType.Float,
            "int" => SqlDbType.Int,
            "nvarchar" => SqlDbType.NVarChar,
            "datetime" => SqlDbType.DateTime,
            "datetime2" => SqlDbType.DateTime2,
            "date" => SqlDbType.Date,
            "datetimetoffset" => SqlDbType.DateTimeOffset,
            "time" => SqlDbType.Time,
            "real" => SqlDbType.Real,
            "decimal" => SqlDbType.Decimal,
            "money" => SqlDbType.Decimal,

            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Type not expected: {type}"),
        };
    }
}
