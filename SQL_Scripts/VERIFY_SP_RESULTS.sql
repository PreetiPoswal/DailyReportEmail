/*==============================================================================
  Quick Verification: SP ke dono result sets alag-alag check karo
  
  Note: Pehle TEST_DATA_INSERT.sql run karo, phir yeh script chalao
==============================================================================*/

USE CENTRALSTORE_DEMO;
GO

DECLARE @Today DATE = CAST(GETDATE() AS DATE);

-- ============================================
-- VERIFICATION 1: Department table mein data hai?
-- ============================================
PRINT '--- Active Departments ---';
SELECT DepartmentID, DepartmentName, Status FROM Department WHERE Status = 1;

-- ============================================
-- VERIFICATION 2: Aaj ke demands kaun se hain?
-- ============================================
PRINT '--- Today Demands by Dept ---';
SELECT 
    D.DepartmentName,
    DEM.Status,
    COUNT(*) AS Count
FROM Demands DEM
JOIN Department D ON D.DepartmentID = DEM.Deptid
WHERE CAST(DEM.CreatedDate AS DATE) = @Today
GROUP BY D.DepartmentName, DEM.Status;

-- ============================================
-- VERIFICATION 3: Aaj ke POs kaun se hain?
-- ============================================
PRINT '--- Today POs by Dept ---';
SELECT 
    D.DepartmentName,
    PO.Status,
    COUNT(*) AS Count,
    SUM(PO.TotalAmount) AS TotalAmt
FROM PurchaseOrders PO
JOIN Department D ON D.DepartmentID = PO.DeptID
WHERE CAST(PO.CreatedDate AS DATE) = @Today
GROUP BY D.DepartmentName, PO.Status;

-- ============================================
-- VERIFICATION 4: SP Execute karke actual results dekho
-- ============================================
PRINT '--- Executing SP (Both Result Sets) ---';
EXEC SP_DailyBackupReport;
