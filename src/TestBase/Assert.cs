using System.Data;

namespace TestBase
{
    public class Assert
    {
        public string SqlQuery { get; set; }
        public SqlDbType ScalarType { get; set; }
        public object ScalarValue { get; set; }
    }
}
