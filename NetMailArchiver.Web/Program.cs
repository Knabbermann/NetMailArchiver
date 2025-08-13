using Microsoft.EntityFrameworkCore;
using NetMailArchiver.Services;
using NetMailArchiver.DataAccess;
using NToastNotify;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql("Host=192.168.178.28;Database=NetMailArchiver;Username=postgres;Password=postgres",
        b => b.MigrationsAssembly("NetMailArchiver.Web"))
    // Add memory optimization settings for Entity Framework
    .EnableServiceProviderCaching(false) // Disable service provider caching to reduce memory usage
    .EnableSensitiveDataLogging(false)); // Disable sensitive data logging in production

// Configure garbage collection for better memory management
if (!builder.Environment.IsDevelopment())
{
    // Use server GC mode for better memory handling in production
    System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.Batch;
}

builder.Services.AddQuartz();

builder.Services.AddQuartzHostedService(q =>
{
    q.WaitForJobsToComplete = true;
});

builder.Services.AddSingleton<QuartzJobSchedulerService>();
builder.Services.AddHostedService<QuartzStartupService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ArchiveLockService>();
builder.Services.AddSingleton<IArchiveProgressService, ArchiveProgressService>();
builder.Services.AddTransient<ArchiveJob>();

builder.Services.AddRazorPages().AddNToastNotifyToastr(new ToastrOptions
{
    ProgressBar = true,
    TimeOut = 5000,
    PositionClass = "toast-bottom-right"
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.UseNToastNotify();
app.Run();
