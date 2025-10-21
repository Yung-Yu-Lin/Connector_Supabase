using System;
using System.Collections.Generic;
using Postgrest.Models;
using Postgrest.Attributes;
using Newtonsoft.Json;

namespace LIS_Middleware.Models
{
    [Table("specimen_tests")]
    public class SpecimenTest : BaseModel
    {
        // Hide the PrimaryKey property from serialization
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public new Dictionary<PrimaryKeyAttribute, object> PrimaryKey { get; set; }
        
        [PrimaryKey("id", false)]
        public int id { get; set; }
        
        [Column("specimen_id")]
        public string specimen_id { get; set; }
        
        [Column("test_id")]
        public Guid test_id { get; set; }
        
        [Column("test_code")]
        public string test_code { get; set; }
        
        [Column("test_name")]
        public string test_name { get; set; }
        
        [Column("test_english_name")]
        public string test_english_name { get; set; }
        
        [Column("result_value")]
        public string result_value { get; set; }
        
        [Column("collection_date")]
        public DateTime collection_date { get; set; }
        
        [Column("result_date")]
        public DateTime? result_date { get; set; }
        
        [Column("category")]
        public string category { get; set; }
        
        [Column("department")]
        public string department { get; set; }
        
        [Column("reference_range")]
        public string reference_range { get; set; }
        
        [Column("unit")]
        public string unit { get; set; }
        
        [Column("status")]
        public string status { get; set; }
        
        [Column("created_at")]
        public DateTime created_at { get; set; }
        
        [Column("updated_at")]
        public DateTime updated_at { get; set; }
        
        [Column("submitter_unit")]
        public string submitter_unit { get; set; }
        
        [Column("specimen_code")]
        public string specimen_code { get; set; }
        
        [Column("DeviceID")]
        public string DeviceID { get; set; }
        
        [Column("unit_id")]
        public Guid? unit_id { get; set; }
        
        [Column("inputter")]
        public string inputter { get; set; }
        
        [Column("input_time")]
        public DateTime? input_time { get; set; }
        
        [Column("last_result")]
        public string last_result { get; set; }
        
        [Column("critical_low")]
        public string critical_low { get; set; }
        
        [Column("critical_high")]
        public string critical_high { get; set; }
        
        [Column("Flag")]
        public string Flag { get; set; }
        
        [Column("UpdatedBy")]
        public string UpdatedBy { get; set; }
        
        [Column("InterfaceType")]
        public string InterfaceType { get; set; }
        
        [Column("HostResponse")]
        public string HostResponse { get; set; }
        
        [Column("Notes")]
        public string Notes { get; set; }
        
        [Column("validation_reviewed")]
        public bool? validation_reviewed { get; set; }
        
        [Column("validation_reviewed_at")]
        public DateTime? validation_reviewed_at { get; set; }
        
        [Column("validation_reviewed_by")]
        public string validation_reviewed_by { get; set; }
        
        [Column("notes")]
        public string notes { get; set; }
        
        [Column("TubeGroupCode")]
        public string TubeGroupCode { get; set; }
        
        [Column("PrintLabel")]
        public bool? PrintLabel { get; set; }
        
        [Column("price")]
        public decimal? price { get; set; }
        
        [Column("price_source")]
        public string price_source { get; set; }
        
        [Column("package_id")]
        public Guid? package_id { get; set; }
        
        [Column("input_type")]
        public string input_type { get; set; }
        
        [Column("qualified_comparison_values")]
        public string qualified_comparison_values { get; set; }
        
        [Column("is_abnormal")]
        public bool? is_abnormal { get; set; }
        
        [Column("is_highly_abnormal")]
        public bool? is_highly_abnormal { get; set; }

        [Column("rack_number")]
        public string rack_number { get; set; }

        [Column("cup_number")]
        public string cup_number { get; set; }
    }
}