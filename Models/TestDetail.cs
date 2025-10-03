using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS_Middleware.Models
{
    [Table("TestDetail")]
    public class TestDetail
    {
        [Key]
        [Column(Order = 0)]
        [StringLength(15)]
        public string SNO { get; set; }

        [Key]
        [Column(Order = 1)]
        [StringLength(20)]
        public string ItemID { get; set; }

        [StringLength(20)]
        public string SetID { get; set; }

        [StringLength(50)]
        public string Name { get; set; }

        [StringLength(50)]
        public string Result { get; set; }

        [StringLength(5)]
        public string Interpretation { get; set; }

        public decimal? Price { get; set; }
        public int? NHI_Price { get; set; }
        public int? Discount { get; set; }

        [StringLength(20)]
        public string NHI_ID { get; set; }

        public bool? IsNHI { get; set; }
        public bool? IsPrint { get; set; }

        [StringLength(12)]
        public string TestID { get; set; }

        public int SubID { get; set; }
        public bool Unquoted { get; set; }

        [StringLength(10)]
        public string KeyIn { get; set; }

        public bool Recheck { get; set; }
        public int Sample_KindID { get; set; }
        public bool IsExcep { get; set; }

        [StringLength(2)]
        public string OutSource { get; set; }

        public int Agio { get; set; }
        public bool NonPricing { get; set; }
        public bool? IsChkD { get; set; }
    }
}