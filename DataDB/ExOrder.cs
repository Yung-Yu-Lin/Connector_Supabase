using System;
using System.Collections.Generic;

#nullable disable

namespace LIS_Middleware.DataDB
{
    public partial class ExOrder
    {
        public int Id { get; set; }
        public string WNo { get; set; }
        public string CDate { get; set; }
        public string Barcode { get; set; }
        public string Item { get; set; }
        public string Sample { get; set; }
        public string PId { get; set; }
        public string Name { get; set; }
        public string Birth { get; set; }
        public string Sex { get; set; }
        public string SpecimenState { get; set; }
        public string Equitemid { get; set; }
        public string SDate { get; set; }
        public string MDate { get; set; }
        public string MTime { get; set; }
        public string ChdV { get; set; }
        public string Low { get; set; }
        public string High { get; set; }
        public string VLow { get; set; }
        public string VHigh { get; set; }
        public string Result { get; set; }
        public string Dwflag { get; set; }
        public string Dworderdate { get; set; }
        public string Dwreportdate { get; set; }
        public string Examiner { get; set; }
        public string Meno { get; set; }
        public int? DeviceId { get; set; }
        public int? ListCreator { get; set; }
    }
}
