# Application Testing Guide

## Quick Test: Database Service se Data Fetch karo

### Option 1: Browser se Test (Simplest)

1. **Application run karo**:
   ```powershell
   cd "D:\Preeti Project\DailyReportEmail\DailyReportEmail"
   dotnet run
   ```

2. **Browser open karo**: `https://localhost:5001` ya `http://localhost:5000`

3. **Console Output dekho** - Application logs mein check karo:
   - Department summaries load ho rahi hain ya nahi
   - Errors aa rahe hain ya nahi

---

### Option 2: Controller mein Test Endpoint Add karo

**File**: `DailyReportEmail/Controllers/HomeController.cs`

Yeh method add karo existing `HomeController` class mein:

```csharp
[HttpGet]
public async Task<IActionResult> TestDepartmentData()
{
    try
    {
        var report = await _databaseService.GetDailyReportAsync();
        
        // Overall Summary
        var overallSummary = new
        {
            ReportDate = report.ReportDate,
            TodayDemand = report.TodayDemand,
            TodayPO = report.TodayPO,
            TodayDMR = report.TodayDMR,
            TodayBillPayment = report.TodayBillPayment
        };
        
        // Department-wise Summary
        var departmentSummary = report.DepartmentSummaries.Select(d => new
        {
            d.DepartmentName,
            d.TodayDemand,
            d.PendingDemand,
            d.ApprovedDemand,
            d.TodayPO,
            d.POAmount,
            d.TodayDMR,
            d.DMRAmount,
            d.TodayBillPayment,
            d.BillAmount
        }).ToList();
        
        return Json(new
        {
            Success = true,
            OverallSummary = overallSummary,
            DepartmentCount = report.DepartmentSummaries.Count,
            DepartmentSummaries = departmentSummary
        });
    }
    catch (Exception ex)
    {
        return Json(new
        {
            Success = false,
            Error = ex.Message,
            StackTrace = ex.StackTrace
        });
    }
}
```

**Test URL**: `https://localhost:5001/Home/TestDepartmentData`

---

### Option 3: Console Application (Standalone Test)

Naya file banao: **TestDbConnection.cs**

```csharp
using Microsoft.Data.SqlClient;
using System;
using System.Data;

class TestDbConnection
{
    static void Main()
    {
        string connectionString = "Data Source=192.168.60.20;Initial Catalog=CENTRALSTORE_DEMO;User ID=preeti20;Password=20@preeti;Integrated Security=False;TrustServerCertificate=True;";
        
        Console.WriteLine("Testing SP_DailyBackupReport...\n");
        
        try
        {
            using var connection = new SqlConnection(connectionString);
            using var command = new SqlCommand("SP_DailyBackupReport", connection) 
            { 
                CommandType = CommandType.StoredProcedure 
            };
            
            connection.Open();
            Console.WriteLine("✓ Connection successful!\n");
            
            using var reader = command.ExecuteReader();
            
            // Result Set 1: Overall Summary
            Console.WriteLine("=== RESULT SET 1: Overall Summary ===");
            if (reader.Read())
            {
                Console.WriteLine($"Today Demand: {reader["TodayDemand"]}");
                Console.WriteLine($"Today PO: {reader["TodayPO"]}");
                Console.WriteLine($"Today DMR: {reader["TodayDMR"]}");
                Console.WriteLine($"Today Bill Payment: {reader["TodayBillPayment"]}");
            }
            
            // Result Set 2: Department-wise
            Console.WriteLine("\n=== RESULT SET 2: Department-wise Summary ===");
            if (reader.NextResult())
            {
                int count = 0;
                while (reader.Read())
                {
                    count++;
                    Console.WriteLine($"\n{count}. {reader["DepartmentName"]}");
                    Console.WriteLine($"   - Today Demand: {reader["TodayDemand"]}");
                    Console.WriteLine($"   - Pending Demand: {reader["PendingDemand"]}");
                    Console.WriteLine($"   - Today PO: {reader["TodayPO"]}");
                    Console.WriteLine($"   - PO Amount: {reader["POAmount"]:N2}");
                    Console.WriteLine($"   - Today DMR: {reader["TodayDMR"]}");
                    Console.WriteLine($"   - DMR Amount: {reader["DMRAmount"]:N2}");
                }
                
                Console.WriteLine($"\nTotal Departments: {count}");
            }
            else
            {
                Console.WriteLine("⚠ No second result set found!");
                Console.WriteLine("   Make sure SP_DailyBackupReport is updated.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
```

