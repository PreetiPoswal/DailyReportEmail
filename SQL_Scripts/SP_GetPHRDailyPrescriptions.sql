USE [PatientPortalDb];
GO

-- Read-only prescription totals and today's department summary for the PHR report.
-- No patient identifiers or clinical text are returned by this procedure.
CREATE OR ALTER PROCEDURE [dbo].[sp_GetPHRDailyPrescriptions]
    @ReportDate date = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @ReportDate = COALESCE(@ReportDate, CONVERT(date, GETDATE()));

    SELECT
        @ReportDate AS ReportDate,
        COUNT_BIG(*) AS TotalPrescriptions,
        COALESCE(SUM(CASE
            WHEN [VisitDate] >= @ReportDate AND [VisitDate] < DATEADD(day, 1, @ReportDate)
            THEN CONVERT(bigint, 1) ELSE CONVERT(bigint, 0)
        END), 0) AS TodayPrescriptions
    FROM [dbo].[DoctorPrescription]
    ;

    SELECT COALESCE(NULLIF(LTRIM(RTRIM([DepartmentName])), ''), 'Unspecified') AS DepartmentName,
           COUNT_BIG(*) AS PrescriptionCount
    FROM [dbo].[DoctorPrescription]
    WHERE [VisitDate] >= @ReportDate
      AND [VisitDate] < DATEADD(day, 1, @ReportDate)
    GROUP BY COALESCE(NULLIF(LTRIM(RTRIM([DepartmentName])), ''), 'Unspecified')
    ORDER BY PrescriptionCount DESC, DepartmentName;

    -- Result set 3: month-to-date department split for the pie chart.
    SELECT COALESCE(NULLIF(LTRIM(RTRIM([DepartmentName])), ''), 'Unspecified') AS DepartmentName,
           COUNT_BIG(*) AS PrescriptionCount
    FROM [dbo].[DoctorPrescription]
    WHERE [VisitDate] >= DATEFROMPARTS(YEAR(@ReportDate), MONTH(@ReportDate), 1)
      AND [VisitDate] < DATEADD(day, 1, @ReportDate)
    GROUP BY COALESCE(NULLIF(LTRIM(RTRIM([DepartmentName])), ''), 'Unspecified')
    ORDER BY PrescriptionCount DESC, DepartmentName;

    -- Result set 4: seven daily totals, including zero-activity dates, for the trend chart.
    ;WITH DateRange AS
    (
        SELECT DATEADD(day, -dayOffsets.DayOffset, @ReportDate) AS ActivityDate
        FROM (VALUES (6), (5), (4), (3), (2), (1), (0)) AS dayOffsets(DayOffset)
    ),
    DailyPrescriptions AS
    (
        SELECT CONVERT(date, [VisitDate]) AS ActivityDate, COUNT_BIG(*) AS PrescriptionCount
        FROM [dbo].[DoctorPrescription]
        WHERE [VisitDate] >= DATEADD(day, -6, @ReportDate)
          AND [VisitDate] < DATEADD(day, 1, @ReportDate)
        GROUP BY CONVERT(date, [VisitDate])
    )
    SELECT dates.ActivityDate,
           COALESCE(daily.PrescriptionCount, 0) AS PrescriptionCount
    FROM DateRange AS dates
    LEFT JOIN DailyPrescriptions AS daily ON daily.ActivityDate = dates.ActivityDate
    ORDER BY dates.ActivityDate;

END;
GO
