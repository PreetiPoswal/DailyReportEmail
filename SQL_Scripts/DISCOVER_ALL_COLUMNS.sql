USE CENTRALSTORE_DEMO;
GO

-- PurchaseOrders columns
PRINT '=== PurchaseOrders ===';
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'PurchaseOrders' 
ORDER BY ORDINAL_POSITION;

-- DMR_Form columns
PRINT '=== DMR_Form ===';
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'DMR_Form' 
ORDER BY ORDINAL_POSITION;

-- Department columns
PRINT '=== Department ===';
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Department' 
ORDER BY ORDINAL_POSITION;

-- BillTransaction columns (most likely Bill table)
PRINT '=== BillTransaction ===';
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'BillTransaction' 
ORDER BY ORDINAL_POSITION;

-- BillApproval columns
PRINT '=== BillApproval ===';
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'BillApproval' 
ORDER BY ORDINAL_POSITION;

-- Sample data from each table (top 1 row to see actual values)
PRINT '=== Sample Demands row ===';
SELECT TOP 1 * FROM Demands ORDER BY D_ID DESC;

PRINT '=== Sample PurchaseOrders row ===';
SELECT TOP 1 * FROM PurchaseOrders;

PRINT '=== Sample DMR_Form row ===';
SELECT TOP 1 * FROM DMR_Form;

PRINT '=== Sample Department rows ===';
SELECT TOP 5 * FROM Department;
