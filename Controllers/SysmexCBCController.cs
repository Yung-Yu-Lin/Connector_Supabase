using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using LIS_Middleware.Models;
using Microsoft.Extensions.Configuration;

namespace LIS_Middleware.Controllers
{
    public class SysmexCBC_ExamineItems
    {
        public static string WBC = "W.B.C";
        public static string RBC = "RBC/M";
        public static string HGB = "HgB/M";
        public static string HCT = "Hct/M";
        public static string MCV = "MCV";
        public static string MCH = "MCH";
        public static string MCHC = "MCHC";
        public static string PLT = "Plt[K]";
        public static string NEUT = "Net-s%";
        public static string LYMPH = "Lym-L%";
        public static string MONO = "Mono%";
        public static string BASO = "Baso%";
        public static string EO = "Eosin%";
        public static string NEUT_Abs = "Net -C";
        public static string LYMPH_Abs = "Lym -C";
        public static string MONO_Abs = "Mono-C";
        public static string EO_Abs = "Eosi-C";
        public static string BASO_Abs = "Baso-C";
        public static string RDW_SD = "RDW";
        public static string RDW_CV = "RDW-CV";
        public static string PDW = "PDW";
        public static string MPV = "MPV";
        public static string PCT = "PCT";
        public static string PLCR = "P-LCR";
    }

    [Route("SysmexCBC")]
    public class SysmexCBCController : Controller
    {
        // 這裡定義的是，Supabase 裡面對應的檢驗項目代碼 (test_code) test_code 必須要在這裡面有出現才會被 select 出來
        private static readonly string[] ExamineItems = new[]
        {
            "W.B.C", "RBC/M", "HgB/M", "Hct/M", "MCV", "MCH", "MCHC", "Plt[K]",
            "Net-s%", "Lym-L%", "Mono%", "Baso%", "Eosin%",
            "Net -C", "Lym -C", "Mono-C", "Eosi-C", "Baso-C",
            "RDW", "RDW-CV", "PDW", "MPV", "PCT", "P-LCR"
        };

        Dictionary<string, string> SysmexCBCCodeToItemID => SysmexCBC_ExamineItems_Dic.ToDictionary(x => x.Value, x => x.Key);

        Dictionary<string, string> SysmexCBC_ExamineItems_Dic = new Dictionary<string, string>()
        {
            { SysmexCBC_ExamineItems.WBC, "WBC" },
            { SysmexCBC_ExamineItems.RBC, "RBC" },
            { SysmexCBC_ExamineItems.HGB, "HGB" },
            { SysmexCBC_ExamineItems.HCT, "HCT" },
            { SysmexCBC_ExamineItems.MCV, "MCV" },
            { SysmexCBC_ExamineItems.MCH, "MCH" },
            { SysmexCBC_ExamineItems.MCHC, "MCHC" },
            { SysmexCBC_ExamineItems.PLT, "PLT" },
            { SysmexCBC_ExamineItems.NEUT, "NEUT%" },
            { SysmexCBC_ExamineItems.LYMPH, "LYMPH%" },
            { SysmexCBC_ExamineItems.MONO, "MONO%" },
            { SysmexCBC_ExamineItems.BASO, "BASO%" },
            { SysmexCBC_ExamineItems.EO, "EO%" },
            { SysmexCBC_ExamineItems.NEUT_Abs, "NEUT#" },
            { SysmexCBC_ExamineItems.LYMPH_Abs, "LYMPH#" },
            { SysmexCBC_ExamineItems.MONO_Abs, "MONO#" },
            { SysmexCBC_ExamineItems.EO_Abs, "EO#" },
            { SysmexCBC_ExamineItems.BASO_Abs, "BASO#" },
            { SysmexCBC_ExamineItems.RDW_SD, "RDW-SD" },
            { SysmexCBC_ExamineItems.RDW_CV, "RDW-CV" },
            { SysmexCBC_ExamineItems.PDW, "PDW" },
            { SysmexCBC_ExamineItems.MPV, "MPV" },
            { SysmexCBC_ExamineItems.PCT, "PCT" },
            { SysmexCBC_ExamineItems.PLCR, "P-LCR" }
        };

        private readonly Supabase.Client _supabaseClient;
        private readonly IConfiguration _configuration;

        public SysmexCBCController(Supabase.Client supabaseClient, IConfiguration configuration)
        {
            _supabaseClient = supabaseClient;
            _configuration = configuration;
        }

