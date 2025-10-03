using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LIS_Middleware.DataDB;
using LIS_Middleware.Models;
using Microsoft.AspNetCore.Mvc;

namespace LIS_Middleware.Controllers
{
    public class Phadia_ExamineItems
    {
        // Phadia 專用的檢驗項目名稱
        public static string ACLG = "ACLG"; // 抗心脂抗體 IgG
        public static string ACLM = "ACLM"; // 抗心脂抗體 IgM
        public static string ALL3 = "ALL-3"; // 吸入性過敏原篩檢
        public static string B2GPIG = "B2GPIG"; // 抗β2醣蛋白I抗體 IgG
        public static string B2GPIM = "B2GPIM"; // 抗β2醣蛋白I抗體 IgM
        public static string CANCA = "C-ANCA";
        public static string CCP = "CCP"; // 抗環狀瓜氨酸胜太抗體
        public static string DNA = "DNA"; // DNA抗體
        public static string ENA = "ENA"; // 可抽出物核抗體測定
        public static string JO1 = "JO1"; // Anti-JO-1
        public static string PANCA = "P-ANCA";
        public static string RIP = "RIP"; // 抗核醣體P抗體
        public static string RNP = "RNP"; // Anti-RNP
        public static string RO = "RO"; // Anti-SSA
        public static string S70 = "S70"; // Anti-ScL 70
        public static string Sm = "Sm"; // Anti-Sm
        public static string La = "La"; // Anti-SSB
        public static string CEN = "CEN"; 

    }


    [Route("Phadia250")]
    public class Phadia250Controller : Controller
    {
        private readonly LISContext _context;
        public Phadia250Controller(LISContext context)
        {
            _context = context;
        }

        private static readonly string[] ExamineItems = new[]
        {
            // 對應資料庫開立的檢驗項目名稱
            "ACLG", "ACLM", "ALL-3", "B2GPIG", "B2GPIM", "C-ANCA", "CCP", "DNA", "ENA", "JO1", "P-ANCA", "RIP", "RNP", "RO", "S70", "Sm", "La", "CEN"
        };

        Dictionary<string, string> Phadia_ExamineItems_Dic = new Dictionary<string, string>()
        {
            //  Phadia 專用的檢驗項目與對應代碼
            { Phadia_ExamineItems.ACLG, "Gcl" }, // EI-G
            { Phadia_ExamineItems.ACLM, "Mcl" }, // EI-M
            { Phadia_ExamineItems.ALL3, "phinf" }, // sIgE
            { Phadia_ExamineItems.B2GPIG, "Gb2" }, // EI-G
            { Phadia_ExamineItems.B2GPIM, "Mb2" }, // EI-G
            { Phadia_ExamineItems.CANCA, "prs" }, // EI-G
            { Phadia_ExamineItems.CCP, "cp" }, // EI-G
            { Phadia_ExamineItems.DNA, "dn" }, // EI-G
            { Phadia_ExamineItems.ENA, "ctd/sy" }, // EI-G
            { Phadia_ExamineItems.JO1, "jo" }, // EI-G
            { Phadia_ExamineItems.PANCA, "mps" }, // EI-G
            { Phadia_ExamineItems.RIP, "rp" }, // EI-G
            { Phadia_ExamineItems.RNP, "rn" }, // EI-G
            { Phadia_ExamineItems.RO, "ro" }, // EI-G
            { Phadia_ExamineItems.S70, "scs" }, // EI-G
            { Phadia_ExamineItems.Sm, "sms" }, // EI-G
            { Phadia_ExamineItems.La, "la" }, // EI-G
            { Phadia_ExamineItems.CEN, "ce" } // EI-G
        };

        // 反向字典：Phadia 代碼 → ItemID
        Dictionary<string, string> PhadiaCodeToItemID => Phadia_ExamineItems_Dic.ToDictionary(x => x.Value, x => x.Key);

        // GET Phadia250/getItems/{barcode}
        [HttpGet("getItems/{barcode}")]
        public Response Get(string barcode)
        {
            Response response = new Response();
            try
            {
                // 只取第一筆符合的 SNO 與 SubName
                string year = "11" + barcode.Substring(0, 1); // "114"
                string month = barcode.Substring(1, 2); // "09"
                string recDateLike = year + "/" + month; // "114/09"
                string orderNo = barcode.Substring(4); // "038316"

                var docData = _context.TestDOCs
                                .Where(doc => doc.OrderNo == orderNo && doc.RecDate.Contains(recDateLike))
                                .OrderByDescending(doc => doc.RecDate)
                                .Select(doc => new { doc.SNO, doc.SubName })
                                .FirstOrDefault();
                if (docData == null)
                {
                    response.success = false;
                    response.message = "查無病歷號!";
                    response.data = null;
                    return response;
                }

                // ...existing code...
                List<Orders> pendingOrders = (from o in _context.TestDetails
                                                where ExamineItems.Contains(o.ItemID) && o.SNO == docData.SNO
                                                select new Orders
                                                {
                                                    BarCode = barcode,
                                                    PatientID = o.SNO,
                                                    PatientName = docData.SubName,
                                                    PatientGender = 0,
                                                    ItemsCode = Phadia_ExamineItems_Dic[o.ItemID],
                                                    ItemsName = o.Name
                                                }).ToList();
                if (pendingOrders.Count > 0)
                {
                    response.success = true;
                    response.message = "有醫令存在!";
                    response.data = pendingOrders;
                }
                else
                {
                    response.success = false;
                    response.message = "查無醫令!";
                    response.data = null;
                }
                return response;
                
            }
            catch (Exception ex)
            {
                response.success = false;
                response.message = "發生例外：" + ex.ToString();
                response.data = null;
                return response;
            }
        }

