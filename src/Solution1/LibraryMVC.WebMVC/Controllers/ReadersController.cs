using LibraryMVC.Domain.Entities;
using LibraryMVC.Infrastructure;
using LibraryMVC.WebMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryMVC.WebMVC.Controllers;

public class ReadersController : Controller
{
    private readonly LibraryContext _context;

    public ReadersController(LibraryContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Readers.OrderBy(r => r.FullName).ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var reader = await _context.Readers.FirstOrDefaultAsync(r => r.Id == id);
        if (reader == null)
        {
            return NotFound();
        }

        return View(reader);
    }

    public IActionResult Create()
    {
        return View(new ReaderForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReaderForm model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _context.Add(new Reader(model.FullName, model.Faculty, model.Department, model.Position));
        await _context.SaveChangesAsync();
        TempData["Success"] = "Читача успішно створено!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var reader = await _context.Readers.FindAsync(id);
        if (reader == null)
        {
            return NotFound();
        }

        return View(new ReaderForm
        {
            Id = reader.Id,
            FullName = reader.FullName,
            Faculty = reader.Faculty,
            Department = reader.Department,
            Position = reader.Position
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ReaderForm model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var reader = await _context.Readers.FindAsync(id);
        if (reader == null)
        {
            return NotFound();
        }

        reader.Update(model.FullName, model.Faculty, model.Department, model.Position);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Дані читача оновлено!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var reader = await _context.Readers.FirstOrDefaultAsync(r => r.Id == id);
        if (reader == null)
        {
            return NotFound();
        }

        return View(reader);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var reader = await _context.Readers.FindAsync(id);
        if (reader != null)
        {
            var hasLoans = await _context.Loans.AnyAsync(l => l.ReaderId == id);
            if (hasLoans)
            {
                TempData["Error"] = "Неможливо видалити читача з історією видач!";
                return RedirectToAction(nameof(Index));
            }

            _context.Readers.Remove(reader);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Читача успішно видалено!";
        }

        return RedirectToAction(nameof(Index));
    }
}
