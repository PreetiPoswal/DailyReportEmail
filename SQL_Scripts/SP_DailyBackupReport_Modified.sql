/*==============================================================================
  Stored Procedure: SP_DailyBackupReport (Modified Version)
  
  Purpose: 
    Generate daily backup report with:
    1. Overall summary (existing functionality - PRESERVED)
    2. Department-wise dynamic summary (NEW FEATURE)
  
  Author: Modified for Dynamic Department Support
  Date: 2026-07-31
  
  Modifications:
    - Added second result set for department-wise breakdown
    - Dynamic department detection from Department master table
    - Zero-value handling for departments without transactions
    - No hardcoded department names
  
  Database Schema Requirements:
    - Department table with: DepartmentID, DepartmentName, Status
    - Demands table with: Deptid column
    - PurchaseOrders table with: DeptID column
    - DMR_Form table with: DMR_Dept column
    - BillPayments table (assumed to have DepartmentID column)
  
==============================================================================*/

ALTER PROCEDURE [dbo].[SP_DailyBackupReport]
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Today DATE = CAST(GETDATE() AS DATE);
    
    /*==========================================================================
      RESULT SET 1: OVERALL SUMMARY (EXISTING FUNCTIONALITY - PRESERVED)
      
      This section remains UNCHANGED to maintain backward compatibility
      Returns aggregate counts and amounts across all departments
    ==========================================================================*/
    
    SELECT 
        -- Demand Metrics
        ISNULL((SELECT COUNT(*) FROM Demands WHERE CAST(CreatedDate AS DATE) = @Today), 0) AS TodayDemand,
        ISNULL((SELECT COUNT(*) FROM Demands WHERE Status = 'Pending'), 0) AS PendingDemand,
        ISNULL((SELECT COUNT(*) FROM Demands WHERE Status = 'Approved'), 0) AS ApprovedDemand,
        ISNULL((SELECT COUNT(*) FROM Demands WHERE Status = 'Rejected'), 0) AS RejectedDemand,
        
        -- Purchase Order Metrics
        ISNULL((SELECT COUNT(*) FROM PurchaseOrders WHERE CAST(CreatedDate AS DATE) = @Today), 0) AS TodayPO,
        ISNULL((SELECT COUNT(*) FROM PurchaseOrders WHERE Status = 'Pending'), 0) AS PendingPO,
        ISNULL((SELECT COUNT(*) FROM PurchaseOrders WHERE Status = 'Approved'), 0) AS ApprovedPO,
        ISNULL((SELECT COUNT(*) FROM PurchaseOrders WHERE Status = 'Rejected'), 0) AS RejectedPO,
        ISNULL((SELECT SUM(TotalAmount) FROM PurchaseOrders WHERE Status = 'Locked'), 0) AS LockedPOAmount,
        ISNULL((SELECT SUM(TotalAmount) FROM PurchaseOrders), 0) AS TotalPOAmount,
        
        -- DMR Metrics
        ISNULL((SELECT COUNT(*) FROM DMR_Form WHERE CAST(CreatedDate AS DATE) = @Today), 0) AS TodayDMR,
        ISNULL((SELECT COUNT(*) FROM DMR_Form WHERE Status = 'Pending'), 0) AS PendingDMR,
        ISNULL((SELECT COUNT(*) FROM DMR_Form WHERE Status = 'Approved'), 0) AS ApprovedDMR,
        ISNULL((SELECT COUNT(*) FROM DMR_Form WHERE Status = 'Rejected'), 0) AS RejectedDMR,
        ISNULL((SELECT SUM(TotalAmount) FROM DMR_Form), 0) AS TotalDMRAmount,
        
        -- Bill Payment Metrics
        ISNULL((SELECT COUNT(*) FROM BillPayments WHERE CAST(CreatedDate AS DATE) = @Today), 0) AS TodayBillPayment,
        ISNULL((SELECT COUNT(*) FROM BillPayments WHERE Status = 'Pending'), 0) AS PendingBillPayment,
        ISNULL((SELECT COUNT(*) FROM BillPayments WHERE Status = 'Approved'), 0) AS ApprovedBillPayment,
        ISNULL((SELECT COUNT(*) FROM BillPayments WHERE Status = 'Rejected'), 0) AS RejectedBillPayment,
        ISNULL((SELECT SUM(Amount) FROM BillPayments), 0) AS TotalBillAmount,
        
        -- User Metrics
        ISNULL((SELECT COUNT(*) FROM Users WHERE CAST(CreatedDate AS DATE) = @Today), 0) AS NewRegisteredUsers,
        ISNULL((SELECT COUNT(*) FROM Users WHERE IsActive = 1), 0) AS ActiveUsers,
        ISNULL((SELECT COUNT(*) FROM Users WHERE IsActive = 0), 0) AS InactiveUsers;
    
    
    /*==========================================================================
      RESULT SET 2: DEPARTMENT-WISE DYNAMIC SUMMARY (NEW FEATURE)
      
      This section dynamically generates department-wise breakdown
      - Automatically includes all active departments
      - Shows zero values for departments without transactions
      - No hardcoded department names
      - Efficient GROUP BY approach (no loops)
    ==========================================================================*/
    
    SELECT 
        D.DepartmentID,
        D.DepartmentName,
        
        -- Demand Counts by Department
        ISNULL(DEM_TODAY.TodayDemand, 0) AS TodayDemand,
        ISNULL(DEM_PENDING.PendingDemand, 0) AS PendingDemand,
        ISNULL(DEM_APPROVED.ApprovedDemand, 0) AS ApprovedDemand,
        ISNULL(DEM_REJECTED.RejectedDemand, 0) AS RejectedDemand,
        
        -- Purchase Order Counts by Department
        ISNULL(PO_TODAY.TodayPO, 0) AS TodayPO,
        ISNULL(PO_PENDING.PendingPO, 0) AS PendingPO,
        ISNULL(PO_APPROVED.ApprovedPO, 0) AS ApprovedPO,
        ISNULL(PO_REJECTED.RejectedPO, 0) AS RejectedPO,
        ISNULL(PO_AMOUNT.POAmount, 0) AS POAmount,
        
        -- DMR Counts by Department
        ISNULL(DMR_TODAY.TodayDMR, 0) AS TodayDMR,
        ISNULL(DMR_PENDING.PendingDMR, 0) AS PendingDMR,
        ISNULL(DMR_APPROVED.ApprovedDMR, 0) AS ApprovedDMR,
        ISNULL(DMR_REJECTED.RejectedDMR, 0) AS RejectedDMR,
        ISNULL(DMR_AMOUNT.DMRAmount, 0) AS DMRAmount,
        
        -- Bill Payment Counts by Department
        ISNULL(BILL_TODAY.TodayBillPayment, 0) AS TodayBillPayment,
        ISNULL(BILL_PENDING.PendingBillPayment, 0) AS PendingBillPayment,
        ISNULL(BILL_APPROVED.ApprovedBillPayment, 0) AS ApprovedBillPayment,
        ISNULL(BILL_REJECTED.RejectedBillPayment, 0) AS RejectedBillPayment,
        ISNULL(BILL_AMOUNT.BillAmount, 0) AS BillAmount
        
    FROM 
        Department D
        
    -- Demand: Today's Count
    LEFT JOIN (
        SELECT Deptid, COUNT(*) AS TodayDemand
        FROM Demands
        WHERE CAST(CreatedDate AS DATE) = @Today
        GROUP BY Deptid
    ) DEM_TODAY ON D.DepartmentID = DEM_TODAY.Deptid
    
    -- Demand: Pending Count
    LEFT JOIN (
        SELECT Deptid, COUNT(*) AS PendingDemand
        FROM Demands
        WHERE Status = 'Pending'
        GROUP BY Deptid
    ) DEM_PENDING ON D.DepartmentID = DEM_PENDING.Deptid
    
    -- Demand: Approved Count
    LEFT JOIN (
        SELECT Deptid, COUNT(*) AS ApprovedDemand
        FROM Demands
        WHERE Status = 'Approved'
        GROUP BY Deptid
    ) DEM_APPROVED ON D.DepartmentID = DEM_APPROVED.Deptid
    
    -- Demand: Rejected Count
    LEFT JOIN (
        SELECT Deptid, COUNT(*) AS RejectedDemand
        FROM Demands
        WHERE Status = 'Rejected'
        GROUP BY Deptid
    ) DEM_REJECTED ON D.DepartmentID = DEM_REJECTED.Deptid
    
    -- Purchase Orders: Today's Count
    LEFT JOIN (
        SELECT DeptID, COUNT(*) AS TodayPO
        FROM PurchaseOrders
        WHERE CAST(CreatedDate AS DATE) = @Today
        GROUP BY DeptID
    ) PO_TODAY ON D.DepartmentID = PO_TODAY.DeptID
    
    -- Purchase Orders: Pending Count
    LEFT JOIN (
        SELECT DeptID, COUNT(*) AS PendingPO
        FROM PurchaseOrders
        WHERE Status = 'Pending'
        GROUP BY DeptID
    ) PO_PENDING ON D.DepartmentID = PO_PENDING.DeptID
    
    -- Purchase Orders: Approved Count
    LEFT JOIN (
        SELECT DeptID, COUNT(*) AS ApprovedPO
        FROM PurchaseOrders
        WHERE Status = 'Approved'
        GROUP BY DeptID
    ) PO_APPROVED ON D.DepartmentID = PO_APPROVED.DeptID
    
    -- Purchase Orders: Rejected Count
    LEFT JOIN (
        SELECT DeptID, COUNT(*) AS RejectedPO
        FROM PurchaseOrders
        WHERE Status = 'Rejected'
        GROUP BY DeptID
    ) PO_REJECTED ON D.DepartmentID = PO_REJECTED.DeptID
    
    -- Purchase Orders: Total Amount
    LEFT JOIN (
        SELECT DeptID, SUM(TotalAmount) AS POAmount
        FROM PurchaseOrders
        GROUP BY DeptID
    ) PO_AMOUNT ON D.DepartmentID = PO_AMOUNT.DeptID
    
    -- DMR: Today's Count
    LEFT JOIN (
        SELECT DMR_Dept, COUNT(*) AS TodayDMR
        FROM DMR_Form
        WHERE CAST(CreatedDate AS DATE) = @Today
        GROUP BY DMR_Dept
    ) DMR_TODAY ON D.DepartmentID = DMR_TODAY.DMR_Dept
    
    -- DMR: Pending Count
    LEFT JOIN (
        SELECT DMR_Dept, COUNT(*) AS PendingDMR
        FROM DMR_Form
        WHERE Status = 'Pending'
        GROUP BY DMR_Dept
    ) DMR_PENDING ON D.DepartmentID = DMR_PENDING.DMR_Dept
    
    -- DMR: Approved Count
    LEFT JOIN (
        SELECT DMR_Dept, COUNT(*) AS ApprovedDMR
        FROM DMR_Form
        WHERE Status = 'Approved'
        GROUP BY DMR_Dept
    ) DMR_APPROVED ON D.DepartmentID = DMR_APPROVED.DMR_Dept
    
    -- DMR: Rejected Count
    LEFT JOIN (
        SELECT DMR_Dept, COUNT(*) AS RejectedDMR
        FROM DMR_Form
        WHERE Status = 'Rejected'
        GROUP BY DMR_Dept
    ) DMR_REJECTED ON D.DepartmentID = DMR_REJECTED.DMR_Dept
    
    -- DMR: Total Amount
    LEFT JOIN (
        SELECT DMR_Dept, SUM(TotalAmount) AS DMRAmount
        FROM DMR_Form
        GROUP BY DMR_Dept
    ) DMR_AMOUNT ON D.DepartmentID = DMR_AMOUNT.DMR_Dept
    
    -- Bill Payments: Today's Count
    LEFT JOIN (
        SELECT DepartmentID, COUNT(*) AS TodayBillPayment
        FROM BillPayments
        WHERE CAST(CreatedDate AS DATE) = @Today
        GROUP BY DepartmentID
    ) BILL_TODAY ON D.DepartmentID = BILL_TODAY.DepartmentID
    
    -- Bill Payments: Pending Count
    LEFT JOIN (
        SELECT DepartmentID, COUNT(*) AS PendingBillPayment
        FROM BillPayments
        WHERE Status = 'Pending'
        GROUP BY DepartmentID
    ) BILL_PENDING ON D.DepartmentID = BILL_PENDING.DepartmentID
    
    -- Bill Payments: Approved Count
    LEFT JOIN (
        SELECT DepartmentID, COUNT(*) AS ApprovedBillPayment
        FROM BillPayments
        WHERE Status = 'Approved'
        GROUP BY DepartmentID
    ) BILL_APPROVED ON D.DepartmentID = BILL_APPROVED.DepartmentID
    
    -- Bill Payments: Rejected Count
    LEFT JOIN (
        SELECT DepartmentID, COUNT(*) AS RejectedBillPayment
        FROM BillPayments
        WHERE Status = 'Rejected'
        GROUP BY DepartmentID
    ) BILL_REJECTED ON D.DepartmentID = BILL_REJECTED.DepartmentID
    
    -- Bill Payments: Total Amount
    LEFT JOIN (
        SELECT DepartmentID, SUM(Amount) AS BillAmount
        FROM BillPayments
        GROUP BY DepartmentID
    ) BILL_AMOUNT ON D.DepartmentID = BILL_AMOUNT.DepartmentID
    
    WHERE 
        D.Status = 1  -- Only active departments
        
    ORDER BY 
        D.DepartmentName;  -- Alphabetical ordering for better readability
    
END;
GO

/*==============================================================================
  USAGE EXAMPLE:
  
  EXEC SP_DailyBackupReport;
  
  RETURNS:
    - Result Set 1: Overall summary (backward compatible)
    - Result Set 2: Department-wise breakdown (new feature)
  
==============================================================================*/

/*==============================================================================
  TESTING NOTES:
  
  1. Test with existing departments - should see data
  2. Add a new department to Department table - should automatically appear
  3. Test with department having no transactions - should show zeros
  4. Verify all LEFT JOINs working correctly
  5. Check performance with large datasets
  
  ASSUMPTIONS:
  - Department.Status = 1 means active
  - BillPayments table has DepartmentID column (adjust if different)
  - Date columns are named CreatedDate (adjust if different)
  - Status values are: 'Pending', 'Approved', 'Rejected', 'Locked'
  
==============================================================================*/
