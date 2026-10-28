using LibraryMVC.WebMVC.Models;

namespace LibraryMVC.WebMVC.Services;

public interface ICoverFileService
{
    IReadOnlyList<string> GetValidationErrors(IFormFile? file);
    Task<string> SaveAsync(IFormFile file);
    void Delete(string? coverPath);
}

public class CoverFileService : ICoverFileService
{
    private const long MaxCoverBytes = 5 * 1024 * 1024; // 5МБ
    private static readonly string[] AllowedCoverExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf"];
    private readonly IWebHostEnvironment _environment;
    //IWebHostEnvironment потрібен, щоб дізнатися папку сайту на диску. Контролер сам її не знає: локально це один шлях,
    //у Docker - інший. ASP.NET передає цей об'єкт у конструктор, і з нього береться WebRootPath - шлях до wwwroot.

    public CoverFileService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public IReadOnlyList<string> GetValidationErrors(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return [];
        }

        var errors = new List<string>();
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedCoverExtensions.Contains(extension))
        {
            var allowed = string.Join(", ", AllowedCoverExtensions.Select(item => item.Trim('.')));
            errors.Add($"Дозволені файли: {allowed}");
        }

        if (file.Length > MaxCoverBytes)
        {
            var maxMegabytes = MaxCoverBytes / (1024 * 1024);
            errors.Add($"Файл більший за {maxMegabytes} МБ");
        }

        return errors;
    }

    public async Task<string> SaveAsync(IFormFile file)
    {
        var coversDir = CoversDirectory;
        Directory.CreateDirectory(coversDir);

        //Генеруємо унікальне ім'я файлу
        //витягуємо розширення файлу (напр.  .jpg,  .pdf ) і робимо його маленькими літерами
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        //:N - генеруємо ідентифікатор без дефісів
        var fileName = $"{Guid.NewGuid():N}{extension}";

        //Зберігаємо файл на диск
        //-Формуємо повний шлях до майбутнього файлу на диску (об'єдную шлях папки covers та унікальне ім'я файлу)
        //Напр:  C:\....\covers\abc123xyz.jpg
        var fullPath = Path.Combine(coversDir, fileName);

        //Створюємо новий порожній файл на сервері за цим шляхом і відкриваємо для нього поток запису. 
        await using var stream = System.IO.File.Create(fullPath);

        //Фізично записуємо дані - копіюємо вміст завантаженого користувачем файлу у створений на сервері поток 
        // (тобто зберігаємо файл на диск)
        await file.CopyToAsync(stream);

        return $"/covers/{fileName}";
    }

    public void Delete(string? coverPath)
    {
        //Якщо шлях заданий та не починається з  /covers/,   то
        if (string.IsNullOrWhiteSpace(coverPath) || !coverPath.StartsWith("/covers/", StringComparison.Ordinal))
        {
            return;
        }

        var fullPath = Path.Combine(CoversDirectory, Path.GetFileName(coverPath));
        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
    }

    //Визначаємо властивість, яка фоормує повний фізичний шлях на жорсткому диску сервера
    //до папки, де зберігаються обкладинки
    private string CoversDirectory
    {
        get
        {
            var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            return Path.Combine(webRoot, "covers");
        }
    }

}
