using System;
using System.Data.SqlClient;
using Spectre.Console;

namespace qest.Commands
{
    internal static class Validators
    {
        internal static ValidationResult ValidateConnectionString(string ConnectionString)
        {
            if (ConnectionString is null || ConnectionString.Length < Utils.ConnectionStringBarebone.Length)
                return ValidationResult.Error("Connection string not supplied or too short (are you missing any keyword?)");
            else
            {
                var sqlConnection = new SqlConnection(ConnectionString);
                try
                {
                    sqlConnection.Open();
                    sqlConnection.Close();
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                    return ValidationResult.Error("Connection to the targed database failed.");
                }
            }

            return ValidationResult.Success();
        }
    }
}
