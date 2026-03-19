using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using LIS_Middleware.Models;
using Microsoft.Extensions.Configuration;

namespace LIS_Middleware.Controllers
{
    public class DXI_ExamineItems
    {
        public static string CA153 = "CA153";
        public static string HCG5 = "B-HCG";
        public static string LH = "LH";
        public static string CA125 = "CA125";
        public static string PSA = "PSA";
        public static string PRL = "PRL";
        public static string freePSA = "freePSA";
        public static string FSH = "FSH";
        public static string CEA = "CEA";
        public static string TES = "TES";
        public static string B12 = "B12";
        public static string AFP = "AFP";
        public static string INS2 = "INS2";
        public static string CA199 = "CA199";
        public static string FOL = "FOL";
        public static string E2 = "E2";
    }

    [Route("Access2")]
    public class BeckmanDXIController : Controller
    {
        // 這裡定義的是，Supabase 裡面對應的檢驗項目代碼 (test_code) test_code 必須要在這裡面有出現才會被 select 出來
        private static readonly string[] ExamineItems = new[]
        {
            "CA153",
            "B-HCG",
            "LH",
            "CA125",
            "PSA",
            "PRL",
            "freePSA",
            "FSH",
            "CEA",
            "TES",
            "B12",
            "AFP",
            "INS2",
            "CA199",
            "FOL",
            "E2"
        };

        // 反向字典：DXI 代碼 → ItemID
        Dictionary<string, string> DXICodeToItemID => DXI_ExamineItems_Dic.ToDictionary(x => x.Value, x => x.Key);

        Dictionary<string, string> DXI_ExamineItems_Dic = new Dictionary<string, string>()
        {
            { DXI_ExamineItems.CA153, "BR15-3Ag" },
            { DXI_ExamineItems.HCG5, "HCG5" },
            { DXI_ExamineItems.LH, "hLH" },
            { DXI_ExamineItems.CA125, "OV125Ag" },
            { DXI_ExamineItems.PSA, "PSA-Hyb" },
            { DXI_ExamineItems.PRL, "PRL" },
            { DXI_ExamineItems.freePSA, "freePSA" },
            { DXI_ExamineItems.FSH, "hFSH" },
            { DXI_ExamineItems.CEA, "CEA2" },
            { DXI_ExamineItems.TES, "Testo" },
            { DXI_ExamineItems.B12, "VitB12" },
            { DXI_ExamineItems.AFP, "AFP" },
            { DXI_ExamineItems.INS2, "Insulin" },
            { DXI_ExamineItems.CA199, "GI19-9Ag" },
            { DXI_ExamineItems.FOL, "FOLW" },
            { DXI_ExamineItems.E2, "SNSE2" }
        };

        private readonly Supabase.Client _supabaseClient;
        private readonly IConfiguration _configuration;

        public BeckmanDXIController(Supabase.Client supabaseClient, IConfiguration configuration)
        {
            _supabaseClient = supabaseClient;
            _configuration = configuration;
        }

