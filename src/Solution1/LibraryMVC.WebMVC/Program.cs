using LibraryMVC.Infrastructure;
using LibraryMVC.WebMVC.Services;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://+:{port}");
}

//Налаштування драйвера PostgreSQL (Npgsql) для роботи з даними. За замовчуванням Npgsql хоче DateTime лише в UTC.
//З форм MVC часто приходить звичайна дата без UTC - і запис у БД може впасти з помилкою
//EnableLegacyTimestampBehavior = true вмикає більш м'яку поведінку: дати з MVC-форм можна зберігати без обов'язкового Kind=Utc
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<ICoverFileService,CoverFileService>();

//Реєстрація контексту БД (підключаємо LibraryContext до Postgres за рядком з конфігу; міграції беремо з проєкту WebMVC)
builder.Services
    .AddDbContext<LibraryContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString(nameof(LibraryContext)),
            npgsqlOptions => npgsqlOptions.MigrationsAssembly(
                typeof(Program).GetTypeInfo().Assembly.GetName().Name)));

var app = builder.Build();

//підготовка БД одразу після запуску додатку, до обробки запитів
using (var scope = app.Services.CreateScope()) //створюємо короткий "скоуп" (як один запит)
{
    //отримуємо готовий LibraryContext (підключений до Postgres)
    var context = scope.ServiceProvider.GetRequiredService<LibraryContext>();
    await DatabaseInitializer.InitializeAsync(context); //викликаємо ініціалізацію БД з цим context: створити базу (якщо немає)
    // -> міграції -> seed
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
