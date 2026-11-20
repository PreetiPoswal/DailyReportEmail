/*==============================================================================
  SP_DailyBackupReport - FINAL VERSION
  Real column names se updated:
  
  Table          | Date col      | Status col    | Dept col    | Amount col
  ---------------|---------------|---------------|-------------|-------------
  Demands        | Dateofgen     | DemandStatus  | Deptid      | -
  PurchaseOrders | Po_Gen_Date   | (no status)   | Department  | PO_Total_Amt
  DMR_Form       | DMR_Gen_Date  | Status        | DMR_Dept    | DMR_TotalAmt
  Department     | -             | Status='Active'| DepartmentID| -

  DemandStatus values : 'Open', 'Active', 'Closed', 'Rejected', 'Complete', 'Inactive'
  DMR Status values   : 'ACTIVE', 'LOCKED', 'CANCEL'
  Department.Status   : 'Active' (nvarchar)

  Note: Deptid (Demands) and Department (PO) store DepartmentID as nvarchar
        DMR_Dept also nvarchar
        JOIN mein CAST karenge
==============================================================================*/

USE CENTRALSTORE_DEMO;
GO

ALTER PROCEDURE [dbo].[SP_DailyBackupReport]
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(GETDATE() AS DATE);

    /*==========================================================================
      RESULT SET 1: OVERALL SUMMARY
    ==========================================================================*/
    SELECT
        -- Demand Metrics (DemandStatus: Open/Active = Pending, Closed/Complete = Approved)
        ISNULL((SELECT COUNT(*) FROM Demands  WHERE CAST(Dateofgen AS DATE) = @Today), 0)                    AS TodayDemand,
        ISNULL((SELECT COUNT(*) FROM Demands  WHERE DemandStatus IN ('Open','Active')), 0)                   AS PendingDemand,
        ISNULL((SELECT COUNT(*) FROM Demands  WHERE DemandStatus IN ('Closed','Complete')), 0)               AS ApprovedDemand,
        ISNULL((SELECT COUNT(*) FROM Demands  WHERE DemandStatus = 'Rejected'), 0)                           AS RejectedDemand,

        -- PO Metrics (PO has no Status col — count by date only; amounts from PO_Total_Amt)
        ISNULL((SELECT COUNT(*) FROM PurchaseOrders WHERE CAST(Po_Gen_Date AS DATE) = @Today), 0)            AS TodayPO,
        0                                                                                                     AS PendingPO,
        0                                                                                                     AS ApprovedPO,
        0                                                                                                     AS RejectedPO,
        ISNULL((SELECT SUM(CAST(ISNULL(PO_Total_Amt,'0') AS DECIMAL(18,2))) FROM PurchaseOrders), 0)         AS LockedPOAmount,
        ISNULL((SELECT SUM(CAST(ISNULL(PO_Total_Amt,'0') AS DECIMAL(18,2))) FROM PurchaseOrders), 0)         AS TotalPOAmount,

        -- DMR Metrics
        ISNULL((SELECT COUNT(*) FROM DMR_Form WHERE CAST(DMR_Gen_Date AS DATE) = @Today), 0)                 AS TodayDMR,
        ISNULL((SELECT COUNT(*) FROM DMR_Form WHERE Status = 'ACTIVE'), 0)                                   AS PendingDMR,
        ISNULL((SELECT COUNT(*) FROM DMR_Form WHERE Status = 'LOCKED'), 0)                                   AS ApprovedDMR,
        ISNULL((SELECT COUNT(*) FROM DMR_Form WHERE Status = 'CANCEL'), 0)                                   AS RejectedDMR,
        ISNULL((SELECT SUM(CAST(ISNULL(DMR_TotalAmt,'0') AS DECIMAL(18,2))) FROM DMR_Form), 0)               AS TotalDMRAmount,

        -- Bill Payment (placeholder — update when Bill table confirmed)
        0 AS TodayBillPayment,
        0 AS PendingBillPayment,
        0 AS ApprovedBillPayment,
        0 AS RejectedBillPayment,
        0 AS TotalBillAmount,

        -- Users (existing SP se preserve karo — adjust if needed)
        0 AS NewRegisteredUsers,
        0 AS ActiveUsers,
        0 AS InactiveUsers;

    /*==========================================================================
      RESULT SET 2: DEPARTMENT-WISE DYNAMIC SUMMARY
      Note: Deptid/Department columns store DepartmentID as nvarchar,
            so we CAST DepartmentID to nvarchar for JOIN
    ==========================================================================*/
    SELECT
        D.DepartmentID,
        D.DepartmentName,

        -- Demand Counts
        ISNULL(DEM_TODAY.TodayDemand,    0) AS TodayDemand,
        ISNULL(DEM_PENDING.PendingDemand, 0) AS PendingDemand,
        ISNULL(DEM_APPROVED.ApprovedDemand, 0) AS ApprovedDemand,
        ISNULL(DEM_REJECTED.RejectedDemand, 0) AS RejectedDemand,

        -- PO Counts
        ISNULL(PO_TODAY.TodayPO,   0)   AS TodayPO,
        0                               AS PendingPO,
        0                               AS ApprovedPO,
        0                               AS RejectedPO,
        ISNULL(PO_AMOUNT.POAmount, 0)   AS POAmount,

        -- DMR Counts
        ISNULL(DMR_TODAY.TodayDMR,      0) AS TodayDMR,
        ISNULL(DMR_ACTIVE.PendingDMR,   0) AS PendingDMR,
        ISNULL(DMR_LOCKED.ApprovedDMR,  0) AS ApprovedDMR,
        ISNULL(DMR_CANCEL.RejectedDMR,  0) AS RejectedDMR,
        ISNULL(DMR_AMOUNT.DMRAmount,    0) AS DMRAmount,

        -- Bill (placeholder)
        0 AS TodayBillPayment,
        0 AS PendingBillPayment,
        0 AS ApprovedBillPayment,
        0 AS RejectedBillPayment,
        0 AS BillAmount

    FROM Department D

    -- Demands: Today
    LEFT JOIN (
        SELECT Deptid, COUNT(*) AS TodayDemand
        FROM Demands
        WHERE CAST(Dateofgen AS DATE) = @Today
        GROUP BY Deptid
    ) DEM_TODAY     ON CAST(D.DepartmentID AS NVARCHAR) = DEM_TODAY.Deptid

    -- Demands: Pending (Open + Active)
    LEFT JOIN (
        SELECT Deptid, COUNT(*) AS PendingDemand
        FROM Demands
        WHERE DemandStatus IN ('Open','Active')
        GROUP BY Deptid
    ) DEM_PENDING   ON CAST(D.DepartmentID AS NVARCHAR) = DEM_PENDING.Deptid

    -- Demands: Approved (Closed + Complete)
    LEFT JOIN (
        SELECT Deptid, COUNT(*) AS ApprovedDemand
        FROM Demands
        WHERE DemandStatus IN ('Closed','Complete')
        GROUP BY Deptid
    ) DEM_APPROVED  ON CAST(D.DepartmentID AS NVARCHAR) = DEM_APPROVED.Deptid

    -- Demands: Rejected
    LEFT JOIN (
        SELECT Deptid, COUNT(*) AS RejectedDemand
        FROM Demands
        WHERE DemandStatus = 'Rejected'
        GROUP BY Deptid
    ) DEM_REJECTED  ON CAST(D.DepartmentID AS NVARCHAR) = DEM_REJECTED.Deptid

    -- PO: Today
    LEFT JOIN (
        SELECT Department, COUNT(*) AS TodayPO
        FROM PurchaseOrders
        WHERE CAST(Po_Gen_Date AS DATE) = @Today
        GROUP BY Department
    ) PO_TODAY      ON CAST(D.DepartmentID AS NVARCHAR) = PO_TODAY.Department

    -- PO: Total Amount
    LEFT JOIN (
        SELECT Department,
               SUM(CAST(ISNULL(PO_Total_Amt,'0') AS DECIMAL(18,2))) AS POAmount
        FROM PurchaseOrders
        GROUP BY Department
    ) PO_AMOUNT     ON CAST(D.DepartmentID AS NVARCHAR) = PO_AMOUNT.Department

    -- DMR: Today
    LEFT JOIN (
        SELECT DMR_Dept, COUNT(*) AS TodayDMR
        FROM DMR_Form
        WHERE CAST(DMR_Gen_Date AS DATE) = @Today
        GROUP BY DMR_Dept
    ) DMR_TODAY     ON CAST(D.DepartmentID AS NVARCHAR) = DMR_TODAY.DMR_Dept

    -- DMR: Active (Pending jaisa)
    LEFT JOIN (
        SELECT DMR_Dept, COUNT(*) AS PendingDMR
        FROM DMR_Form
        WHERE Status = 'ACTIVE'
        GROUP BY DMR_Dept
    ) DMR_ACTIVE    ON CAST(D.DepartmentID AS NVARCHAR) = DMR_ACTIVE.DMR_Dept

    -- DMR: Locked (Approved jaisa)
    LEFT JOIN (
        SELECT DMR_Dept, COUNT(*) AS ApprovedDMR
        FROM DMR_Form
        WHERE Status = 'LOCKED'
        GROUP BY DMR_Dept
    ) DMR_LOCKED    ON CAST(D.DepartmentID AS NVARCHAR) = DMR_LOCKED.DMR_Dept

    -- DMR: Cancel (Rejected jaisa)
    LEFT JOIN (
        SELECT DMR_Dept, COUNT(*) AS RejectedDMR
        FROM DMR_Form
        WHERE Status = 'CANCEL'
        GROUP BY DMR_Dept
    ) DMR_CANCEL    ON CAST(D.DepartmentID AS NVARCHAR) = DMR_CANCEL.DMR_Dept

    -- DMR: Total Amount
    LEFT JOIN (
        SELECT DMR_Dept,
               SUM(CAST(ISNULL(DMR_TotalAmt,'0') AS DECIMAL(18,2))) AS DMRAmount
        FROM DMR_Form
        GROUP BY DMR_Dept
    ) DMR_AMOUNT    ON CAST(D.DepartmentID AS NVARCHAR) = DMR_AMOUNT.DMR_Dept

    WHERE D.Status = 'Active'   -- nvarchar 'Active', not int 1
    ORDER BY D.DepartmentName;

END;
GO

-- Test karo
EXEC SP_DailyBackupReport;
GO