**Run kaise karein:**
```powershell
csc TestDbConnection.cs
.\TestDbConnection.exe
```

---

### Option 4: Existing Index Page par Display karo

**File**: `DailyReportEmail/Views/Home/Index.cshtml`

Model mein department summaries dikhaao (existing page ke end mein add karo):

```html
@if (Model.DepartmentSummaries != null && Model.DepartmentSummaries.Any())
{
    <div class="card mt-4">
        <div class="card-header bg-info text-white">
            <h4>📊 Department-wise Summary</h4>
        </div>
        <div class="card-body">
            <div class="table-responsive">
                <table class="table table-striped table-bordered">
                    <thead class="table-dark">
                        <tr>
                            <th>Department</th>
                            <th>Today Demand</th>
                            <th>Pending Demand</th>
                            <th>Today PO</th>
                            <th>PO Amount</th>
                            <th>Today DMR</th>
                            <th>DMR Amount</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var dept in Model.DepartmentSummaries)
                        {
                            <tr>
                                <td><strong>@dept.DepartmentName</strong></td>
                                <td>@dept.TodayDemand</td>
                                <td>@dept.PendingDemand</td>
                                <td>@dept.TodayPO</td>
                                <td>₹@dept.POAmount.ToString("N2")</td>
                                <td>@dept.TodayDMR</td>
                                <td>₹@dept.DMRAmount.ToString("N2")</td>
                            </tr>
                        }
                    </tbody>
                </table>
            </div>
        </div>
    </div>
}
else
{
    <div class="alert alert-warning mt-4">
        <strong>⚠ No department data found!</strong>
        <p>Make sure SP_DailyBackupReport is updated and returns Result Set 2.</p>
    </div>
}
```

---

## Testing Steps Summary

1. ✅ **Deploy Modified SP**: Run `SP_DailyBackupReport_Modified.sql`
2. ✅ **Insert Test Data**: Run `TEST_DATA_INSERT.sql`
3. ✅ **Verify SQL**: Run `VERIFY_SP_RESULTS.sql`
4. ✅ **Test Application**: 
   - Run `dotnet run`
   - Navigate to home page
   - Check if department table displays
5. ✅ **Check JSON**: Visit `/Home/TestDepartmentData` endpoint

---

## Expected Output

### Console/Logs:
```
Today Demands: 5
Department Summaries Count: 8
- Finance Department: 2 demands, 3 POs
- HR Department: 1 demand, 0 POs
- Test IT Department: 2 demands, 2 POs
...
```

### Browser (JSON):
```json
{
  "Success": true,
  "DepartmentCount": 8,
  "DepartmentSummaries": [
    {
      "DepartmentName": "Test IT Department",
      "TodayDemand": 2,
      "PendingDemand": 2,
      "TodayPO": 2,
      "POAmount": 22500.00
    }
  ]
}
```

---

## Troubleshooting

### Issue: DepartmentSummaries.Count = 0
**Cause**: SP modified nahi hai ya second result set nahi aa raha  
**Fix**: `VERIFY_SP_RESULTS.sql` run karke dekho SP properly execute ho raha hai ya nahi

### Issue: "Column 'DepartmentID' not found"
**Cause**: Column names mismatch ho sakta hai  
**Fix**: Database schema check karo aur SQL script adjust karo

### Issue: Connection timeout
**Cause**: Database server unreachable  
**Fix**: `appsettings.json` mein connection string verify karo

---

**Happy Testing! 🚀**
