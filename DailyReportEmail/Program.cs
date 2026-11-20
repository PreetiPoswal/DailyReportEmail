using DailyReportEmail.Interfaces;
using DailyReportEmail.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Framework Services (MVC/Views)
builder.Services.AddControllersWithViews();

// 2. Register Application Core Services (Scoped Lifetimes)
// Inhe scope ke andar background service properly resolve kar legi
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddScoped<IDashboardDataService, DashboardDataService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPeonBookReportService, PeonBookReportService>();
builder.Services.AddScoped<IMrdReportService, MrdReportService>();
builder.Services.AddScoped<IMarshmallowReportService, MarshmallowReportService>();
builder.Services.AddScoped<IPhrReportService, PhrReportService>();

// 3. Register Hosted Background Service (Singleton Lifetime)
// Yeh background me continuous chalta rahega
builder.Services.AddHostedService<DailyReportBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
