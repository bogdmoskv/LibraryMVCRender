using LibraryMVC.Domain.Entities;
using LibraryMVC.Infrastructure;
using LibraryMVC.WebMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryMVC.WebMVC.Controllers;

public class QualificationFeaturesController : Controller
{
    private readonly LibraryContext _context;

    public QualificationFeaturesController(LibraryContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.QualificationFeatures.OrderBy(f => f.Name).ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var feature = await _context.QualificationFeatures
            .Include(f => f.Books)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (feature == null)
        {
            return NotFound();
        }

        return View(feature);
    }

    public IActionResult Create()
    {
        return View(new FeatureForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FeatureForm model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _context.Add(new QualificationFeature(model.Name, model.Description));
        await _context.SaveChangesAsync();
        TempData["Success"] = "Ознаку успішно створено!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var feature = await _context.QualificationFeatures.FindAsync(id);
        if (feature == null)
        {
            return NotFound();
        }

        return View(new FeatureForm
        {
            Id = feature.Id,
            Name = feature.Name,
            Description = feature.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FeatureForm model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var feature = await _context.QualificationFeatures.FindAsync(id);
        if (feature == null)
        {
            return NotFound();
        }

        feature.Update(model.Name, model.Description);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Ознаку успішно відредаговано!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var feature = await _context.QualificationFeatures.FirstOrDefaultAsync(f => f.Id == id);
        if (feature == null)
        {
            return NotFound();
        }

        return View(feature);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var feature = await _context.QualificationFeatures
            .Include(f => f.Books)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (feature != null)
        {
            if (feature.Books.Count > 0)
            {
                TempData["Error"] = "Неможливо видалити ознаку, пов'язану з виданнями!";
                return RedirectToAction(nameof(Index));
            }

            _context.QualificationFeatures.Remove(feature);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Ознаку успішно видалено!";
        }

        return RedirectToAction(nameof(Index));
    }
}
