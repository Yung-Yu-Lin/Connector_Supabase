using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LIS_Middleware.Middleware
{
    public class SupabaseLogger
    {
        private static readonly string _logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "supabase_logs.txt");
        private static readonly object _fileLock = new object();

        public static void LogQuery(string operation, string table, object filters, object result = null)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"========== SUPABASE OPERATION [{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ==========");
                sb.AppendLine($"Operation: {operation}");
                sb.AppendLine($"Table: {table}");
                
                if (filters != null)
                {
                    sb.AppendLine($"Filters/Parameters: {JsonConvert.SerializeObject(filters, Formatting.Indented)}");
                }
                
                if (result != null)
                {
                    var resultJson = JsonConvert.SerializeObject(result, Formatting.Indented);
                    if (resultJson.Length > 10000)
                    {
                        sb.AppendLine($"Result: {resultJson.Substring(0, 10000)}... [TRUNCATED - Total Length: {resultJson.Length}]");
                    }
                    else
                    {
                        sb.AppendLine($"Result: {resultJson}");
                    }
                }
                
                sb.AppendLine("==================================================");
                sb.AppendLine();

                lock (_fileLock)
                {
                    File.AppendAllText(_logFilePath, sb.ToString());
                }
            }
            catch (Exception ex)
            {
                // 靜默處理，避免影響主流程
                Console.WriteLine($"Failed to log Supabase operation: {ex.Message}");
            }
        }

        public static void LogError(string operation, string table, Exception ex)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"========== SUPABASE ERROR [{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ==========");
                sb.AppendLine($"Operation: {operation}");
                sb.AppendLine($"Table: {table}");
                sb.AppendLine($"Error: {ex.Message}");
                sb.AppendLine($"StackTrace: {ex.StackTrace}");
                sb.AppendLine("==================================================");
                sb.AppendLine();

                lock (_fileLock)
                {
                    File.AppendAllText(_logFilePath, sb.ToString());
                }
            }
            catch
            {
                // 靜默處理
            }
        }
    }
}