        // 日誌記錄輔助方法
        private void LogApiCall(string endpoint, string method, object requestBody, object responseData, bool success, string message, DateTime startTime)
        {
            try
            {
                var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "log");
                var logFileName = $"{startTime:yyyyMMdd}_log.txt";
                var logFilePath = Path.Combine(logDirectory, logFileName);
                
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                
                var endTime = DateTime.Now;
                var duration = (endTime - startTime).TotalMilliseconds;
                
                var logEntry = $"[{startTime:yyyy-MM-dd HH:mm:ss}] REQUEST - {endpoint}" + Environment.NewLine;
                logEntry += $"Method: {method}" + Environment.NewLine;
                logEntry += $"Endpoint: {endpoint}" + Environment.NewLine;
                logEntry += $"Request Body: {(requestBody != null ? System.Text.Json.JsonSerializer.Serialize(requestBody) : "N/A")}" + Environment.NewLine;
                logEntry += $"RESPONSE - Status: {success}, Message: {message}" + Environment.NewLine;
                logEntry += $"Response Time: {endTime:yyyy-MM-dd HH:mm:ss}" + Environment.NewLine;
                logEntry += $"Duration: {duration}ms" + Environment.NewLine;
                logEntry += $"Response Data: {(responseData != null ? System.Text.Json.JsonSerializer.Serialize(responseData) : "null")}" + Environment.NewLine;
                logEntry += new string('-', 80) + Environment.NewLine;
                
                System.IO.File.AppendAllText(logFilePath, logEntry);
            }
            catch
            {
                // 日誌失敗不影響主流程
            }
        }

        
        // 讀取 QC 檢體檢驗項目
        // Get DXI/getQcTargets/{instrumentId}/{barcode}
        [HttpGet("getQcTargets/{instrumentId}/{barcode}")]
        public async Task<IActionResult> GetQcTargets(string instrumentId, string barcode)
        {
            var startTime = DateTime.Now;
            Response response = new Response();

            // 抽出單位的ID
            var defaultUnitId = _configuration["Supabase:DefaultUnitID"];
            // 1. 查 qc_targets
            if (string.IsNullOrEmpty(instrumentId) || string.IsNullOrEmpty(defaultUnitId))
            {
                response.success = false;
                response.data = null;
                response.message = "instrumentId 或 defaultUnitId 不可為空";
                LogApiCall($"/Access2/getQcTargets/{instrumentId}/{barcode}", "GET", new { instrumentId, barcode }, response.data, response.success, response.message, startTime);
                return BadRequest(response);
            }

            // 檢查 uuid 格式
            Guid guidCheck;
            if (!Guid.TryParse(instrumentId, out guidCheck) || !Guid.TryParse(defaultUnitId, out guidCheck))
            {
                response.success = false;
                response.data = null;
                response.message = "instrumentId 或 defaultUnitId 格式錯誤 (必須為 uuid)";
                LogApiCall($"/Access2/getQcTargets/{instrumentId}/{barcode}", "GET", new { instrumentId, barcode }, response.data, response.success, response.message, startTime);
                return BadRequest(response);
            }

            var qcTargetsResult = await _supabaseClient
                .From<QcTarget>()
                .Filter("instrument_id", Postgrest.Constants.Operator.Equals, instrumentId)
                // .Filter("status", Postgrest.Constants.Operator.Equals, "pending")
                .Filter("unit_id", Postgrest.Constants.Operator.Equals, defaultUnitId)
                .Filter("active", Postgrest.Constants.Operator.Equals, "true")
                .Filter("qc_barcode", Postgrest.Constants.Operator.Equals, barcode)
                .Get();

            // 狀態對照表
            // pending=待處理
            // processing=上機
            // done=完成

            var qcTargets = qcTargetsResult.Models.FirstOrDefault();

            if (qcTargets == null) {
                response.success = false;
                response.data = null;
                response.message = "查無QC資料";
                LogApiCall($"/Access2/getQcTargets/{instrumentId}/{barcode}", "GET", new { instrumentId, barcode }, response.data, response.success, response.message, startTime);
                return NotFound(response);
            }

            var ordersList = qcTargetsResult.Models.Select(test => new Orders
            {
                BarCode = barcode,
                PatientID = "",
                PatientName = "",
                ItemsCode = DXI_ExamineItems_Dic[test.qc_number], // 將項目名稱轉成儀器使用的代碼
                ItemsName = test.qc_number,
                ItemsType = "QC", // QC檢體
                InstrumentID = instrumentId
            }).ToList();

            response.success = true;
            response.data = ordersList;
            response.message = "查詢QC成功";
            LogApiCall($"/Access2/getQcTargets/{instrumentId}/{barcode}", "GET", new { instrumentId, barcode }, response.data, response.success, response.message, startTime);
            return Ok(response);
        }

