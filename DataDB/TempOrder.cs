using System;
using System.Collections.Generic;

#nullable disable

namespace LIS_Middleware.DataDB
{
    public partial class TempOrder
    {
        public string Sno { get; set; }
        public string SubName { get; set; }
        public string Gender { get; set; }
        public string SubIdno { get; set; }
        public string SubBirthday { get; set; }
        public string Barcode { get; set; }
        public string Mark { get; set; }
        public string SpecimenId { get; set; }
        public string Pc { get; set; }
        public int DeviceId { get; set; }
        public string DataBase { get; set; }
        public int? ListCreator { get; set; }
        public int Status { get; set; }
        public int Id { get; set; }
    }
}
