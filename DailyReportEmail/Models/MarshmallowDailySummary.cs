namespace DailyReportEmail.Models;

public class MarshmallowDailySummary
{
    public DateTime ReportDate { get; set; } = DateTime.Today;
    public int ActiveStoreCount { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalOrderValue { get; set; }
    public int TotalDelivered { get; set; }
    public List<MarshmallowStoreTotalSummary> LifetimeStores { get; set; } = new();
    public List<MarshmallowStoreDailySummary> Stores { get; set; } = new();
    public List<MarshmallowDailyActivity> WeeklyActivity { get; set; } = new();
    public bool WeeklyHistoryAvailable { get; set; }
}

public class MarshmallowStoreTotalSummary
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string StoreStatus { get; set; } = string.Empty;
    public int Orders { get; set; }
    public decimal OrderValue { get; set; }
    public int Delivered { get; set; }
}

public class MarshmallowStoreDailySummary
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int Orders { get; set; }
    public decimal OrderValue { get; set; }
    public int Confirmed { get; set; }
    public int Preparing { get; set; }
    public int Delivered { get; set; }
    public int Pending { get; set; }
    public int Cancelled { get; set; }
    public int Other { get; set; }
}

public class MarshmallowDailyActivity
{
    public DateTime Date { get; set; }
    public int Orders { get; set; }
    public int Delivered { get; set; }
    public int Pending { get; set; }
}
