using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoomGoHanoi.Data;
using RoomGoHanoi.Hubs;
using RoomGoHanoi.Repositories;
using RoomGoHanoi.Services;

var builder = WebApplication.CreateBuilder(args);

// Lấy chuỗi kết nối từ appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Khử lỗi trùng dấu gạch chéo ngược
if (!string.IsNullOrEmpty(connectionString))
{
    connectionString = connectionString.Replace("\\\\", "\\");
}
builder.Services.AddScoped<IListingRepository, ListingRepository>();

var databaseName = "db_RoomGoWNC";
var candidates = new List<string>();

if (!string.IsNullOrEmpty(connectionString))
{
    candidates.Add(connectionString);
}

candidates.AddRange(new[]
{
    $"Server=LAPTOP-3L2V1AFT\\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true",
    $"Server=127.0.0.1,1433;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true",
    $"Server=.;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true",
    $"Server=localhost;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
});

// Chọn chuỗi kết nối khả dụng
var finalConnectionString = candidates.FirstOrDefault(IsServerAvailable) ?? connectionString;

if (string.IsNullOrEmpty(finalConnectionString))
{
    throw new InvalidOperationException("Không thể khởi tạo ConnectionString. Vui lòng kiểm tra appsettings.json.");
}

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddHttpClient<IGeocodingService, GeocodingService>();
builder.Services.AddDbContext<RoomGoDbContext>(o => o.UseSqlServer(finalConnectionString));

//builder.Services.AddScoped<IUserRepository, UserRepository>();
//builder.Services.AddScoped<IRoomRepository, RoomRepository>();
//builder.Services.AddScoped<IReportRepository, ReportRepository>();

// THÊM CẤU HÌNH SESSION
builder.Services.AddDistributedMemoryCache(); // Lưu session trong memory
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian hết hạn session
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.AccessDeniedPath = "/Home/AccessDenied";
        o.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var principal = context.Principal;
                if (principal?.Identity?.IsAuthenticated != true)
                {
                    return;
                }

                var currentInstance = Environment.ProcessId.ToString();
                var instanceClaim = principal.FindFirst("AppInstance");

                if (instanceClaim is null || instanceClaim.Value != currentInstance)
                {
                    context.RejectPrincipal();
                    var httpContext = context.HttpContext;
                    if (httpContext is not null)
                    {
                        httpContext.Session?.Clear();
                        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        context.Response.Redirect("/");
                    }
                }
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// using (var scope = app.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<RoomGoDbContext>();
//     db.Database.EnsureCreated();
//     DbSeeder.Seed(db);
// }

app.UseDeveloperExceptionPage();
app.UseHsts();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// THÊM DÒNG NÀY - Sử dụng Session
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<ChatHub>("/chatHub");
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static bool IsServerAvailable(string candidate)
{
    try
    {
        var cleanConnectionString = candidate.Replace("\\\\", "\\");
        var builder = new SqlConnectionStringBuilder(cleanConnectionString)
        {
            InitialCatalog = "master",
            ConnectTimeout = 2
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