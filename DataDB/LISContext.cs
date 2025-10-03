using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using LIS_Middleware.Models;

#nullable disable

namespace LIS_Middleware.DataDB
{
    public partial class LISContext : DbContext
    {
    public LISContext()
        {
        }

        public LISContext(DbContextOptions<LISContext> options)
            : base(options)
        {
        }

        // public virtual DbSet<ExOrder> ExOrders { get; set; }
        // public virtual DbSet<TempOrder> TempOrders { get; set; }
        public virtual DbSet<TestDOC> TestDOCs { get; set; }
        public virtual DbSet<TestDetail> TestDetails { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "Chinese_Taiwan_Stroke_CI_AS");

            modelBuilder.Entity<TestDOC>(entity =>
            {
                entity.HasKey(e => e.SNO);
                entity.ToTable("TestDOC");
                entity.Property(e => e.SNO).HasMaxLength(15);
                entity.Property(e => e.OrderNo).HasMaxLength(12);
                entity.Property(e => e.CustID).HasMaxLength(10);
                entity.Property(e => e.SubName).HasMaxLength(20);
                entity.Property(e => e.SubBirthDay).HasMaxLength(9);
                entity.Property(e => e.SubAge).HasMaxLength(3);
                entity.Property(e => e.SubIDNO).HasMaxLength(10);
                entity.Property(e => e.SubGender).HasMaxLength(1);
                entity.Property(e => e.MedicalNo).HasMaxLength(30);
                entity.Property(e => e.SpecimenConditions).HasMaxLength(20);
                entity.Property(e => e.TestSpecies).HasMaxLength(3);
                entity.Property(e => e.RecDate).HasMaxLength(9);
                entity.Property(e => e.InspDate).HasMaxLength(9);
                entity.Property(e => e.ReportDate).HasMaxLength(9);
                entity.Property(e => e.PickDate).HasMaxLength(15);
                entity.Property(e => e.RegEmp).HasMaxLength(10);
                entity.Property(e => e.Payment).HasMaxLength(1);
                entity.Property(e => e.Tel).HasMaxLength(10);
                entity.Property(e => e.Address).HasMaxLength(50);
                entity.Property(e => e.Reviewers).HasMaxLength(10);
                entity.Property(e => e.AuditDay).HasMaxLength(9);
                entity.Property(e => e.AuditTime).HasMaxLength(8);
                entity.Property(e => e.Examiner).HasMaxLength(10);
                entity.Property(e => e.AccountMonth).HasMaxLength(6);
                entity.Property(e => e.Payee).HasMaxLength(10);
                entity.Property(e => e.BNO).HasMaxLength(14);
                // 其他型別 EF 會自動對應
            });

            modelBuilder.Entity<TestDetail>(entity =>
            {
                entity.HasKey(e => new { e.SNO, e.ItemID });
                entity.ToTable("TestDetail");
                entity.Property(e => e.SNO).HasMaxLength(15);
                entity.Property(e => e.ItemID).HasMaxLength(20);
                entity.Property(e => e.SetID).HasMaxLength(20);
                entity.Property(e => e.Name).HasMaxLength(50);
                entity.Property(e => e.Result).HasMaxLength(50);
                entity.Property(e => e.Interpretation).HasMaxLength(5);
                entity.Property(e => e.Price).HasColumnType("decimal(8,2)");
                entity.Property(e => e.NHI_Price);
                entity.Property(e => e.Discount);
                entity.Property(e => e.NHI_ID).HasMaxLength(20);
                entity.Property(e => e.IsNHI);
                entity.Property(e => e.IsPrint);
                entity.Property(e => e.TestID).HasMaxLength(12);
                entity.Property(e => e.SubID);
                entity.Property(e => e.Unquoted);
                entity.Property(e => e.KeyIn).HasMaxLength(10);
                entity.Property(e => e.Recheck);
                entity.Property(e => e.Sample_KindID);
                entity.Property(e => e.IsExcep);
                entity.Property(e => e.OutSource).HasMaxLength(2);
                entity.Property(e => e.Agio);
                entity.Property(e => e.NonPricing);
                entity.Property(e => e.IsChkD);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
