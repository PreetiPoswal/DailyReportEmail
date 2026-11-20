/*==============================================================================
  Test Script: Insert Test Data and Verify SP_DailyBackupReport
  
  Purpose: 
    - Create test department
    - Insert test transactions
    - Execute stored procedure to verify department-wise data
    - Display results
  
  Database: CENTRALSTORE_DEMO
  Server: 192.168.60.20
  
  Note: Run this script AFTER deploying the modified SP_DailyBackupReport
==============================================================================*/

USE CENTRALSTORE_DEMO;
GO

PRINT '========================================';
PRINT 'Starting Test Data Creation';
PRINT '========================================';
PRINT '';

-- ============================================
-- STEP 1: Create Test Department
-- ============================================
PRINT '--- Step 1: Creating Test Department ---';

-- Check if test department already exists
IF NOT EXISTS (SELECT 1 FROM Department WHERE DepartmentName = 'Test IT Department')
BEGIN
    INSERT INTO Department (DepartmentName, Status)
    VALUES ('Test IT Department', 1);  -- 1 = Active
    
    PRINT '✓ Test Department created: Test IT Department';
END
ELSE
BEGIN
    PRINT '⚠ Test Department already exists: Test IT Department';
END

DECLARE @TestDeptID INT;
SELECT @TestDeptID = DepartmentID FROM Department WHERE DepartmentName = 'Test IT Department';
PRINT 'Test Department ID: ' + CAST(@TestDeptID AS VARCHAR(10));
PRINT '';

-- ============================================
-- STEP 2: Insert Test Demands
-- ============================================
PRINT '--- Step 2: Inserting Test Demands ---';

-- Today's Demand (Pending)
INSERT INTO Demands (Deptid, Status, CreatedDate)
VALUES (@TestDeptID, 'Pending', GETDATE());

-- Today's Demand (Approved)
INSERT INTO Demands (Deptid, Status, CreatedDate)
VALUES (@TestDeptID, 'Approved', GETDATE());

-- Yesterday's Demand (for testing date filter)
INSERT INTO Demands (Deptid, Status, CreatedDate)
VALUES (@TestDeptID, 'Pending', DATEADD(DAY, -1, GETDATE()));

-- Rejected Demand
INSERT INTO Demands (Deptid, Status, CreatedDate)
VALUES (@TestDeptID, 'Rejected', DATEADD(DAY, -2, GETDATE()));

PRINT '✓ Inserted 4 test demands';
PRINT '  - 2 Today (1 Pending, 1 Approved)';
PRINT '  - 1 Yesterday (Pending)';
PRINT '  - 1 Old (Rejected)';
PRINT '';

-- ============================================
-- STEP 3: Insert Test Purchase Orders
-- ============================================
PRINT '--- Step 3: Inserting Test Purchase Orders ---';

-- Today's PO (Pending)
INSERT INTO PurchaseOrders (DeptID, Status, TotalAmount, CreatedDate)
VALUES (@TestDeptID, 'Pending', 5000.00, GETDATE());

-- Today's PO (Approved)
INSERT INTO PurchaseOrders (DeptID, Status, TotalAmount, CreatedDate)
VALUES (@TestDeptID, 'Approved', 10000.00, GETDATE());

-- Locked PO
INSERT INTO PurchaseOrders (DeptID, Status, TotalAmount, CreatedDate)
VALUES (@TestDeptID, 'Locked', 7500.00, DATEADD(DAY, -1, GETDATE()));

PRINT '✓ Inserted 3 test purchase orders';
PRINT '  - 2 Today (Pending: 5000, Approved: 10000)';
PRINT '  - 1 Locked (7500)';
PRINT '';

-- ============================================
-- STEP 4: Insert Test DMR
-- ============================================
PRINT '--- Step 4: Inserting Test DMRs ---';

-- Today's DMR (Pending)
INSERT INTO DMR_Form (DMR_Dept, Status, TotalAmount, CreatedDate)
VALUES (@TestDeptID, 'Pending', 3000.00, GETDATE());

-- Today's DMR (Approved)
INSERT INTO DMR_Form (DMR_Dept, Status, TotalAmount, CreatedDate)
VALUES (@TestDeptID, 'Approved', 4500.00, GETDATE());

PRINT '✓ Inserted 2 test DMRs';
PRINT '  - 2 Today (Pending: 3000, Approved: 4500)';
PRINT '';

-- ============================================
-- STEP 5: Insert Test Bill Payments
-- ============================================
PRINT '--- Step 5: Inserting Test Bill Payments ---';

-- Today's Bill (Pending)
INSERT INTO BillPayments (DepartmentID, Status, Amount, CreatedDate)
VALUES (@TestDeptID, 'Pending', 2000.00, GETDATE());

-- Approved Bill
INSERT INTO BillPayments (DepartmentID, Status, Amount, CreatedDate)
VALUES (@TestDeptID, 'Approved', 3500.00, DATEADD(DAY, -1, GETDATE()));

PRINT '✓ Inserted 2 test bill payments';
PRINT '  - 1 Today (Pending: 2000)';
PRINT '  - 1 Yesterday (Approved: 3500)';
PRINT '';

-- ============================================
-- STEP 6: Execute Stored Procedure
-- ============================================
PRINT '========================================';
PRINT 'Executing SP_DailyBackupReport';
PRINT '========================================';
PRINT '';

EXEC SP_DailyBackupReport;

PRINT '';
PRINT '========================================';
PRINT 'Test Data Creation Complete!';
PRINT '========================================';
PRINT '';
PRINT 'Expected Results in Result Set 2:';
PRINT '  Department: Test IT Department';
PRINT '  - TodayDemand: 2';
PRINT '  - PendingDemand: 2 (today + yesterday)';
PRINT '  - ApprovedDemand: 1';
PRINT '  - RejectedDemand: 1';
PRINT '  - TodayPO: 2';
PRINT '  - PendingPO: 1';
PRINT '  - ApprovedPO: 1';
PRINT '  - POAmount: 22500 (5000 + 10000 + 7500)';
PRINT '  - TodayDMR: 2';
PRINT '  - DMRAmount: 7500 (3000 + 4500)';
PRINT '  - TodayBillPayment: 1';
PRINT '  - BillAmount: 5500 (2000 + 3500)';
PRINT '';

/*==============================================================================
  CLEANUP SCRIPT (Run separately if you want to remove test data)
==============================================================================

-- Delete Test Data
DELETE FROM BillPayments WHERE DepartmentID IN (SELECT DepartmentID FROM Department WHERE DepartmentName = 'Test IT Department');
DELETE FROM DMR_Form WHERE DMR_Dept IN (SELECT DepartmentID FROM Department WHERE DepartmentName = 'Test IT Department');
DELETE FROM PurchaseOrders WHERE DeptID IN (SELECT DepartmentID FROM Department WHERE DepartmentName = 'Test IT Department');
DELETE FROM Demands WHERE Deptid IN (SELECT DepartmentID FROM Department WHERE DepartmentName = 'Test IT Department');
DELETE FROM Department WHERE DepartmentName = 'Test IT Department';

PRINT 'Test data cleaned up successfully!';

==============================================================================*/
