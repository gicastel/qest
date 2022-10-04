using System.Data;

namespace qest.Models
{
    public class Assert
    {
        public string SqlQuery { get; set; }
        public SqlDbType ScalarType { get; set; }
        public object ScalarValue { get; set; }
    }
}
