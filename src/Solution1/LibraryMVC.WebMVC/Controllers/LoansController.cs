using LibraryMVC.Domain.Entities;
using LibraryMVC.Infrastructure;
using LibraryMVC.WebMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryMVC.WebMVC.Controllers;

public class LoansController : Controller
{
    private readonly LibraryContext _context;

    public LoansController(LibraryContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var loans = await _context.Loans
            .Include(l => l.Book)
            .Include(l => l.Reader)
            .OrderByDescending(l => l.LoanDate)
            .ToListAsync();
        return View(loans);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var loan = await _context.Loans
            .Include(l => l.Book)
            .Include(l => l.Reader)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (loan == null)
        {
            return NotFound();
        }

        return View(loan);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateLookupsAsync();
        return View(new LoanForm { LoanDate = DateTime.Today, DueDate = DateTime.Today.AddDays(14) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LoanForm model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(model.BookId, model.ReaderId);
            return View(model);
        }

        _context.Add(new Loan(model.BookId, model.ReaderId, model.LoanDate, model.DueDate));
        await _context.SaveChangesAsync();
        TempData["Success"] = "Видачу успішно оформлено!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var loan = await _context.Loans.FindAsync(id);
        if (loan == null)
        {
            return NotFound();
        }

        await PopulateLookupsAsync(loan.BookId, loan.ReaderId);
        return View(new LoanForm
        {
            Id = loan.Id,
            BookId = loan.BookId,
            ReaderId = loan.ReaderId,
            LoanDate = loan.LoanDate,
            DueDate = loan.DueDate,
            ReturnDate = loan.ReturnDate
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LoanForm model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(model.BookId, model.ReaderId);
            return View(model);
        }

        var loan = await _context.Loans.FindAsync(id);
        if (loan == null)
        {
            return NotFound();
        }

        loan.Update(model.BookId, model.ReaderId, model.LoanDate, model.DueDate, model.ReturnDate);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Видачу успішно відредаговано!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var loan = await _context.Loans
            .Include(l => l.Book)
            .Include(l => l.Reader)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (loan == null)
        {
            return NotFound();
        }

        return View(loan);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var loan = await _context.Loans.FindAsync(id);
        if (loan != null)
        {
            _context.Loans.Remove(loan);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Видачу успішно видалено!";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateLookupsAsync(int? bookId = null, int? readerId = null)
    {
        ViewBag.BookId = new SelectList(
            await _context.Books.OrderBy(b => b.Title).ToListAsync(),
            "Id",
            "Title",
            bookId);
        ViewBag.ReaderId = new SelectList(
            await _context.Readers.OrderBy(r => r.FullName).ToListAsync(),
            "Id",
            "FullName",
            readerId);
    }
}
