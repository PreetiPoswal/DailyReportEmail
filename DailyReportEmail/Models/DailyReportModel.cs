using System;
using System.Collections.Generic;

namespace DailyReportEmail.Models
{
    public class DailyReportModel
    {
        //=========================================================================
        // HMPK PROJECT (Preserved for existing UI/Functionality - DO NOT REMOVE)
        //=========================================================================
        public DateTime ReportDate { get; set; }
        public decimal OnlineCollection { get; set; }
        public decimal CashCollection { get; set; }
        public int TodayDemand { get; set; }
        public int PendingDemand { get; set; }
        public int ApprovedDemand { get; set; }
        public int RejectedDemand { get; set; }

        public int TodayPO { get; set; }
        public int PendingPO { get; set; }
        public int ApprovedPO { get; set; }
        public int RejectedPO { get; set; }

        public decimal LockedPOAmount { get; set; }
        public decimal TotalPOAmount { get; set; }

        public int TodayDMR { get; set; }
        public int PendingDMR { get; set; }
        public int ApprovedDMR { get; set; }
        public int RejectedDMR { get; set; }
        public decimal TotalDMRAmount { get; set; }

        public int TodayBillPayment { get; set; }
        public int PendingBillPayment { get; set; }
        public int ApprovedBillPayment { get; set; }
        public int RejectedBillPayment { get; set; }
        public decimal TotalBillAmount { get; set; }

        public int NewRegisteredUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public List<HmpkDailyActivity> HmpkWeeklyActivity { get; set; } = new();
        public bool HmpkWeeklyHistoryAvailable { get; set; }

        //=========================================================================
        // TOOTHTECH SUMMARY (Global Metrics for Summary Cards)
        //=========================================================================
        public decimal ToothTech_TotalCashPaid { get; set; }
        public decimal ToothTech_TotalOnlinePaid { get; set; }
        public decimal ToothTech_OverallRevenue { get; set; }
        public int ToothTech_NewPatients { get; set; }
        public int ToothTech_OldPatients { get; set; }
        public int ToothTech_TotalFootfall { get; set; }

        // Extra Global Summary Fields specifically mapped for the new PDF Design
        public decimal TodaysCollection { get; set; }
        public decimal TodaysRevenue { get; set; }
        public decimal TodaysDiscount { get; set; }
        public bool ToothTechDiscountAvailable { get; set; }
        public List<SmileSyncDailyCollection> SmileSyncWeeklyCollections { get; set; } = new();
        public bool SmileSyncWeeklyHistoryAvailable { get; set; }

        //=========================================================================
        // FLAG 1: Department Summary List
        //=========================================================================
        public List<DepartmentMetric> Departments { get; set; } = new();

        //=========================================================================
        // FLAG 2: Multi-Result Set Lists (Preserved & Enhanced)
        //=========================================================================
        public List<Flag2DepartmentPaymentSummary> Flag2PaymentSummaries { get; set; } = new();
        public List<DiscountAlert> DiscountAlerts { get; set; } = new();
        public List<StaffStudentAlert> StaffStudentAlerts { get; set; } = new();

        //=========================================================================
        // FLAG 3: Today's Patient List
        //=========================================================================
        public List<TodayPatient> TodayPatients { get; set; } = new();

        //=========================================================================
        // FLAG 4: HOD Patient Group
        //=========================================================================
        public List<Flag4PatientListForHod> Flag4HodPatients { get; set; } = new();
        public List<HodPatientGroup> HodPatientGroups { get; set; } = new();

        //=========================================================================
        // HMPK DEPARTMENT-WISE SUMMARY (Dynamic - SP_DailyBackupReport Result Set 2)
        //=========================================================================
        public List<DepartmentWiseSummary> DepartmentSummaries { get; set; } = new();

        // PEON BOOK: all-time cards and daily status summary
        public PeonBookDailySummary PeonBookSummary { get; set; } = new();

        public MrdDailySummary MrdSummary { get; set; } = new();
        public MarshmallowDailySummary MarshmallowSummary { get; set; } = new();
        public PhrDailySummary PhrSummary { get; set; } = new();
    }

    public class SmileSyncDailyCollection
    {
        public DateTime Date { get; set; }
        public decimal Cash { get; set; }
        public decimal Online { get; set; }
    }

