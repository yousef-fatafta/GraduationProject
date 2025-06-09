using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Onlink.Data;

using Onlink.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ Database context
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DataContext")
        ?? throw new InvalidOperationException("Connection string 'DataContext' not found.")));

// ✅ Add MVC services
builder.Services.AddControllersWithViews();

// ✅ Cookie Authentication (manual, no Identity)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "OnlinkAuthCookie";
        options.LoginPath = "/Accounts/Login";
        options.LogoutPath = "/Accounts/Logout";
        options.AccessDeniedPath = "/Accounts/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

// ✅ Authorization services
builder.Services.AddAuthorization();
builder.Services.AddScoped<OpenAiService>();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
    DbSeeder.Seed(db);
}

// ✅ تحقق إذا ملف النموذج موجود، إذا مش موجود دربه

// ✅ Middleware pipeline


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ✅ Authentication/Authorization
app.UseAuthentication();
app.UseAuthorization();

// ✅ Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Accounts}/{action=Login}/{id?}");

app.Run();
