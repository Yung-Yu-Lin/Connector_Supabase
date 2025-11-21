using System;
namespace LIS_Middleware.Models
{
    public class QcOrderItems
    {
        public string BarCode { get; set; }
        public string ItemsCode { get; set; }
        public string ItemsResult { get; set; }
        public string ItemsFlag { get; set; }
        public string ItemsType { get; set; }
        public string InstrumentID { get; set; }
        public string RackNumber { get; set; }
        public string CupNumber { get; set; }
        public string Level { get; set; }
        public string Cuvette { get; set; }
        public string ResultDate { get; set; }
    }
}
