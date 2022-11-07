using System.Collections.Generic;
using System.Linq;
using Spectre.Console;

namespace qest
{
    internal static partial class Utils
    {
        internal static object? ReplaceVarsInParameter(this object value, Dictionary<string, object>? variables)
        {
            if (variables != null && value is string stringValue)
            {
                var result = stringValue.ReplaceVars(variables);
                return result != "NULL" ? result : null;
            }
            else
            {
                return value;
            }
        }

        internal static string ReplaceVars(this string value, Dictionary<string, object>? variables)
        {
            if (variables != null)
            {
                return variables.Aggregate(value, (acc, var) => acc.Replace($"{{{var.Key}}}", var.Value?.ToString() ?? "NULL"));
            }
            else
            {
                return value;
            }
        }

        internal static string EscapeAndAddStyles(this string message, string? customStyle)
        {
            message = message.EscapeMarkup();

            if (customStyle is not null)
                foreach (string style in customStyle.Split(','))
                    message = $"[{style}]{message}[/]";

            return message;
        }

        internal static void RemoveLast<T>(this List<T> list)
        {
            if (list.Count > 0)
                list.RemoveAt(list.Count-1);
            else
                throw new System.NotSupportedException();
        }
    }
}
