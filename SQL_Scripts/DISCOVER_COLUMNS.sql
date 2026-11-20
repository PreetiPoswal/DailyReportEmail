/*==============================================================================
  Yeh script chalao - sab tables ke actual column names dikhayega
  Server: 192.168.60.20 | DB: CENTRALSTORE_DEMO
==============================================================================*/

USE CENTRALSTORE_DEMO;
GO

-- Demands table columns
PRINT '=== Demands Table ===';
SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Demands' ORDER BY ORDINAL_POSITION;

-- PurchaseOrders table columns
PRINT '=== PurchaseOrders Table ===';
SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PurchaseOrders' ORDER BY ORDINAL_POSITION;

-- DMR_Form table columns
PRINT '=== DMR_Form Table ===';
SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DMR_Form' ORDER BY ORDINAL_POSITION;

-- BillPayments - check if different name
PRINT '=== Tables containing Bill/Payment ===';
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME LIKE '%Bill%' OR TABLE_NAME LIKE '%Payment%' OR TABLE_NAME LIKE '%Pay%'
ORDER BY TABLE_NAME;

-- Department table columns
PRINT '=== Department Table ===';
SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Department' ORDER BY ORDINAL_POSITION;
