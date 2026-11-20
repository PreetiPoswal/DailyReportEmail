/*==============================================================================
  ToothTech DB pe chalao
  Server: 192.168.60.150 | DB: ToothTechDB_DEMO
  
  Purpose: GET_PAYMENT_SUMMARY flag se PrescribedPerformed column ka data check karo
==============================================================================*/

USE ToothTechDB_DEMO;
GO

-- SP output mein PrescribedPerformed column kya return karta hai?
EXEC sp_GetToothTechDailySummary @Flag = 'GET_PAYMENT_SUMMARY';
GO

-- Agar SP ka source dekhna ho:
-- EXEC sp_helptext 'sp_GetToothTechDailySummary';
