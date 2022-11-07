using System;
using qest.Models;

namespace qest
{
    internal static partial class Utils
    {
        internal static Type MapQestTypeToInternal(qestType type) => type switch
        {
            qestType.Bit => typeof(bool),
            qestType.TinyInt => typeof(byte),
            qestType.SmallInt => typeof(short),
            qestType.BigInt => typeof(long),
            qestType.Float => typeof(double),
            qestType.Int => typeof(int),
            qestType.NVarChar => typeof(string),
            qestType.DateTime => typeof(DateTime),
            qestType.DateTime2 => typeof(DateTime),
            qestType.Date => typeof(DateTime),
            qestType.DateTimeOffset => typeof(DateTimeOffset),
            qestType.Time => typeof(TimeSpan),
            qestType.Real => typeof(float),
            qestType.Decimal => typeof(decimal),
            qestType.Money => typeof(decimal),

            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Type not expected: {type}"),
        };
    }
}
