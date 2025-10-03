using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LIS_Middleware.Models
{
    [Table("TestDOC")]
    public class TestDOC
    {
        [Key]
        [StringLength(15)]
        public string SNO { get; set; }
        [StringLength(12)]
        public string OrderNo { get; set; }
        [StringLength(10)]
        public string CustID { get; set; }
        [StringLength(20)]
        public string SubName { get; set; }
        [StringLength(9)]
        public string SubBirthDay { get; set; }
        [StringLength(3)]
        public string SubAge { get; set; }
        [StringLength(10)]
        public string SubIDNO { get; set; }
        [StringLength(1)]
        public string SubGender { get; set; }
        [StringLength(30)]
        public string MedicalNo { get; set; }
        [StringLength(20)]
        public string SpecimenConditions { get; set; }
        [StringLength(3)]
        public string TestSpecies { get; set; }
        [StringLength(9)]
        public string RecDate { get; set; }
        [StringLength(9)]
        public string InspDate { get; set; }
        [StringLength(9)]
        public string ReportDate { get; set; }
        [StringLength(15)]
        public string PickDate { get; set; }
        public decimal Amount { get; set; }
        [StringLength(10)]
        public string RegEmp { get; set; }
        [StringLength(1)]
        public string Payment { get; set; }
        [StringLength(10)]
        public string Tel { get; set; }
        [StringLength(50)]
        public string Address { get; set; }
        [StringLength(10)]
        public string Reviewers { get; set; }
        [StringLength(9)]
        public string AuditDay { get; set; }
        [StringLength(8)]
        public string AuditTime { get; set; }
        [StringLength(10)]
        public string Examiner { get; set; }
        public bool? Completion { get; set; }
        public bool? Printed { get; set; }
        public bool? IsPass { get; set; }
        [StringLength(6)]
        public string AccountMonth { get; set; }
        public bool GoldenGate { get; set; }
        public decimal OwnExpense { get; set; }
        public bool prnA4 { get; set; }
        public bool A4Printed { get; set; }
        public bool IsPrescription { get; set; }
        public bool BarCodePlusOne { get; set; }
        public long Paid { get; set; }
        public bool CheckOut { get; set; }
        public bool HealthPrinted { get; set; }
        public bool IsFobt { get; set; }
        [StringLength(10)]
        public string Payee { get; set; }
        public bool IsDialysis { get; set; }
        public bool prnB5 { get; set; }
        public bool B5Printed { get; set; }
        [StringLength(14)]
        public string BNO { get; set; }
    }
}