1. Supabase 裡面的檢體 Table 是利用 UnitID 來判斷是哪一間醫院的檢體，所以如果這個專案要給其他單位使用，必須要先改 appSetting > DefaultUnitID
2. API Log 功能：專案已加入 HTTP 請求/回應記錄功能，所有進出的 API 都會記錄到專案根目錄的 `api_logs.txt` 檔案中

## API Log 功能設定步驟

如需在其他分支加入 API Log 功能，請依照以下步驟：

### 1. 建立 Middleware 檔案

#### 1.1 建立 API Request/Response Logging
在專案根目錄建立 `Middleware/RequestResponseLoggingMiddleware.cs`，內容如下：

```csharp
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
```

#### 1.2 建立 Supabase Request/Response Logging
在專案根目錄建立 `Middleware/SupabaseLoggingHandler.cs`，內容如下：

```csharp
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
```

### 2. 修改 Startup.cs

在 `Startup.cs` 檔案中加入以下修改：

**步驟 2.1：加入 using 宣告**
```csharp
using LIS_Middleware.Middleware;
```

**步驟 2.2：修改 ConfigureServices 方法，註冊 Supabase Client 並加入 Logging Handler**
```csharp
public void ConfigureServices(IServiceCollection services)
{
    // 註冊 Supabase Client with Logging Handler
    var supabaseUrl = Configuration["Supabase:Url"];
    var supabaseKey = Configuration["Supabase:ApiKey"];
    
    var options = new Supabase.SupabaseOptions
    {
        AutoConnectRealtime = false
    };
    
    // 建立帶有 logging 的 HttpClient
    var loggingHandler = new SupabaseLoggingHandler();
    var httpClient = new System.Net.Http.HttpClient(loggingHandler)
    {
        BaseAddress = new Uri(supabaseUrl)
    };
    httpClient.DefaultRequestHeaders.Add("apikey", supabaseKey);
    
    var supabaseClient = new Supabase.Client(supabaseUrl, supabaseKey, options);
    
    // 替換 Supabase 內部的 HttpClient
    var restClientField = typeof(Supabase.Client).GetField("_restClient", 
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    if (restClientField != null)
    {
        var restClient = restClientField.GetValue(supabaseClient);
        var httpClientField = restClient?.GetType().GetField("_httpClient", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (httpClientField != null)
        {
            httpClientField.SetValue(restClient, httpClient);
        }
    }
    
    supabaseClient.InitializeAsync().Wait();
    services.AddSingleton(supabaseClient);
    services.AddControllers();
}
```

**步驟 2.3：在 Configure 方法中註冊 API Logging Middleware**
在 `app.UseRouting();` 之前加入：
```csharp
// 加入 Request/Response 記錄 Middleware
app.UseMiddleware<RequestResponseLoggingMiddleware>();
```

完整的 Configure 方法應該如下：
```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    if (!env.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    // 加入 Request/Response 記錄 Middleware
    app.UseMiddleware<RequestResponseLoggingMiddleware>();

    app.UseRouting();

    app.UseAuthorization();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}
```

### 3. 編譯測試
```bash
dotnet build
```

### 4. Log 檔案說明
- **API Log 檔案名稱**：`api_logs.txt`
- **Supabase Log 檔案名稱**：`supabase_logs.txt`
- **檔案位置**：專案執行目錄（通常是專案根目錄或 publish 資料夾）
- **API Log 記錄內容**：
  - 請求時間戳記
  - HTTP Method、Path、QueryString
  - 所有 Request Headers
  - Request Body
  - Response StatusCode
  - 所有 Response Headers
  - Response Body
- **Supabase Log 記錄內容**：
  - Supabase API 請求時間戳記
  - HTTP Method、URI
  - Request/Response Headers（apikey 和 Authorization 會被遮蔽）
  - Request/Response Body（超過 10000 字元會被截斷）

### 注意事項
- Log 檔案會持續累加，建議定期清理
- Supabase Log 中的敏感資訊（apikey、Authorization）會被自動遮蔽為 [REDACTED]
- 檔案使用 lock 機制避免併發寫入衝突
- Supabase Response Body 超過 10000 字元會被截斷，避免檔案過大

---

發佈留意
1. 發布的指令：dotnet publish -c Release
2. 發布檔案的位置：bin > Release > publish

現代大甲 Supabase 連線API專案

現代大甲
"DefaultUnitID": "5647b1bb-fd7c-44b6-b57f-b30210231dd6"

現代台南
"DefaultUnitID": "56c493b8-d36d-4ba0-afbb-31f016e12ee1"

杏仁台南
"DefaultUnitID": "c068f09e-fdbc-4739-b97f-df58f92ca813"

屏東國仁
"DefaultUnitID": "0b48a248-c4c7-48ff-b81a-b0d534c6d16e"