        // 當檢驗項目經連線程式送往儀器後，批次更新QC檢驗項目的狀態
        // POST 更新QC檢驗項目已被機器讀走
        [HttpPost("setQcItemsQueried")]
        public async Task<IActionResult> SetQcItemsQueried([FromBody] List<Orders> orders)
        {
            var startTime = DateTime.Now;
            Response response = new Response();
            var defaultUnitId = _configuration["Supabase:DefaultUnitID"];
            var updateTasks = new List<Task>();

            foreach (var order in orders)
            {
                var itemsCode = order.ItemsName;
                if (DXICodeToItemID.ContainsKey(itemsCode))
                {
                    itemsCode = DXICodeToItemID[itemsCode];
                }

                // 先查出 QcTarget 實體
                var qcTargetResult = await _supabaseClient
                    .From<QcTarget>()
                    .Filter("instrument_id", Postgrest.Constants.Operator.Equals, order.InstrumentID)
                    .Filter("qc_number", Postgrest.Constants.Operator.Equals, itemsCode)
                    .Filter("unit_id", Postgrest.Constants.Operator.Equals, defaultUnitId)
                    .Filter("qc_barcode", Postgrest.Constants.Operator.Equals, order.BarCode)
                    .Get();

                var qcTarget = qcTargetResult.Models.FirstOrDefault();
                if (qcTarget == null) continue; // 查無資料跳過

                qcTarget.status = "processing";
                updateTasks.Add(_supabaseClient.From<QcTarget>().Update(qcTarget));
            }

            await Task.WhenAll(updateTasks);
            response.success = true;
            response.message = "更新QC檢驗項目狀態成功";
            LogApiCall("/Access2/setQcItemsQueried", "POST", orders, response.data, response.success, response.message, startTime);
            return Ok(response);
        }

        // 寫入 QC 檢驗結果
        [HttpPost("setQcItemsResult")]
        public async Task<IActionResult> SetQcItemsResult([FromBody] QcOrderItems qcItems)
        {
            var startTime = DateTime.Now;
            Response response = new Response();

            // 抽出單位的ID
            var defaultUnitId = _configuration["Supabase:DefaultUnitID"];

            var convertedItemsCode = qcItems.ItemsCode;
            if (DXICodeToItemID.ContainsKey(qcItems.ItemsCode))
            {
                convertedItemsCode = DXICodeToItemID[qcItems.ItemsCode];
            }

            var qcTargetsResult = await _supabaseClient
                .From<QcTarget>()
                .Filter("instrument_id", Postgrest.Constants.Operator.Equals, qcItems.InstrumentID)
                .Filter("unit_id", Postgrest.Constants.Operator.Equals, defaultUnitId)
                .Filter("qc_number", Postgrest.Constants.Operator.Equals, convertedItemsCode)
                .Filter("qc_barcode", Postgrest.Constants.Operator.Equals, qcItems.BarCode)
                .Get();

            var qcTargets = qcTargetsResult.Models.FirstOrDefault();
            if (qcTargets == null) {
                response.success = false;
                response.data = null;
                response.message = "查無QC資料";
                return NotFound(response);
            }

            // 防呆檢查
            if (qcTargets.qc_item_id == null || qcTargets.lot_id == null || qcTargets.instrument_id == null)
            {
                response.success = false;
                response.data = null;
                response.message = "QC目標資料欄位為空";
                LogApiCall("/Access2/setQcItemsResult", "POST", qcItems, response.data, response.success, response.message, startTime);
                return BadRequest(response);
            }
            if (qcItems.BarCode == null || qcItems.ItemsCode == null || qcItems.ItemsResult == null)
            {
                response.success = false;
                response.data = null;
                response.message = "QC檢體資料欄位為空";
                LogApiCall("/Access2/setQcItemsResult", "POST", qcItems, response.data, response.success, response.message, startTime);
                return BadRequest(response);
            }

            decimal resultValue = 0;
            decimal.TryParse(qcItems.ItemsResult, out resultValue);

            // 解析 TestDate，如果為空或解析失敗則使用 DateTime.Now
            DateTime testDate = DateTime.Now;
            if (!string.IsNullOrEmpty(qcItems.TestDate))
            {
                DateTime.TryParseExact(qcItems.TestDate, "yyyy/MM/dd HH:mm:ss", 
                    System.Globalization.CultureInfo.InvariantCulture, 
                    System.Globalization.DateTimeStyles.None, 
                    out testDate);
            }

            var qcData = new QcData
            {
                id = Guid.NewGuid(),
                qc_item_id = qcTargets.qc_item_id,
                lot_id = qcTargets.lot_id,
                value = resultValue,
                test_date = testDate,
                instrument_id = qcTargets.instrument_id,
                status = "pending",
                created_at = DateTime.Now,
                updated_at = DateTime.Now,
                unit_id = Guid.Parse(defaultUnitId),
                performed_by = "system",
                rack_number = qcItems.RackNumber,
                cup_number = qcItems.CupNumber,
                concentration = qcItems.Level,
                cuvette = qcItems.Cuvette
            };

            await _supabaseClient.From<QcData>().Insert(qcData);

            response.success = true;
            response.message = "寫入QC檢驗結果成功";
            LogApiCall("/Access2/setQcItemsResult", "POST", qcItems, response.data, response.success, response.message, startTime);
            return Ok(response);
        }

