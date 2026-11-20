USE [MarshmallowDB];
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_GetMarshmallowDailySummary]
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ReportDate date = CAST(GETDATE() AS date);

    -- Result set 1: cumulative totals through the report date for the summary cards.
    SELECT
        @ReportDate AS ReportDate,
        (SELECT COUNT(*) FROM dbo.shops WHERE LOWER(LTRIM(RTRIM(COALESCE(status, '')))) = 'active') AS ActiveStoreCount,
        (SELECT COUNT(*) FROM dbo.orders WHERE created_at < DATEADD(day, 1, @ReportDate)) AS TotalOrders,
        COALESCE((SELECT SUM(total_amount) FROM dbo.orders WHERE created_at < DATEADD(day, 1, @ReportDate)), 0) AS TotalOrderValue,
        (SELECT COUNT(*) FROM dbo.orders
         WHERE created_at < DATEADD(day, 1, @ReportDate)
           AND LOWER(LTRIM(RTRIM(COALESCE(status, '')))) = 'delivered') AS TotalDelivered;

    -- Result set 2: cumulative totals for every store, including inactive/deleted stores.
    SELECT
        sh.id AS StoreId,
        sh.name AS StoreName,
        COALESCE(sh.status, '') AS StoreStatus,
        COUNT(o.id) AS Orders,
        COALESCE(SUM(o.total_amount), 0) AS OrderValue,
        SUM(CASE WHEN LOWER(LTRIM(RTRIM(COALESCE(o.status, '')))) = 'delivered' THEN 1 ELSE 0 END) AS Delivered
    FROM dbo.shops AS sh
    LEFT JOIN dbo.orders AS o
        ON o.shop_id = sh.id
       AND o.created_at < DATEADD(day, 1, @ReportDate)
    GROUP BY sh.id, sh.name, sh.status
    ORDER BY sh.name;

    -- Result set 3: one row per active store, including stores with no orders today.
    SELECT
        sh.id AS StoreId,
        sh.name AS StoreName,
        COUNT(o.id) AS Orders,
        COALESCE(SUM(o.total_amount), 0) AS OrderValue,
        SUM(CASE WHEN LOWER(LTRIM(RTRIM(COALESCE(o.status, '')))) = 'confirmed' THEN 1 ELSE 0 END) AS Confirmed,
        SUM(CASE WHEN LOWER(LTRIM(RTRIM(COALESCE(o.status, '')))) = 'preparing' THEN 1 ELSE 0 END) AS Preparing,
        SUM(CASE WHEN LOWER(LTRIM(RTRIM(COALESCE(o.status, '')))) = 'delivered' THEN 1 ELSE 0 END) AS Delivered,
        SUM(CASE WHEN LOWER(LTRIM(RTRIM(COALESCE(o.status, '')))) IN ('payment_pending', 'pending') THEN 1 ELSE 0 END) AS Pending,
        SUM(CASE WHEN LOWER(LTRIM(RTRIM(COALESCE(o.status, '')))) = 'cancelled' THEN 1 ELSE 0 END) AS Cancelled,
        SUM(CASE WHEN o.id IS NOT NULL AND LOWER(LTRIM(RTRIM(COALESCE(o.status, '')))) NOT IN
                 ('confirmed', 'preparing', 'delivered', 'payment_pending', 'pending', 'cancelled') THEN 1 ELSE 0 END) AS Other
    FROM dbo.shops AS sh
    LEFT JOIN dbo.orders AS o
        ON o.shop_id = sh.id
       AND o.created_at >= @ReportDate
       AND o.created_at < DATEADD(day, 1, @ReportDate)
    WHERE LOWER(LTRIM(RTRIM(COALESCE(sh.status, '')))) = 'active'
    GROUP BY sh.id, sh.name
    ORDER BY sh.name;

    -- Result set 4: last seven days for the report trend chart.
    ;WITH LastSevenDays AS
    (
        SELECT DATEADD(day, -dayOffsets.DayOffset, @ReportDate) AS ReportDate
        FROM (VALUES (6), (5), (4), (3), (2), (1), (0)) AS dayOffsets(DayOffset)
    ),
    DailyOrders AS
    (
        SELECT CAST(o.created_at AS date) AS ReportDate,
               COUNT(*) AS Orders,
               SUM(CASE WHEN LOWER(LTRIM(RTRIM(COALESCE(o.status, '')))) = 'delivered' THEN 1 ELSE 0 END) AS Delivered,
               SUM(CASE WHEN LOWER(LTRIM(RTRIM(COALESCE(o.status, '')))) IN ('payment_pending', 'pending') THEN 1 ELSE 0 END) AS Pending
        FROM dbo.orders AS o
        INNER JOIN dbo.shops AS sh ON sh.id = o.shop_id
        WHERE o.created_at >= DATEADD(day, -6, @ReportDate)
          AND o.created_at < DATEADD(day, 1, @ReportDate)
          AND LOWER(LTRIM(RTRIM(COALESCE(sh.status, '')))) = 'active'
        GROUP BY CAST(o.created_at AS date)
    )
    SELECT days.ReportDate,
           COALESCE(daily.Orders, 0) AS Orders,
           COALESCE(daily.Delivered, 0) AS Delivered,
           COALESCE(daily.Pending, 0) AS Pending
    FROM LastSevenDays AS days
    LEFT JOIN DailyOrders AS daily ON daily.ReportDate = days.ReportDate
    ORDER BY days.ReportDate;
END;
GO
