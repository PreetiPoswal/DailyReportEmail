USE CENTRALSTORE_DEMO;
GO

-- Existing active dept ka ID lete hain (TEST_Dept_Demo ya koi bhi active dept)
DECLARE @DeptID NVARCHAR(50) = (
    SELECT TOP 1 CAST(DepartmentID AS NVARCHAR) 
    FROM Department 
    WHERE Status = 'Active' 
    ORDER BY DepartmentID
);

PRINT 'Using DeptID: ' + @DeptID;

-- Rejected Demand insert karo
INSERT INTO Demands (Demand_usr, Collegeid, Deptid, Dateofgen, DemandStatus)
VALUES ('TestUser', 'DEMO', @DeptID, GETDATE(), 'Rejected');

PRINT 'Rejected Demand inserted';

-- Verify
SELECT DemandStatus, COUNT(*) AS Count
FROM Demands
WHERE Deptid = @DeptID
  AND CAST(Dateofgen AS DATE) = CAST(GETDATE() AS DATE)
GROUP BY DemandStatus;
GO