        // ------------------

        // GET DXI/getItems/{barcode}
        [HttpGet("getItems/{barcode}")]
        public async Task<IActionResult> GetSpecimenByBarcode(string barcode)
        {
            var startTime = DateTime.Now;
            Response response = new Response();

            // 抽出單位的ID
            var defaultUnitId = _configuration["Supabase:DefaultUnitID"];
            // 1. 先查 specimen
            var specimenResult = await _supabaseClient
                    .From<Specimen>()
                    .Filter("specimen_code", Postgrest.Constants.Operator.Equals, barcode)
                    .Filter("unit_id", Postgrest.Constants.Operator.Equals, defaultUnitId)
                    // .Filter("status", Postgrest.Constants.Operator.Equals, "received")
                    .Order("created_at", Postgrest.Constants.Ordering.Descending)
                    .Get();

            // 狀態對照表
            // received=簽收
            // processing=上機
            // completed=完成
            // reported=最終報告送出
            // validation_failed=驗證失敗
            // approved=審核通過
            // rejected=退件

            var specimen = specimenResult.Models.FirstOrDefault();

            if (specimen == null) {
                response.success = false;
                response.data = null;
                response.message = "查無資料";
                LogApiCall($"/Access2/getItems/{barcode}", "GET", new { barcode }, response.data, response.success, response.message, startTime);
                return NotFound(response);
            }
            // if (specimen.status != "received") {
            //     response.success = false;
            //     response.data = null;
            //     response.message = $"醫令狀態非 'received'，目前狀態為 '{specimen.status}'";
            //     return BadRequest(response);
            // }

            // 如果找到資料，則繼續
            var specimenId = specimen.specimen_id;
            var testResult = await _supabaseClient
                .From<SpecimenTest>()
                .Filter("specimen_id", Postgrest.Constants.Operator.Equals, specimenId)
                .Filter("status", Postgrest.Constants.Operator.Equals, "pending") // 只撈出還沒被讀走的項目
                .Filter("test_code", Postgrest.Constants.Operator.In, ExamineItems) // 只撈出符合的項目
                .Get();
            
            var ordersList = testResult.Models.Select(test => new Orders
            {
                BarCode = barcode,
                PatientID = specimen.specimen_id,
                PatientName = specimen.patient_name,
                ItemsCode = DXI_ExamineItems_Dic[test.test_code], // 將項目名稱轉成儀器使用的代碼
                ItemsName = test.test_name,
                ItemsType = "NORMAL", // 一般檢體
                InstrumentID = ""
            }).ToList();

            response.success = true;
            response.data = ordersList;
            response.message = "查詢成功";
            LogApiCall($"/Access2/getItems/{barcode}", "GET", new { barcode }, response.data, response.success, response.message, startTime);
            return Ok(response);
        }

