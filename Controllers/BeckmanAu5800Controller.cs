using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using LIS_Middleware.Models;
using Microsoft.Extensions.Configuration;

namespace LIS_Middleware.Controllers
{
    public class AU_ExamineItems
    {
        public static string TP = "TP";
        public static string ALB = "ALB";
        public static string TBIL = "TBIL";
        public static string ALKP = "ALKP";
        public static string AST = "AST";
        public static string ALT = "ALT";
        public static string LDH = "LDH";
        public static string GLU = "GLU";
        public static string TRIG = "TRIG";
        public static string HDL = "HDL";
        public static string BUN = "BUN";
        public static string CREA = "CREA";
        public static string URIC = "URIC";
        public static string GGT = "GGT";
        public static string CHOL = "CHOL";
    }

    [Route("AU")]
    public class BeckmanAu5800Controller : Controller
    {
        // 這裡定義的是，Supabase 裡面對應的檢驗項目代碼 (test_code) test_code 必須要在這裡面有出現才會被 select 出來
        private static readonly string[] ExamineItems = new[]
        {
            "TP", "ALB", "TBIL", "ALKP", "AST", "ALT", "LDH", "GLU", "TRIG", "HDL", "BUN", "CREA", "URIC", "GGT", "CHOL"
        };

        // 反向字典：AU 代碼 → ItemID
        Dictionary<string, string> AUCodeToItemID => AU_ExamineItems_Dic.ToDictionary(x => x.Value, x => x.Key);

        Dictionary<string, string> AU_ExamineItems_Dic = new Dictionary<string, string>()
        {
            { AU_ExamineItems.TP, "001" },
            { AU_ExamineItems.ALB, "002" },
            { AU_ExamineItems.TBIL, "003" },
            { AU_ExamineItems.ALKP, "007" },
            { AU_ExamineItems.AST, "008" },
            { AU_ExamineItems.ALT, "009" },
            { AU_ExamineItems.LDH, "012" },
            { AU_ExamineItems.GLU, "031" },
            { AU_ExamineItems.TRIG, "016" },
            { AU_ExamineItems.HDL, "017" },
            { AU_ExamineItems.BUN, "020" },
            { AU_ExamineItems.CREA, "021" },
            { AU_ExamineItems.URIC, "022" },
            { AU_ExamineItems.GGT, "014" },
            { AU_ExamineItems.CHOL, "015" }
        };

        private readonly Supabase.Client _supabaseClient;
        private readonly IConfiguration _configuration;

        public BeckmanAu5800Controller(Supabase.Client supabaseClient, IConfiguration configuration)
        {
            _supabaseClient = supabaseClient;
            _configuration = configuration;
        }

        // 讀取 QC 檢體檢驗項目
        // Get AU/getQcTargets/{instrumentId}/{barcode}
        [HttpGet("getQcTargets/{instrumentId}/{barcode}")]
        public async Task<IActionResult> GetQcTargets(string instrumentId, string barcode)
        {
            Response response = new Response();

            // 抽出單位的ID
            var defaultUnitId = _configuration["Supabase:DefaultUnitID"];
            // 1. 查 qc_targets
            if (string.IsNullOrEmpty(instrumentId) || string.IsNullOrEmpty(defaultUnitId))
            {
                response.success = false;
                response.data = null;
                response.message = "instrumentId 或 defaultUnitId 不可為空";
                return BadRequest(response);
            }

            // 檢查 uuid 格式
            Guid guidCheck;
            if (!Guid.TryParse(instrumentId, out guidCheck) || !Guid.TryParse(defaultUnitId, out guidCheck))
            {
                response.success = false;
                response.data = null;
                response.message = "instrumentId 或 defaultUnitId 格式錯誤 (必須為 uuid)";
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
                return NotFound(response);
            }

            var ordersList = qcTargetsResult.Models.Select(test => new Orders
            {
                BarCode = barcode,
                PatientID = "",
                PatientName = "",
                ItemsCode = AU_ExamineItems_Dic[test.qc_number], // 將項目名稱轉成儀器使用的代碼
                ItemsName = test.qc_number,
                ItemsType = "QC", // QC檢體
                InstrumentID = instrumentId
            }).ToList();

            response.success = true;
            response.data = ordersList;
            response.message = "查詢QC成功";
            return Ok(response);
        }

