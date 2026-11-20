/*==============================================================================
  STEP 1: Pehle yeh CENTRALSTORE_DEMO pe chalao
  Server: 192.168.60.20
  
  Agar koi column missing hai to error message clearly batayega.
  Ek ek section alag chalao (Ctrl+E).
==============================================================================*/

USE CENTRALSTORE_DEMO;
GO

-- ============================================
-- CHECK 1: Department table ka actual structure dekho
-- ============================================
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Department'
ORDER BY ORDINAL_POSITION;
GO

-- ============================================
-- CHECK 2: Demands table structure
-- ============================================
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Demands'
ORDER BY ORDINAL_POSITION;
GO

-- ============================================
-- CHECK 3: PurchaseOrders table structure
-- ============================================
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'PurchaseOrders'
ORDER BY ORDINAL_POSITION;
GO

-- ============================================
-- CHECK 4: DMR_Form table structure  
-- ============================================
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'DMR_Form'
ORDER BY ORDINAL_POSITION;
GO

-- ============================================
-- CHECK 5: BillPayments table structure
-- ============================================
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'BillPayments'
ORDER BY ORDINAL_POSITION;
GO

-- ============================================
-- CHECK 6: Existing departments list
-- ============================================
SELECT * FROM Department WHERE Status = 1;
GO

/*==============================================================================
  STEP 2: Upar wale checks chalao, column names confirm karo
  Phir neeche wale INSERT statements chalao

  Agar column names match hain:
  - Department    : DepartmentID, DepartmentName, Status
  - Demands       : Deptid, Status, CreatedDate
  - PurchaseOrders: DeptID, Status, TotalAmount, CreatedDate
  - DMR_Form      : DMR_Dept, Status, TotalAmount, CreatedDate
  - BillPayments  : DepartmentID, Status, Amount, CreatedDate
==============================================================================*/

-- ============================================
-- TEST DEPARTMENT INSERT
-- ============================================
IF NOT EXISTS (SELECT 1 FROM Department WHERE DepartmentName = 'TEST_Dept_Demo')
BEGIN
    INSERT INTO Department (DepartmentName, Status)
    VALUES ('TEST_Dept_Demo', 1);
    PRINT 'Test Department created';
END

DECLARE @DeptID INT = (SELECT DepartmentID FROM Department WHERE DepartmentName = 'TEST_Dept_Demo');
PRINT 'Department ID = ' + CAST(@DeptID AS VARCHAR);
GO

-- ============================================
-- TEST DEMANDS INSERT (aaj ki date se)
-- ============================================
DECLARE @DeptID INT = (SELECT DepartmentID FROM Department WHERE DepartmentName = 'TEST_Dept_Demo');

INSERT INTO Demands (Deptid, Status, CreatedDate) VALUES (@DeptID, 'Pending',  GETDATE());
INSERT INTO Demands (Deptid, Status, CreatedDate) VALUES (@DeptID, 'Approved', GETDATE());
INSERT INTO Demands (Deptid, Status, CreatedDate) VALUES (@DeptID, 'Rejected', GETDATE());
PRINT 'Demands inserted';
GO

-- ============================================
-- TEST PURCHASE ORDERS INSERT
-- ============================================
DECLARE @DeptID INT = (SELECT DepartmentID FROM Department WHERE DepartmentName = 'TEST_Dept_Demo');

INSERT INTO PurchaseOrders (DeptID, Status, TotalAmount, CreatedDate) VALUES (@DeptID, 'Pending',  15000.00, GETDATE());
INSERT INTO PurchaseOrders (DeptID, Status, TotalAmount, CreatedDate) VALUES (@DeptID, 'Approved', 25000.00, GETDATE());
INSERT INTO PurchaseOrders (DeptID, Status, TotalAmount, CreatedDate) VALUES (@DeptID, 'Locked',    8000.00, GETDATE());
PRINT 'Purchase Orders inserted';
GO

-- ============================================
-- TEST DMR INSERT
-- ============================================
DECLARE @DeptID INT = (SELECT DepartmentID FROM Department WHERE DepartmentName = 'TEST_Dept_Demo');

INSERT INTO DMR_Form (DMR_Dept, Status, TotalAmount, CreatedDate) VALUES (@DeptID, 'Pending',  5000.00, GETDATE());
INSERT INTO DMR_Form (DMR_Dept, Status, TotalAmount, CreatedDate) VALUES (@DeptID, 'Approved', 7500.00, GETDATE());
PRINT 'DMR inserted';
GO

-- ============================================
-- TEST BILL PAYMENTS INSERT
-- ============================================
DECLARE @DeptID INT = (SELECT DepartmentID FROM Department WHERE DepartmentName = 'TEST_Dept_Demo');

INSERT INTO BillPayments (DepartmentID, Status, Amount, CreatedDate) VALUES (@DeptID, 'Pending',  3500.00, GETDATE());
INSERT INTO BillPayments (DepartmentID, Status, Amount, CreatedDate) VALUES (@DeptID, 'Approved', 6000.00, GETDATE());
PRINT 'Bill Payments inserted';
GO

-- ============================================
-- FINAL: SP chalao aur result dekho
-- ============================================
EXEC SP_DailyBackupReport;
GO

/*==============================================================================
  EXPECTED RESULT SET 1 (Overall Summary):
    TodayDemand     = 3+ (aaj ke saare demands)
    TodayPO         = 3+ (aaj ke POs)
    TodayDMR        = 2+ 
    TodayBillPayment= 2+

  EXPECTED RESULT SET 2 (Department-wise):
    Row for 'TEST_Dept_Demo':
      TodayDemand = 3
      PendingDemand = 1
      ApprovedDemand = 1
      RejectedDemand = 1
      TodayPO = 3
      POAmount = 48000
      TodayDMR = 2
      DMRAmount = 12500
      TodayBillPayment = 2
      BillAmount = 9500

  Agar Result Set 2 nahi aa raha => SP update nahi hui hai.
==============================================================================*/

/*==============================================================================
  CLEANUP - Baad mein test data delete karna ho to yeh chalao:

DECLARE @DeptID INT = (SELECT DepartmentID FROM Department WHERE DepartmentName = 'TEST_Dept_Demo');
DELETE FROM BillPayments   WHERE DepartmentID = @DeptID;
DELETE FROM DMR_Form       WHERE DMR_Dept     = @DeptID;
DELETE FROM PurchaseOrders WHERE DeptID       = @DeptID;
DELETE FROM Demands        WHERE Deptid       = @DeptID;
DELETE FROM Department     WHERE DepartmentName = 'TEST_Dept_Demo';
PRINT 'Cleanup done';

==============================================================================*/
