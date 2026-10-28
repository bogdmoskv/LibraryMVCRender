using LibraryMVC.Domain.Entities;
using LibraryMVC.Infrastructure;
using LibraryMVC.WebMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryMVC.WebMVC.Controllers
{
    public class AuthorsController : Controller
    {
        private readonly LibraryContext _context;
        public AuthorsController(LibraryContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            return View(await _context.Authors.OrderBy(a => a.FullName).ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var author = await _context.Authors
                    .Include(a => a.Books)
                    .FirstOrDefaultAsync(a => a.Id == id);

            if (author == null)
            {
                return NotFound();
            }

            return View(author);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,Biography")] AuthorForm model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var author = new Author(model.FullName, model.Biography);
            _context.Add(author);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Автора успішно створено!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var author = await _context.Authors.FindAsync(id);
            if (author == null)
            {
                return NotFound();
            }

            return View(new AuthorForm
            {
                Id = author.Id,
                FullName = author.FullName,
                Biography = author.Biography
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AuthorForm model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var author = await _context.Authors.FindAsync(id);
            if (author == null)
            {
                return NotFound();
            }

            author.Update(model.FullName, model.Biography);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Дані автора оновлено!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == id);
            if (author == null)
            {
                return NotFound();
            }

            return View(author);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var author = await _context.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (author != null)
            {
                if (author.Books.Count > 0)
                {
                    TempData["Error"] = "Неможливо видалити автора, який пов'язаний із виданнями";
                    return RedirectToAction(nameof(Index));
                }

                _context.Authors.Remove(author);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Автора видалено";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
