using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoomGoHanoi.Data;
using RoomGoHanoi.Hubs;
using RoomGoHanoi.Services;

var builder = WebApplication.CreateBuilder(args);
var databaseName = "RoomGoHanoi";
var configuredConnection = builder.Configuration.GetConnectionString("RoomGo")!;
var candidates = new[]
{
    configuredConnection,
    $"Server=localhost;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true",
    $"Server=.;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true",
    $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true",
};
var connectionString = candidates.FirstOrDefault(IsServerAvailable) ?? configuredConnection;

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddHttpClient<IGeocodingService, GeocodingService>();
builder.Services.AddDbContext<RoomGoDbContext>(o => o.UseSqlServer(connectionString));
builder
    .Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.AccessDeniedPath = "/Home/AccessDenied";
    });
builder.Services.AddAuthorization();
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RoomGoDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}
app.UseExceptionHandler("/Home/Error");
app.UseHsts();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<ChatHub>("/chatHub");
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

static bool IsServerAvailable(string candidate)
{
    try
    {
        var builder = new SqlConnectionStringBuilder(candidate)
        {
            InitialCatalog = "master",
            ConnectTimeout = 2,
        };
        using var connection = new SqlConnection(builder.ConnectionString);
        connection.Open();
        return true;
    }
    catch
    {
        return false;
    }
}