        [HttpGet("convertItems/{itemsCode}")]
        public Response convertItems(string itemsCode)
        {
            Response response = new Response();
            try
            {
                var convertedItemsCode = "";
                // 自動轉換：如果 ItemsCode 是 Phadia 代碼，轉成 ItemID
                if (PhadiaCodeToItemID.ContainsKey(itemsCode))
                {
                    convertedItemsCode = PhadiaCodeToItemID[itemsCode];
                }

                response.success = true;
                response.message = "測試!";
                response.data = convertedItemsCode;

                return response;
                
            }
            catch (Exception ex)
            {
                response.success = false;
                response.message = "發生例外：" + ex.ToString();
                response.data = null;
                return response;
            }
        }

        // POST 更新檢驗項目檢驗結果
        [HttpPost("setItemsResult")]
        public Response setItemsResult([FromBody] OrderItems orderitems)
        {
            Response response = new Response();
            response.success = false;
            try
            {
                // 只取第一筆符合的 SNO 與 SubName
                // 只取第一筆符合的 SNO 與 SubName
                string year = "11" + orderitems.BarCode.Substring(0, 1); // "114"
                string month = orderitems.BarCode.Substring(1, 2); // "09"
                string recDateLike = year + "/" + month; // "114/09"
                string orderNo = orderitems.BarCode.Substring(4); // "038316"

                var docData = _context.TestDOCs
                                .Where(doc => doc.OrderNo == orderNo && doc.RecDate.Contains(recDateLike))
                                .OrderByDescending(doc => doc.RecDate)
                                .Select(doc => new { doc.SNO, doc.SubName })
                                .FirstOrDefault();

                if (docData == null)
                {
                    response.success = false;
                    response.message = "查無病歷號!";
                    response.data = orderNo;
                    return response;
                }

                // 更新 TestDOCs where SNO = docData.SNO, Completion = true
                // var updateDoc = _context.TestDOCs.FirstOrDefault(d => d.SNO == docData.SNO);
                // updateDoc.Completion = true;

                var itemsCode = orderitems.ItemsCode;
                // 自動轉換：如果 ItemsCode 是 Phadia 代碼，轉成 ItemID
                if (PhadiaCodeToItemID.ContainsKey(itemsCode))
                {
                    itemsCode = PhadiaCodeToItemID[itemsCode];
                }

                var updateItems = (from o in _context.TestDetails
                                   where o.SNO == docData.SNO && o.ItemID == itemsCode
                                   select o).FirstOrDefault();
                if (updateItems != null)
                {
                    if (orderitems.ItemsFlag == "Positive") {
                        updateItems.Result = orderitems.ItemsResult + "(+)";
                    } 
                    else if (orderitems.ItemsFlag == "Negative") {
                        updateItems.Result = orderitems.ItemsResult + "(-)";
                    }
                    else {
                        updateItems.Result = orderitems.ItemsResult;
                    }
                    response.success = true;
                }

                _context.SaveChanges();
                response.message = "寫入醫令結果完成！";
                response.data = null;
                return response;
            }
            catch (Exception ex)
            {
                response.success = false;
                response.message = "發生例外：" + ex.ToString();
                response.data = null;
                return response;
            }
        }

        // // POST 更新檢驗項目檢驗 Flag(comment標籤)
        // [HttpPost("setItemsFlag")]
        // public Response setItemsFlag([FromBody] OrderItems orderitems)
        // {
        //     Response response = new Response();
        //     try
        //     {
        //         using (LISContext beckManContext = new LISContext())
        //         {
        //             var itemsCode = orderitems.ItemsCode;
        //             // TODO: 如有特殊對應，請在此處處理
        //             var updateItems = (from o in beckManContext.ExOrders
        //                                where o.Barcode == orderitems.BarCode && o.Equitemid == itemsCode
        //                                select o).FirstOrDefault();
        //             if (updateItems != null)
        //             {
        //                 updateItems.Meno = orderitems.ItemsFlag;
        //             }
        //             beckManContext.SaveChanges();
        //             response.success = true;
        //             response.message = "寫入醫令標籤完成！";
        //             response.data = null;
        //             return response;
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         response.success = false;
        //         response.message = "發生例外：" + ex.ToString();
        //         response.data = null;
        //         return response;
        //     }
        // }
    }
}
