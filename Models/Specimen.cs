using System;
using System.Collections.Generic;  // Add this line
using Postgrest.Models;
using Postgrest.Attributes;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace LIS_Middleware.Models
{
    [Table("specimens")]
    public class Specimen : BaseModel
    {
        // Hide the PrimaryKey property from serialization
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public new Dictionary<PrimaryKeyAttribute, object> PrimaryKey { get; set; }
        
        [PrimaryKey("id", false)]
        public int id { get; set; }
        
        [Column("specimen_id")]
        public string specimen_id { get; set; }
        
        [Column("order_no")]
        public int order_no { get; set; }
        
        [Column("patient_name")]
        public string patient_name { get; set; }
        
        [Column("patient_id")]
        public string patient_id { get; set; }
        
        [Column("medical_record_number")]
        public string medical_record_number { get; set; }
        
        [Column("birthdate")]
        public DateTime? birthdate { get; set; }
        
        [Column("patient_age")]
        public string patient_age { get; set; }
        
        [Column("patient_gender")]
        public string patient_gender { get; set; }
        
        [Column("collection_date")]
        public DateTime collection_date { get; set; }
        
        [Column("priority")]
        public string priority { get; set; }
        
        [Column("created_at")]
        public DateTime created_at { get; set; }
        
        [Column("updated_at")]
        public DateTime updated_at { get; set; }
        
        [Column("status")]
        public string status { get; set; }
        
        [Column("submitter_unit")]
        public string submitter_unit { get; set; }
        
        [Column("specimen_code")]
        public string specimen_code { get; set; }
        
        [Column("source_name")]
        public string source_name { get; set; }
        
        [Column("File_date")]
        public DateTime? File_date { get; set; }
        
        [Column("contact_phone")]
        public string contact_phone { get; set; }
        
        [Column("contact_address")]
        public string contact_address { get; set; }
        
        [Column("specimen_status")]
        public string specimen_status { get; set; }
        
        [Column("unit_id")]
        public Guid? unit_id { get; set; }
        
        [Column("processing_started_at")]
        public DateTime? processing_started_at { get; set; }
        
        [Column("processing_completed_at")]
        public DateTime? processing_completed_at { get; set; }
        
        [Column("last_updated_by")]
        public string last_updated_by { get; set; }
        
        [Column("print_report_time")]
        public DateTime? print_report_time { get; set; }
        
        [Column("rejected_at")]
        public DateTime? rejected_at { get; set; }
        
        [Column("rejected_by")]
        public string rejected_by { get; set; }
        
        [Column("rejection_reason")]
        public string rejection_reason { get; set; }
        
        [Column("cust_id")]
        public Guid cust_id { get; set; }
        
        [Column("process_history")]
        public JToken process_history { get; set; }
        
        [Column("specimen_appearance")]
        public string specimen_appearance { get; set; }
        
        [Column("appearance_updated_at")]
        public DateTime? appearance_updated_at { get; set; }
        
        [Column("appearance_updated_by")]
        public string appearance_updated_by { get; set; }
    }
}