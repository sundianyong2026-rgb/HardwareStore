using HardwareStore.Data;
using HardwareStore.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC controllers and Razor views.
builder.Services.AddControllersWithViews();

// Product galleries send up to 8 cropped images as base64 form values, so the
// default limits have to be lifted a bit (default value limit is small).
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = 8 * 1024 * 1024;
    options.MultipartBodyLengthLimit = 64 * 1024 * 1024;
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 64 * 1024 * 1024;
});

// PostgreSQL via Entity Framework Core.
var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<StoreDbContext>(options =>
    options.UseNpgsql(connectionString));

// Session keeps the current customer / merchant / admin identity.
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddScoped<CurrentUserSession>();
builder.Services.AddScoped<FileStorage>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Insert demo data when the database tables are already created.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
    await DbSeeder.SeedAsync(db);
}

app.Run();
