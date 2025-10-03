using System;
namespace LIS_Middleware.Models
{
    // 回傳到檢驗項目類別
    public class Orders
    {
        public string BarCode { get; set; }
        public string PatientID { get; set; }
        public string PatientName { get; set; }
        public int PatientGender { get; set; }
        public string ItemsCode { get; set; }
        public string ItemsName { get; set; }
    }
}
