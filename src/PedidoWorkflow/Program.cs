using Microsoft.AspNetCore.DataProtection;
using PedidoWorkflow.Data;
using PedidoWorkflow.Services;

var builder = WebApplication.CreateBuilder(args);
var dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection");

Directory.CreateDirectory(dataProtectionPath);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllersWithViews();
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));
builder.Services.AddSingleton<SqlConnectionFactory>();
builder.Services.AddSingleton<AppStorageMode>();
builder.Services.AddSingleton<InMemoryStore>();
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddScoped<DashboardRepository>();
builder.Services.AddScoped<PedidoRepository>();
builder.Services.AddScoped<WorkflowRepository>();
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<WorkflowService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
