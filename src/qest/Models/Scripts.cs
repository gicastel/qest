using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace qest.Models
{
    public class Scripts : List<Script>
    {
        public async Task RunAsync(Test parentTest, string scopeName)
        {
            var transaction = parentTest.Connection!.BeginTransaction();
            parentTest.Logger($"Running {scopeName} scripts...", null);
            try
            {
                foreach (var item in this)
                {
                    string script = item.Compact(parentTest.Variables);
                    if (script.Length > 0)
                    {
                        var cmd = new SqlCommand(script, parentTest.Connection, transaction);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            parentTest.Logger("Completed.", false);            
        }
    }

}
