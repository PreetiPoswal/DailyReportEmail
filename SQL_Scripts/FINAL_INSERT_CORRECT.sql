/*==============================================================================
  FINAL CORRECT TEST DATA INSERT
  Server: 192.168.60.20 | DB: CENTRALSTORE_DEMO
==============================================================================*/

USE CENTRALSTORE_DEMO;
GO

-- Confirm test dept exists
SELECT DepartmentID, DepartmentName, Status
FROM Department WHERE DepartmentName = 'TEST_Dept_Demo';
GO

-- ============================================
-- INSERT 1: Demands (4 rows - alag status)
-- ============================================
DECLARE @DeptID NVARCHAR(50) = '4066';

INSERT INTO Demands (Demand_usr, Collegeid, Deptid, Dateofgen, DemandStatus)
VALUES ('TestUser','DEMO', @DeptID, GETDATE(), 'Open');

INSERT INTO Demands (Demand_usr, Collegeid, Deptid, Dateofgen, DemandStatus)
VALUES ('TestUser','DEMO', @DeptID, GETDATE(), 'Active');

INSERT INTO Demands (Demand_usr, Collegeid, Deptid, Dateofgen, DemandStatus)
VALUES ('TestUser','DEMO', @DeptID, GETDATE(), 'Closed');

INSERT INTO Demands (Demand_usr, Collegeid, Deptid, Dateofgen, DemandStatus)
VALUES ('TestUser','DEMO', @DeptID, GETDATE(), 'Rejected');

PRINT 'Demands inserted: 4 rows';
GO

-- ============================================
-- INSERT 2: PurchaseOrders (3 rows)
-- Department column = nvarchar, DeptID store karo
-- ============================================
INSERT INTO PurchaseOrders (Po_Gen_Date, PO_Grand_Total, PO_Total_Amt, Department)
VALUES (GETDATE(), '15000', '15000', '4066');

INSERT INTO PurchaseOrders (Po_Gen_Date, PO_Grand_Total, PO_Total_Amt, Department)
VALUES (GETDATE(), '25000', '25000', '4066');

INSERT INTO PurchaseOrders (Po_Gen_Date, PO_Grand_Total, PO_Total_Amt, Department)
VALUES (GETDATE(), '8000',  '8000',  '4066');

PRINT 'PurchaseOrders inserted: 3 rows';
GO

-- ============================================
-- INSERT 3: DMR_Form (2 rows)
-- DMR_Dept = nvarchar, Status = ACTIVE/LOCKED
-- ============================================
INSERT INTO DMR_Form (DMR_Code, DMR_Gen_Date, DMR_By, DMR_Dept, Status, DMR_TotalAmt)
VALUES ('TEST-DMR-001', GETDATE(), 'TestUser', '4066', 'ACTIVE', 5000);

INSERT INTO DMR_Form (DMR_Code, DMR_Gen_Date, DMR_By, DMR_Dept, Status, DMR_TotalAmt)
VALUES ('TEST-DMR-002', GETDATE(), 'TestUser', '4066', 'LOCKED', 7500);

PRINT 'DMR_Form inserted: 2 rows';
GO

-- ============================================
-- VERIFY: Data check
-- ============================================
DECLARE @DeptID NVARCHAR(50) = '4066';
DECLARE @Today  DATE = CAST(GETDATE() AS DATE);

SELECT 'Today Demands'  AS [Table], COUNT(*) AS [Count]
FROM Demands
WHERE Deptid = @DeptID AND CAST(Dateofgen AS DATE) = @Today

UNION ALL

SELECT 'Today POs', COUNT(*)
FROM PurchaseOrders
WHERE Department = @DeptID AND CAST(Po_Gen_Date AS DATE) = @Today

UNION ALL

SELECT 'Today DMRs', COUNT(*)
FROM DMR_Form
WHERE DMR_Dept = @DeptID AND CAST(DMR_Gen_Date AS DATE) = @Today;
GO

-- ============================================
-- SP RUN - Dono result sets dekho
-- ============================================
EXEC SP_DailyBackupReport;
GO
