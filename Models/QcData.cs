using System;
using Postgrest.Models;
using Postgrest.Attributes;
using Newtonsoft.Json.Linq;

namespace LIS_Middleware.Models
{
    [Table("qc_data")]
    public class QcData : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public Guid id { get; set; }

        [Column("qc_item_id")]
        public Guid qc_item_id { get; set; }

        [Column("lot_id")]
        public Guid lot_id { get; set; }

        [Column("value")]
        public decimal value { get; set; }

        [Column("test_date")]
        public DateTime test_date { get; set; }

        [Column("performed_by")]
        public string performed_by { get; set; }

        [Column("instrument_id")]
        public Guid? instrument_id { get; set; }

        [Column("status")]
        public string status { get; set; }

        [Column("reviewed_by")]
        public string reviewed_by { get; set; }

        [Column("reviewed_at")]
        public DateTime? reviewed_at { get; set; }

        [Column("deviation_sd")]
        public decimal? deviation_sd { get; set; }

        [Column("created_at")]
        public DateTime created_at { get; set; }

        [Column("updated_at")]
        public DateTime updated_at { get; set; }

        [Column("notes")]
        public JToken notes { get; set; }

        [Column("unit_id")]
        public Guid? unit_id { get; set; }

        [Column("action_date")]
        public DateTime? action_date { get; set; }

        [Column("rack_number")]
        public string rack_number { get; set; }

        [Column("cup_number")]
        public string cup_number { get; set; }

        [Column("concentration")]
        public string concentration { get; set; }

        [Column("cuvette")]
        public string cuvette { get; set; }
    }
}