    public class HmpkDailyActivity
    {
        public DateTime Date { get; set; }
        public int Demands { get; set; }
        public int PurchaseOrders { get; set; }
        public int Dmr { get; set; }
        public int BillPayments { get; set; }
    }

    // ==========================================
    // FIXED: SummaryCard Definition Added Here!
    // ==========================================
    public class SummaryCard
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public string BackgroundColor { get; set; } = "#FFFFFF";
    }

    // ==========================================
    // FLAG 1 Sub-Model 
    // ==========================================
    public class DepartmentMetric
    {
        public long SNo { get; set; }
        public string? DepartmentNameID { get; set; }
        public string? DepartmentName { get; set; }
        public decimal CollectionCash { get; set; }
        public decimal CollectionOnline { get; set; }
        public int PatientNew { get; set; }
        public int PatientReVisit { get; set; }
        public decimal RevenueCreated { get; set; }
        public decimal RevenueCollected { get; set; }
        public decimal RevenueDiscount { get; set; }
        public string PrescribedPerformed { get; set; } = "-";
    }

    // ==========================================
    // FLAG 2 Sub-Models
    // ==========================================
    public class Flag2DepartmentPaymentSummary
    {
        public string Department { get; set; } = string.Empty;
        public string HodName { get; set; } = string.Empty;
        public decimal CashPaid { get; set; }
        public decimal OnlinePaid { get; set; }
        public decimal TotalPaid { get; set; }
    }

    public class DiscountAlert
    {
        public string? DepartmentName { get; set; }
        public string? PatientName { get; set; }
        public decimal Amount { get; set; }
        public decimal Discount { get; set; }
        public decimal PaidAmount { get; set; }
        public string? UHID { get; set; }
        public string? ApprovedBy { get; set; }
    }

    public class StaffStudentAlert
    {
        public string? DepartmentName { get; set; }
        public string? PatientName { get; set; }
        public decimal Amount { get; set; }
        public decimal PaidAmount { get; set; }
        public string? UHID { get; set; }
        public string? PatientType { get; set; }
    }

    // ==========================================
    // FLAG 3 Sub-Model 
    // ==========================================
    public class TodayPatient
    {
        public string? Department { get; set; }
        public string? UHID { get; set; }
        public string? Name { get; set; }
        public string? PatientType { get; set; }
        public string? Gender { get; set; }
        public string? Mobile { get; set; }
        public string? NewOld { get; set; }
        public string? RegisteredBy { get; set; }
        public DateTime VisitDate { get; set; }
    }

    // ==========================================
    // FLAG 4 Sub-Models
    // ==========================================
    public class Flag4PatientListForHod
    {
        public string Department { get; set; } = string.Empty;
        public string HodName { get; set; } = string.Empty;
        public string HodEmail { get; set; } = string.Empty;
        public string UHID { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string TreatmentGiven { get; set; } = string.Empty;
        public decimal BillAmount { get; set; }
        // Is property ko add karein compiler error hatane ke liye
        public decimal GrossAmount { get; set; }
    }

    public class HodPatientGroup
    {
        public string? HODName { get; set; }
        public string? Department { get; set; }
        public List<TodayPatient> Patients { get; set; } = new();
    }

    // ==========================================
    // HMPK Department-Wise Summary (Dynamic)
    // ==========================================
    public class DepartmentWiseSummary
    {
        public int DepartmentID { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        
        // Demand Metrics
        public int TodayDemand { get; set; }
        public int PendingDemand { get; set; }
        public int ApprovedDemand { get; set; }
        public int RejectedDemand { get; set; }
        
        // Purchase Order Metrics
        public int TodayPO { get; set; }
        public int PendingPO { get; set; }
        public int ApprovedPO { get; set; }
        public int RejectedPO { get; set; }
        public decimal POAmount { get; set; }
        
        // DMR Metrics
        public int TodayDMR { get; set; }
        public int PendingDMR { get; set; }
        public int ApprovedDMR { get; set; }
        public int RejectedDMR { get; set; }
        public decimal DMRAmount { get; set; }
        
        // Bill Payment Metrics
        public int TodayBillPayment { get; set; }
        public int PendingBillPayment { get; set; }
        public int ApprovedBillPayment { get; set; }
        public int RejectedBillPayment { get; set; }
        public decimal BillAmount { get; set; }
    }
}
