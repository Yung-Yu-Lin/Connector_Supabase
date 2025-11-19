using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LIS_Middleware.Middleware
{
    public class SupabaseLoggingHandler : DelegatingHandler
    {
        private static readonly string _logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "supabase_logs.txt");
        private static readonly object _fileLock = new object();

        public SupabaseLoggingHandler() : base(new HttpClientHandler())
        {
        }

        public SupabaseLoggingHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var requestLog = await LogRequest(request);

            var response = await base.SendAsync(request, cancellationToken);

            var responseLog = await LogResponse(response);

            WriteLog(requestLog, responseLog);

            return response;
        }

        private async Task<string> LogRequest(HttpRequestMessage request)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"========== SUPABASE REQUEST [{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ==========");
            sb.AppendLine($"Method: {request.Method}");
            sb.AppendLine($"URI: {request.RequestUri}");
            
            sb.AppendLine("Headers:");
            foreach (var header in request.Headers)
            {
                // 遮蔽敏感資訊
                if (header.Key.Equals("apikey", StringComparison.OrdinalIgnoreCase) || 
                    header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"  {header.Key}: [REDACTED]");
                }
                else
                {
                    sb.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                }
            }

            if (request.Content != null)
            {
                sb.AppendLine("Content Headers:");
                foreach (var header in request.Content.Headers)
                {
                    sb.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                }

                var content = await request.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    sb.AppendLine($"Body: {content}");
                }
            }

            return sb.ToString();
        }

        private async Task<string> LogResponse(HttpResponseMessage response)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"========== SUPABASE RESPONSE [{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ==========");
            sb.AppendLine($"StatusCode: {(int)response.StatusCode} {response.StatusCode}");
            
            sb.AppendLine("Headers:");
            foreach (var header in response.Headers)
            {
                sb.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
            }

            if (response.Content != null)
            {
                sb.AppendLine("Content Headers:");
                foreach (var header in response.Content.Headers)
                {
                    sb.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                }

                var content = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    // 限制 body 長度避免檔案過大
                    if (content.Length > 10000)
                    {
                        sb.AppendLine($"Body: {content.Substring(0, 10000)}... [TRUNCATED - Total Length: {content.Length}]");
                    }
                    else
                    {
                        sb.AppendLine($"Body: {content}");
                    }
                }
            }

            sb.AppendLine("==================================================");
            sb.AppendLine();

            return sb.ToString();
        }

        private void WriteLog(string requestLog, string responseLog)
        {
            try
            {
                lock (_fileLock)
                {
                    File.AppendAllText(_logFilePath, requestLog + responseLog);
                }
            }
            catch (Exception)
            {
                // 靜默處理，避免影響主流程
            }
        }
    }
}