        // 當檢驗項目經連線程式送往儀器後，批次更新QC檢驗項目的狀態
        // POST 更新QC檢驗項目已被機器讀走
        [HttpPost("setQcItemsQueried")]
        public async Task<IActionResult> SetQcItemsQueried([FromBody] List<Orders> orders)
        {
            Response response = new Response();
            var defaultUnitId = _configuration["Supabase:DefaultUnitID"];
            var updateTasks = new List<Task>();

            foreach (var order in orders)
            {
                var itemsCode = order.ItemsName;
                if (AUCodeToItemID.ContainsKey(itemsCode))
                {
                    itemsCode = AUCodeToItemID[itemsCode];
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
            return Ok(response);
        }

        // 寫入 QC 檢驗結果
        [HttpPost("setQcItemsResult")]
        public async Task<IActionResult> SetQcItemsResult([FromBody] QcOrderItems qcItems)
        {
            Response response = new Response();

            // 抽出單位的ID
            var defaultUnitId = _configuration["Supabase:DefaultUnitID"];

            var convertedItemsCode = qcItems.ItemsCode;
            if (AUCodeToItemID.ContainsKey(qcItems.ItemsCode))
            {
                convertedItemsCode = AUCodeToItemID[qcItems.ItemsCode];
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
                return BadRequest(response);
            }
            if (qcItems.BarCode == null || qcItems.ItemsCode == null || qcItems.ItemsResult == null)
            {
                response.success = false;
                response.data = null;
                response.message = "QC檢體資料欄位為空";
                return BadRequest(response);
            }

            decimal resultValue = 0;
            decimal.TryParse(qcItems.ItemsResult, out resultValue);

            var qcData = new QcData
            {
                id = Guid.NewGuid(),
                qc_item_id = qcTargets.qc_item_id,
                lot_id = qcTargets.lot_id,
                value = resultValue,
                test_date = DateTime.Now,
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
            return Ok(response);
        }

        // ------------------

        // GET AU/getItems/{barcode}
        [HttpGet("getItems/{barcode}")]
        public async Task<IActionResult> GetSpecimenByBarcode(string barcode)
        {
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
                ItemsCode = AU_ExamineItems_Dic[test.test_code], // 將項目名稱轉成儀器使用的代碼
                ItemsName = test.test_name,
                ItemsType = "NORMAL", // 一般檢體
                InstrumentID = ""
            }).ToList();

            response.success = true;
            response.data = ordersList;
            response.message = "查詢成功";
            return Ok(response);
        }

        // POST 更新檢驗項目已被機器讀走
        // 第二步：當檢驗項目經連線程式送往儀器後，批次更新檢驗項目的狀態
        [HttpPost("setItemsQueried")]
        public async Task<IActionResult> setItemsQueried([FromBody] List<Orders> orders)
        {
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
                if (AUCodeToItemID.ContainsKey(itemsCode))
                {
                    itemsCode = AUCodeToItemID[itemsCode];
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
            return Ok(response);
        }

        // 更新項目的 Result Value
        [HttpPost("setItemsResult")]
        public async Task<IActionResult> UpdateSpecimenTestResult([FromBody] OrderItems orderItems)
        {
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
                return NotFound(response);
            }

            var specimenId = specimen.specimen_id;
            var convertedItemsCode = orderItems.ItemsCode;
            // 自動轉換：如果 ItemsCode 是 AU 代碼，轉成 ItemID
            if (AUCodeToItemID.ContainsKey(orderItems.ItemsCode))
            {
                convertedItemsCode = AUCodeToItemID[orderItems.ItemsCode];
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
            return Ok(response);
        }

        public class UpdateStatusRequest
        {
            public int order_no { get; set; }
            public string status { get; set; }
        }
    }
}
