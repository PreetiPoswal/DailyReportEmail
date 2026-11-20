/*==============================================================================
  CORRECT TEST DATA INSERT - Based on actual column names
  Server: 192.168.60.20 | DB: CENTRALSTORE_DEMO
  
  Pehle STEP A chalao (columns confirm karo), phir STEP B
==============================================================================*/

USE CENTRALSTORE_DEMO;
GO

-- ============================================
-- STEP A: PurchaseOrders ka DeptID column confirm karo
-- ============================================
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'PurchaseOrders' 
  AND (COLUMN_NAME LIKE '%Dept%' OR COLUMN_NAME LIKE '%Dep%' OR COLUMN_NAME LIKE '%Center%')
ORDER BY ORDINAL_POSITION;

-- DMR_Form ka DeptID column confirm karo  
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'DMR_Form' 
  AND (COLUMN_NAME LIKE '%Dept%' OR COLUMN_NAME LIKE '%Dep%' OR COLUMN_NAME LIKE '%Center%')
ORDER BY ORDINAL_POSITION;

-- DemandStatus ke actual values kya hain?
SELECT DISTINCT DemandStatus FROM Demands WHERE DemandStatus IS NOT NULL;

-- DMR Status ke actual values kya hain?
SELECT DISTINCT Status FROM DMR_Form WHERE Status IS NOT NULL;
GO

-- ============================================
-- STEP B: Test Department use karo (ID = 4066 jo pehle bana)
-- Ya koi existing active department use karo
-- ============================================

-- Existing active departments dekho (inme se koi ek use karenge)
SELECT TOP 5 DepartmentID, DepartmentName, Status 
FROM Department 
WHERE Status = 'Active'
ORDER BY DepartmentID;
GO

-- ============================================
-- STEP C: Test Demand Insert (correct columns)
-- ============================================
DECLARE @DeptID   NVARCHAR(50) = '4066';   -- TEST_Dept_Demo ka ID
DECLARE @College  NVARCHAR(50) = 'DEMO';   -- sample data se dekha
DECLARE @Center   NVARCHAR(50) = NULL;

-- Demand 1: "Open" status (Pending jaisa)
INSERT INTO Demands 
    (Demand_usr, Collegeid, Deptid, Centerid, Dateofgen, DemandStatus)
VALUES 
    ('TestUser', @College, @DeptID, @Center, GETDATE(), 'Open');

-- Demand 2: "Closed" status (Approved jaisa)  
INSERT INTO Demands 
    (Demand_usr, Collegeid, Deptid, Centerid, Dateofgen, DemandStatus)
VALUES 
    ('TestUser', @College, @DeptID, @Center, GETDATE(), 'Closed');

-- Demand 3: "Rejected" status
INSERT INTO Demands 
    (Demand_usr, Collegeid, Deptid, Centerid, Dateofgen, DemandStatus)
VALUES 
    ('TestUser', @College, @DeptID, @Center, GETDATE(), 'Rejected');

SELECT 'Demands inserted: ' + CAST(@@ROWCOUNT AS VARCHAR) AS Result;
GO

-- ============================================
-- STEP D: Test PurchaseOrder Insert
-- (DeptID column name PO table mein confirm karo STEP A se)
-- ============================================
-- Sample row se dekha: VendorID=23, Dept column "DepartID" ya "Dept" hoga
-- Uncomment karo sahi wala:

/*
DECLARE @DeptID NVARCHAR(50) = '4066';

INSERT INTO PurchaseOrders 
    (Po_Gen_Date, PO_Grand_Total, PO_Total_Amt, VendorID, DepartID)  -- DepartID confirm karo
VALUES 
    (GETDATE(), '48000', '48000', '1', @DeptID);

SELECT 'PO inserted: ' + CAST(@@ROWCOUNT AS VARCHAR) AS Result;
*/
GO

-- ============================================
-- STEP E: Test DMR Insert
-- Sample row se: Status='LOCKED', Centerid column hai
-- ============================================
/*
DECLARE @DeptID NVARCHAR(50) = '4066';

INSERT INTO DMR_Form 
    (DMR_Code, DMR_Gen_Date, DMR_By, Centerid, Status, DMR_TotalAmt)
VALUES 
    ('TEST-DMR-001', GETDATE(), 'TestUser', @DeptID, 'LOCKED', 5000);

SELECT 'DMR inserted: ' + CAST(@@ROWCOUNT AS VARCHAR) AS Result;
*/
GO

-- ============================================
-- VERIFICATION: Data insert hua kya?
-- ============================================
DECLARE @DeptID NVARCHAR(50) = '4066';

SELECT 'Demands Count Today' AS [Check], COUNT(*) AS [Value]
FROM Demands 
WHERE Deptid = @DeptID 
  AND CAST(Dateofgen AS DATE) = CAST(GETDATE() AS DATE)

UNION ALL

SELECT 'Demands by Status', COUNT(*)
FROM Demands 
WHERE Deptid = @DeptID;
GO
