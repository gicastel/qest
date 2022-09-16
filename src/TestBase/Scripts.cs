using System.Data.SqlClient;

namespace TestBase
{
    public class Scripts : List<Script>
    {
        public void Run(Test parentTest, string scopeName)
        {
            var loadData = parentTest.Connection.BeginTransaction();
            try
            {
                foreach (var item in this)
                {
                    string script = item.Compact(parentTest.Variables);
                    if (script.Length > 0)
                    {
                        parentTest.ReportAdd($"Running {scopeName} scripts...");
                        var cmd = new SqlCommand(script, parentTest.Connection, loadData);
                        cmd.ExecuteNonQuery();
                        parentTest.ReportAdd("Completed.", false);
                    }
                }
                loadData.Commit();
            }
            catch
            {
                loadData.Rollback();
                throw;
            }
        }
    }

}