        // GET SysmexCBC/getItems/{barcode}
        [HttpGet("getItems/{barcode}")]
        public async Task<IActionResult> GetSpecimenByBarcode(string barcode)
        {
            var response = new Response();
            var defaultUnitId = _configuration["Supabase:DefaultUnitID"];

            if (string.IsNullOrEmpty(defaultUnitId))
            {
                response.success = false;
                response.message = "DefaultUnitID 未設定";
                return BadRequest(response);
            }

            var specimenResult = await _supabaseClient
                .From<Specimen>()
                .Filter("specimen_code", Postgrest.Constants.Operator.Equals, barcode)
                .Filter("unit_id", Postgrest.Constants.Operator.Equals, defaultUnitId)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            var specimen = specimenResult.Models.FirstOrDefault();
            if (specimen == null)
            {
                response.success = false;
                response.message = "查無資料";
                return NotFound(response);
            }

            var testResult = await _supabaseClient
                .From<SpecimenTest>()
                .Filter("specimen_id", Postgrest.Constants.Operator.Equals, specimen.specimen_id)
                .Filter("status", Postgrest.Constants.Operator.Equals, "pending")
                .Filter("test_code", Postgrest.Constants.Operator.In, ExamineItems) // 只撈出符合的項目
                .Get();

            var ordersList = testResult.Models.Select(t => new Orders
            {
                BarCode = barcode,
                PatientID = specimen.specimen_id,
                PatientName = specimen.patient_name,
                ItemsCode = SysmexCBC_ExamineItems_Dic[t.test_code],
                ItemsName = t.test_name,
                ItemsType = "NORMAL",
                InstrumentID = ""
            }).ToList();

            response.success = true;
            response.data = ordersList;
            response.message = "查詢成功";
            return Ok(response);
        }

        // POST SysmexCBC/setItemsQueried
        [HttpPost("setItemsQueried")]
        public async Task<IActionResult> SetItemsQueried([FromBody] List<Orders> orders)
        {
            var response = new Response();
            var defaultUnitId = _configuration["Supabase:DefaultUnitID"];
            var tasks = new List<Task>();

            foreach (var item in orders)
            {
                var specimenResult = await _supabaseClient
                    .From<Specimen>()
                    .Filter("specimen_code", Postgrest.Constants.Operator.Equals, item.BarCode)
                    .Filter("unit_id", Postgrest.Constants.Operator.Equals, defaultUnitId)
                    .Order("created_at", Postgrest.Constants.Ordering.Descending)
                    .Get();

                var specimen = specimenResult.Models.FirstOrDefault();
                if (specimen == null) continue;

                var itemsCode = item.ItemsCode;
                // 自動轉換：如果 ItemsCode 是 AU 代碼，轉成 ItemID
                if (SysmexCBCCodeToItemID.ContainsKey(itemsCode))
                {
                    itemsCode = SysmexCBCCodeToItemID[itemsCode];
                }

                var testResult = await _supabaseClient
                    .From<SpecimenTest>()
                    .Filter("specimen_id", Postgrest.Constants.Operator.Equals, specimen.specimen_id)
                    .Filter("test_code", Postgrest.Constants.Operator.Equals, itemsCode)
                    .Get();

                var specimenTest = testResult.Models.FirstOrDefault();
                if (specimenTest == null) continue;

                specimenTest.status = "processing";
                tasks.Add(_supabaseClient.From<SpecimenTest>().Update(specimenTest));
            }

            await Task.WhenAll(tasks);
            response.success = true;
            response.message = "批次更新完成";
            return Ok(response);
        }

        // POST SysmexCBC/setItemsResult
        [HttpPost("setItemsResult")]
        public async Task<IActionResult> SetItemsResult([FromBody] OrderItems orderItems)
        {
            var response = new Response();
            var defaultUnitId = _configuration["Supabase:DefaultUnitID"];

            var specimenResult = await _supabaseClient
                .From<Specimen>()
                .Filter("specimen_code", Postgrest.Constants.Operator.Equals, orderItems.BarCode)
                .Filter("unit_id", Postgrest.Constants.Operator.Equals, defaultUnitId)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            var specimen = specimenResult.Models.FirstOrDefault();
            if (specimen == null)
            {
                response.success = false;
                response.message = "查無醫令資料";
                return NotFound(response);
            }

            var itemsCode = orderItems.ItemsCode;
            // 自動轉換：如果 ItemsCode 是 AU 代碼，轉成 ItemID
            if (SysmexCBCCodeToItemID.ContainsKey(itemsCode))
            {
                itemsCode = SysmexCBCCodeToItemID[itemsCode];
            }

            var testResult = await _supabaseClient
                .From<SpecimenTest>()
                .Filter("specimen_id", Postgrest.Constants.Operator.Equals, specimen.specimen_id)
                .Filter("test_code", Postgrest.Constants.Operator.Equals, itemsCode)
                .Get();

            var specimenTest = testResult.Models.FirstOrDefault();
            if (specimenTest == null)
            {
                response.success = false;
                response.message = "查無檢驗項目資料";
                return NotFound(response);
            }

            // 若是 WBC 且結果為數值，乘以 100
            if (itemsCode == "WBC")
            {
                decimal wbcValue;
                if (decimal.TryParse(orderItems.ItemsResult, out wbcValue))
                {
                    specimenTest.result_value = (wbcValue * 100).ToString();
                }
                else
                {
                    specimenTest.result_value = orderItems.ItemsResult;
                }
            }
            else
            {
                specimenTest.result_value = orderItems.ItemsResult;
            }
            specimenTest.result_date = DateTime.Now;
            specimenTest.Flag = orderItems.ItemsFlag;
            specimenTest.status = "completed"; // 預設更新為 completed

            await _supabaseClient.From<SpecimenTest>().Update(specimenTest);

            response.success = true;
            response.message = "更新檢驗項目成功";
            return Ok(response);
        }
    }
}
