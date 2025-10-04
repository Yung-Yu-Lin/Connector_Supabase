using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LIS_Middleware.Models;
using Microsoft.Extensions.Configuration;

namespace LIS_Middleware.Controllers
{
    [Route("BeckmanAu5800")]
    public class BeckmanAu5800Controller : Controller
    {
        private readonly Supabase.Client _supabaseClient;
        private readonly IConfiguration _configuration;

        public BeckmanAu5800Controller(Supabase.Client supabaseClient, IConfiguration configuration)
        {
            _supabaseClient = supabaseClient;
            _configuration = configuration;
        }

        // GET BeckmanAu5800/specimen/{barcode}
        [HttpGet("specimen/{barcode}")]
        public async Task<IActionResult> GetSpecimenByBarcode(string barcode)
        {
            Response response = new Response();

            // 抽出單位的ID
            var defaultUnitId = _configuration["Supabase:DefaultUnitID"];

            var result = await _supabaseClient
                .From<Specimen>()
                .Filter("specimen_code", Postgrest.Constants.Operator.Equals, barcode)
                .Filter("unit_id", Postgrest.Constants.Operator.Equals, defaultUnitId)
                .Filter("status", Postgrest.Constants.Operator.Equals, "received")
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

            var specimen = result.Models.FirstOrDefault();
            if (specimen == null) {
                response.success = false;
                response.data = null;
                response.message = "查無資料";
                return NotFound(response);
            }

            // 如果找到資料，則繼續
            var specimenId = specimen.specimen_id;
            var testResult = await _supabaseClient
                .From<SpecimenTest>()
                .Filter("specimen_id", Postgrest.Constants.Operator.Equals, specimenId)
                .Get();
            
            var ordersList = testResult.Models.Select(test => new Orders
            {
                BarCode = barcode,
                PatientID = specimen.specimen_id,
                PatientName = specimen.patient_name,
                ItemsCode = test.test_code,
                ItemsName = test.test_name
            }).ToList();

            response.success = true;
            response.data = ordersList;
            response.message = "查詢成功";
            return Ok(response);
        }

        // 更新項目的 Result Value
        [HttpGet("updateResult/{barcode}/{itemsCode}/{itemsResult}")]
        public async Task<IActionResult> UpdateSpecimenTestResult(string barcode, string itemsCode, string itemsResult)
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

            var specimenId = specimen.specimen_id;
            var testResult = await _supabaseClient
                .From<SpecimenTest>()
                .Filter("specimen_id", Postgrest.Constants.Operator.Equals, specimenId)
                .Filter("test_code", Postgrest.Constants.Operator.Equals, itemsCode)
                .Get();

            var specimenTest = testResult.Models.FirstOrDefault();
            if (specimenTest == null) {
                response.success = false;
                response.data = null;
                response.message = "查無檢驗項目資料";
                return NotFound(response);
            }
            specimenTest.result_value = itemsResult;
            specimenTest.result_date = DateTime.Now;

            var updateTestResp = await _supabaseClient
                .From<SpecimenTest>()
                .Update(specimenTest);

            response.success = true;
            response.data = null;
            response.message = "更新檢驗項目成功";
            return Ok(response);
        }

        // 更新項目的 Flag Value
        [HttpGet("updateFlag/{barcode}/{itemsCode}/{itemsFlag}")]
        public async Task<IActionResult> UpdateSpecimenTestFlag(string barcode, string itemsCode, string itemsFlag)
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

            var specimenId = specimen.specimen_id;
            var testResult = await _supabaseClient
                .From<SpecimenTest>()
                .Filter("specimen_id", Postgrest.Constants.Operator.Equals, specimenId)
                .Filter("test_code", Postgrest.Constants.Operator.Equals, itemsCode)
                .Get();

            var specimenTest = testResult.Models.FirstOrDefault();
            if (specimenTest == null) {
                response.success = false;
                response.data = null;
                response.message = "查無檢驗項目資料";
                return NotFound(response);
            }
            specimenTest.Flag = itemsFlag;

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