        // POST 更新檢驗項目已被機器讀走
        // 第二步：當檢驗項目經連線程式送往儀器後，批次更新檢驗項目的狀態
        [HttpPost("setItemsQueried")]
        public async Task<IActionResult> setItemsQueried([FromBody] List<Orders> orders)
        {
            var startTime = DateTime.Now;
            Response response = new Response();
            var defaultUnitId = _configuration["Supabase:DefaultUnitID"];
            var updateTasks = new List<Task>();

            foreach (var item in orders)
            {
                // 1. 查 specimen_id
                var specimenResult = await _supabaseClient
                    .From<Specimen>()
                    .Filter("specimen_code", Postgrest.Constants.Operator.Equals, item.BarCode)
                    .Filter("unit_id", Postgrest.Constants.Operator.Equals, defaultUnitId)
                    .Order("created_at", Postgrest.Constants.Ordering.Descending)
                    .Get();

                var specimen = specimenResult.Models.FirstOrDefault();
                if (specimen == null) continue; // 查無資料跳過

                var itemsCode = item.ItemsCode;
                // 自動轉換：如果 ItemsCode 是 AU 代碼，轉成 ItemID
                if (DXICodeToItemID.ContainsKey(itemsCode))
                {
                    itemsCode = DXICodeToItemID[itemsCode];
                }

                // 2. 查 SpecimenTest
                var testResult = await _supabaseClient
                    .From<SpecimenTest>()
                    .Filter("specimen_id", Postgrest.Constants.Operator.Equals, specimen.specimen_id)
                    .Filter("test_code", Postgrest.Constants.Operator.Equals, itemsCode)
                    .Get();

                var specimenTest = testResult.Models.FirstOrDefault();
                if (specimenTest == null) continue; // 查無資料跳過

                // 3. 更新 status
                specimenTest.status = "processing";
                updateTasks.Add(_supabaseClient.From<SpecimenTest>().Update(specimenTest));
            }

            await Task.WhenAll(updateTasks);

            response.success = true;
            response.message = "批次更新完成";
            response.data = null;
            LogApiCall("/Access2/setItemsQueried", "POST", orders, response.data, response.success, response.message, startTime);
            return Ok(response);
        }

        // 更新 spciments 的 status
        // received=簽收
        // processing=上機
        // completed=完成
        // reported=最終報告送出
        // validation_failed=驗證失敗
        // approved=審核通過
        // rejected=退件
        [HttpGet("updateStatus/{barcode}/{status}")]
        public async Task<IActionResult> UpdateSpecimenStatus(string barcode, string status)
        {
            var startTime = DateTime.Now;
            Response response = new Response();

            var defaultUnitId = _configuration["Supabase:DefaultUnitID"];
            var result = await _supabaseClient
                .From<Specimen>()
                .Filter("specimen_code", Postgrest.Constants.Operator.Equals, barcode)
                .Filter("unit_id", Postgrest.Constants.Operator.Equals, defaultUnitId)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            var specimen = result.Models.FirstOrDefault();
            if (specimen == null) {
                response.success = false;
                response.data = null;
                response.message = "查無醫令資料";
                return NotFound(response);
            }

            specimen.status = status;
            var updateResp = await _supabaseClient
                .From<Specimen>()
                .Update(specimen);

            response.success = true;
            response.data = null;
            response.message = "更新醫令狀態成功";
            LogApiCall($"/Access2/updateStatus/{barcode}/{status}", "GET", new { barcode, status }, response.data, response.success, response.message, startTime);
            return Ok(response);
        }

