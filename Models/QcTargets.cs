using System;
using Postgrest.Models;
using Postgrest.Attributes;

namespace LIS_Middleware.Models
{
    [Table("qc_targets")]
    public class QcTarget : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public Guid id { get; set; }

        [Column("qc_item_id")]
        public Guid qc_item_id { get; set; }

        [Column("lot_id")]
        public Guid lot_id { get; set; }

        [Column("target_value")]
        public decimal target_value { get; set; }

        [Column("standard_deviation")]
        public decimal standard_deviation { get; set; }

        [Column("min_acceptable")]
        public decimal min_acceptable { get; set; }

        [Column("max_acceptable")]
        public decimal max_acceptable { get; set; }

        [Column("created_at")]
        public DateTime created_at { get; set; }

        [Column("updated_at")]
        public DateTime updated_at { get; set; }

        [Column("instrument_id")]
        public Guid? instrument_id { get; set; }

        [Column("effective_start_date")]
        public DateTime? effective_start_date { get; set; }

        [Column("effective_end_date")]
        public DateTime? effective_end_date { get; set; }

        [Column("unit_id")]
        public Guid? unit_id { get; set; }

        [Column("active")]
        public bool active { get; set; }

        [Column("qc_barcode")]
        public string qc_barcode { get; set; }

        [Column("status")]
        public string status { get; set; }

        [Column("qc_number")]
        public string qc_number { get; set; }
    }
}