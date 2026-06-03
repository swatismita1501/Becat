using System;
using System.Text;

namespace EcatDesktop.Services
{
    internal static class CsvUtility
    {
        public static string[] ParseLine(string line)
        {
            var values = new System.Collections.Generic.List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];

                if (ch == '"' && inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                    continue;
                }

                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (ch == ',' && !inQuotes)
                {
                    values.Add(current.ToString());
                    current.Length = 0;
                    continue;
                }

                current.Append(ch);
            }

            values.Add(current.ToString());
            return values.ToArray();
        }

        public static string Escape(string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            if (value.IndexOf('"') >= 0 || value.IndexOf(',') >= 0 || value.IndexOf('\n') >= 0 || value.IndexOf('\r') >= 0)
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }

        public static string DateOrEmpty(DateTime? date)
        {
            return date.HasValue ? date.Value.ToString("MM/dd/yyyy") : string.Empty;
        }

        public static DateTime? ParseDate(string value)
        {
            DateTime parsed;
            if (DateTime.TryParse(value, out parsed))
            {
                return parsed.Date;
            }

            return null;
        }
    }
}
