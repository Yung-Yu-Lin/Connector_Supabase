using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LIS_Middleware.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
        private static readonly string _logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "api_logs.txt");
        private static readonly object _fileLock = new object();

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 記錄請求
            var requestLog = await LogRequest(context.Request);
            
            // 暫存原始的 Response Body Stream
            var originalBodyStream = context.Response.Body;

            try
            {
                using (var responseBody = new MemoryStream())
                {
                    context.Response.Body = responseBody;

                    // 執行下一個 middleware
                    await _next(context);

                    // 記錄回應
                    var responseLog = await LogResponse(context.Response);
                    
                    // 寫入檔案
                    WriteLog(requestLog, responseLog);

                    // 將 response 複製回原始 stream
                    await responseBody.CopyToAsync(originalBodyStream);
                }
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private async Task<string> LogRequest(HttpRequest request)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"========== REQUEST [{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ==========");
            sb.AppendLine($"Method: {request.Method}");
            sb.AppendLine($"Path: {request.Path}");
            sb.AppendLine($"QueryString: {request.QueryString}");
            sb.AppendLine($"Scheme: {request.Scheme}");
            sb.AppendLine($"Host: {request.Host}");
            
            sb.AppendLine("Headers:");
            foreach (var header in request.Headers)
            {
                sb.AppendLine($"  {header.Key}: {header.Value}");
            }

            // 讀取 Request Body
            request.EnableBuffering();
            var buffer = new byte[Convert.ToInt32(request.ContentLength ?? 0)];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            var bodyAsText = Encoding.UTF8.GetString(buffer);
            request.Body.Position = 0; // 重設位置以便後續讀取

            if (!string.IsNullOrWhiteSpace(bodyAsText))
            {
                sb.AppendLine($"Body: {bodyAsText}");
            }

            return sb.ToString();
        }

        private async Task<string> LogResponse(HttpResponse response)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"========== RESPONSE [{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ==========");
            sb.AppendLine($"StatusCode: {response.StatusCode}");
            
            sb.AppendLine("Headers:");
            foreach (var header in response.Headers)
            {
                sb.AppendLine($"  {header.Key}: {header.Value}");
            }

            // 讀取 Response Body
            response.Body.Seek(0, SeekOrigin.Begin);
            var bodyAsText = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            if (!string.IsNullOrWhiteSpace(bodyAsText))
            {
                sb.AppendLine($"Body: {bodyAsText}");
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write log to file");
            }
        }
    }
}