        // 更新項目的 Result Value
        [HttpPost("setItemsResult")]
        public async Task<IActionResult> UpdateSpecimenTestResult([FromBody] OrderItems orderItems)
        {
            var startTime = DateTime.Now;
            Response response = new Response();

            var defaultUnitId = _configuration["Supabase:DefaultUnitID"];
            var result = await _supabaseClient
                .From<Specimen>()
                .Filter("specimen_code", Postgrest.Constants.Operator.Equals, orderItems.BarCode)
                .Filter("unit_id", Postgrest.Constants.Operator.Equals, defaultUnitId)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            var specimen = result.Models.FirstOrDefault();
            if (specimen == null) {
                response.success = false;
                response.data = null;
                response.message = "查無醫令資料";
                LogApiCall("/Access2/setItemsResult", "POST", orderItems, response.data, response.success, response.message, startTime);
                return NotFound(response);
            }

            var specimenId = specimen.specimen_id;
            var convertedItemsCode = orderItems.ItemsCode;
            // 自動轉換：如果 ItemsCode 是 DXI 代碼，轉成 ItemID
            if (DXICodeToItemID.ContainsKey(orderItems.ItemsCode))
            {
                convertedItemsCode = DXICodeToItemID[orderItems.ItemsCode];
            }

            var testResult = await _supabaseClient
                .From<SpecimenTest>()
                .Filter("specimen_id", Postgrest.Constants.Operator.Equals, specimenId)
                .Filter("test_code", Postgrest.Constants.Operator.Equals, convertedItemsCode)
                .Get();

            var specimenTest = testResult.Models.FirstOrDefault();
            if (specimenTest == null) {
                response.success = false;
                response.data = null;
                response.message = "查無檢驗項目資料";
                LogApiCall("/Access2/setItemsResult", "POST", orderItems, response.data, response.success, response.message, startTime);
                return NotFound(response);
            }
            specimenTest.result_value = orderItems.ItemsResult;
            specimenTest.result_date = DateTime.Now;
            specimenTest.Flag = orderItems.ItemsFlag;
            specimenTest.rack_number = orderItems.RackNumber;
            specimenTest.cup_number = orderItems.CupNumber;
            specimenTest.status = "completed"; // 預設更新為 completed

            var updateTestResp = await _supabaseClient
                .From<SpecimenTest>()
                .Update(specimenTest);

            response.success = true;
            response.data = null;
            response.message = "更新檢驗項目成功";
            LogApiCall("/Access2/setItemsResult", "POST", orderItems, response.data, response.success, response.message, startTime);
            return Ok(response);
        }

        // HealthCheck API - 用於保持 Supabase 連線活躍，避免 cold start
        [HttpGet("healthcheck")]
        public async Task<IActionResult> HealthCheck()
        {
            var startTime = DateTime.Now;
            Response response = new Response();
            
            try
            {
                var defaultUnitId = _configuration["Supabase:DefaultUnitID"];
                
                // 執行一個簡單的查詢來保持連線活躍
                var result = await _supabaseClient
                    .From<Specimen>()
                    .Filter("unit_id", Postgrest.Constants.Operator.Equals, defaultUnitId)
                    .Limit(1)
                    .Get();
                
                response.success = true;
                response.message = "HealthCheck OK";
                response.data = new { 
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    status = "connected"
                };
                
                LogApiCall("/Access2/healthcheck", "GET", null, response.data, response.success, response.message, startTime);
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.success = false;
                response.message = $"HealthCheck Failed: {ex.Message}";
                response.data = null;
                
                LogApiCall("/Access2/healthcheck", "GET", null, response.data, response.success, response.message, startTime);
                return StatusCode(500, response);
            }
        }

        public class UpdateStatusRequest
        {
            public int order_no { get; set; }
            public string status { get; set; }
        }

    }
}
