using LibraryMVC.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryMVC.WebMVC.Controllers;

/// <summary>
/// Пошук для autocomplete: не віддає весь довідник, лише до 20 збігів від 3 символів.
/// </summary>
public class LookupController : Controller
{
    private const int MinLength = 3;
    private const int Limit = 20;
    private readonly LibraryContext _context;

    public LookupController(LibraryContext context) => _context = context;

    [HttpGet]
    public Task<IActionResult> Authors(string? term) =>
        SearchAsync(term, pattern => _context.Authors.AsNoTracking()
            .Where(a => EF.Functions.ILike(a.FullName, pattern, "\\"))
            .OrderBy(a => a.FullName)
            .Select(a => new LookupItem(a.Id, a.FullName)));

    [HttpGet]
    public Task<IActionResult> Features(string? term) =>
        SearchAsync(term, pattern => _context.QualificationFeatures.AsNoTracking()
            .Where(f => EF.Functions.ILike(f.Name, pattern, "\\"))
            .OrderBy(f => f.Name)
            .Select(f => new LookupItem(f.Id, f.Name)));

    [HttpGet]
    public Task<IActionResult> Books(string? term) =>
        SearchAsync(term, pattern => _context.Books.AsNoTracking()
            .Where(b => EF.Functions.ILike(b.Title, pattern, "\\"))
            .OrderBy(b => b.Title)
            .Select(b => new LookupItem(b.Id, b.Title)));

    [HttpGet]
    public Task<IActionResult> Readers(string? term) =>
        SearchAsync(term, pattern => _context.Readers.AsNoTracking()
            .Where(r => EF.Functions.ILike(r.FullName, pattern, "\\"))
            .OrderBy(r => r.FullName)
            .Select(r => new LookupItem(r.Id, r.FullName)));

    private async Task<IActionResult> SearchAsync(string? term, Func<string, IQueryable<LookupItem>> query)
    {
        var text = term?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length < MinLength)
        {
            return Json(Array.Empty<LookupItem>());
        }

        var pattern = "%" + text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
        var items = await query(pattern).Take(Limit).ToListAsync();
        return Json(items);
    }

    private sealed record LookupItem(int id, string text);
}
