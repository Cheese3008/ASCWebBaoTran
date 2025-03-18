using ASC.DataAccess;
using ASC.DataAccess.Interfaces;
using ASC.Web.Configuration;
using ASC.Web.Data;
using ASC.Web.Solution.Services;
using ASC.Web.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Load Configuration
builder.Services.Configure<ApplicationSettings>(builder.Configuration.GetSection("AppSettings"));

// 🔹 Kết nối database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// cấu hình Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 🔹 Thêm session & cache
builder.Services.AddSession();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// 🔹 Đăng ký các dịch vụ ứng dụng
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddTransient<IEmailSender, AuthMessageSender>();
builder.Services.AddTransient<ISmsSender, AuthMessageSender>();
builder.Services.AddSingleton<IIdentitySeed, IdentitySeed>();
builder.Services.AddSingleton<INavigationCacheOperations, NavigationCacheOperations>();

// 🔹 Cấu hình MVC & Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddAuthentication();

var app = builder.Build();

// 🔹 Cấu hình môi trường
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 🔹 Middleware
app.UseSession();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();  // ✅ Đảm bảo Authentication hoạt động
app.UseAuthorization();

// 🔹 Cấu hình route
app.MapControllerRoute(
    name: "areaRoute",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// 🔹 Chạy seed dữ liệu ban đầu (Identity)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var storageSeed = scope.ServiceProvider.GetRequiredService<IIdentitySeed>();
        await storageSeed.Seed(
            scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>(),
            scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(),
            scope.ServiceProvider.GetRequiredService<IOptions<ApplicationSettings>>()
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Lỗi Seed Identity: {ex.Message}");
    }
}

// 🔹 Tạo cache menu
using (var scope = app.Services.CreateScope())
{
    try
    {
        var navigationCacheOperations = scope.ServiceProvider.GetRequiredService<INavigationCacheOperations>();
        await navigationCacheOperations.CreateNavigationCacheAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Lỗi tạo cache menu: {ex.Message}");
    }
}

// 🔹 Chạy ứng dụng
app.Run();