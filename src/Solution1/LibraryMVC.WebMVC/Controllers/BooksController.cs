using LibraryMVC.Domain.Entities;
using LibraryMVC.Infrastructure;
using LibraryMVC.WebMVC.Models;
using LibraryMVC.WebMVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryMVC.WebMVC.Controllers
{
    public class BooksController : Controller
    {
        private readonly LibraryContext _context;
        private readonly ICoverFileService _covers;
       
        public BooksController(LibraryContext context, ICoverFileService covers)
        {
            _context = context;
            _covers = covers;
        }

        public async Task<IActionResult> Index()
        {
            var books = await _context.Books
                .Include(b => b.Authors)
                .Include(b => b.Features)
                .OrderBy(b => b.Title)
                .ToListAsync();

            return View(books);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Authors)
                .Include(b => b.Features)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        public async Task<IActionResult> Create()
        {
            return View(await BuildEditModelAsync(new BookEditViewModel()));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookEditViewModel model)
        {
            AddCoverErrors(model.CoverFile);
            if (!ModelState.IsValid)
            {
                return View(await BuildEditModelAsync(model));
            }

            var book = new Book(
                model.Title,
                model.Annotation,
                model.PublicationYear,
                model.Publisher,
                model.Isbn);

            if (model.CoverFile is {  Length: > 0 })
            {
                book.SetCoverPath(await _covers.SaveAsync(model.CoverFile));
            }

            var selectedAuthorIds = model.SelectedAuthorIds ?? new List<int>();
            var selectedFeatureIds = model.SelectedFeatureIds ?? new List<int>();

            var authors = await _context.Authors
                .Where(a => selectedAuthorIds.Contains(a.Id))
                .ToListAsync();

            var features = await _context.QualificationFeatures
                .Where(f => selectedFeatureIds.Contains(f.Id))
                .ToListAsync();

            book.SetAuthors(authors);
            book.SetFeatures(features);

            _context.Add(book);
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Authors)
                .Include(b => b.Features)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            var model = new BookEditViewModel
            {
                Id = book.Id,
                Title = book.Title,
                Annotation = book.Annotation,
                PublicationYear = book.PublicationYear,
                Publisher = book.Publisher,
                Isbn = book.Isbn,
                CoverPath = book.CoverPath,
                SelectedAuthorIds = book.Authors.Select(a => a.Id).ToList(),
                SelectedFeatureIds = book.Features.Select(f => f.Id).ToList()
            };

            return View(await BuildEditModelAsync(model));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BookEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            AddCoverErrors(model.CoverFile);

            if (!ModelState.IsValid)
            {
                return View(await BuildEditModelAsync(model));
            }

            var book = await _context.Books
               .Include(b => b.Authors)
               .Include(b => b.Features)
               .FirstOrDefaultAsync(b => b.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            book.Update(
                model.Title,
                model.Annotation,
                model.PublicationYear,
                model.Publisher,
                model.Isbn);

            var selectedAuthorIds = model.SelectedAuthorIds ?? new List<int>();
            var selectedFeatureIds = model.SelectedFeatureIds ?? new List<int>();

            var authors = await _context.Authors
                .Where(a => selectedAuthorIds.Contains(a.Id))
                .ToListAsync();

            var features = await _context.QualificationFeatures
                .Where(f => selectedFeatureIds.Contains(f.Id))
                .ToListAsync();

            book.SetAuthors(authors);
            book.SetFeatures(features);

            if (model.CoverFile is { Length: > 0 })
            {
                var previousPath = book.CoverPath;
                book.SetCoverPath(await _covers.SaveAsync(model.CoverFile));
                _covers.Delete(previousPath);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Authors)
                .Include(b => b.Features)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books
              .Include(b => b.Authors)
              .Include(b => b.Features)
              .FirstOrDefaultAsync(b => b.Id == id);

            if (book != null)
            {
                var hasLoans = await _context.Loans.AnyAsync(l => l.BookId == id);
                if (hasLoans)
                {
                    TempData["Error"] = "Неможливо видалити видання з історією видач!";
                    return RedirectToAction(nameof(Index));
                }

                var coverPath = book.CoverPath;
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
                _covers.Delete(coverPath);
                TempData["Success"] = "Видання успішно видалено!";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<BookEditViewModel> BuildEditModelAsync(BookEditViewModel model)
        {
            model.SelectedAuthorIds ??= new List<int>();
            model.SelectedFeatureIds ??= new List<int>();

            var authors = await _context.Authors.OrderBy(a => a.FullName).ToListAsync();
            var features = await _context.QualificationFeatures.OrderBy(f => f.Name).ToListAsync();

            model.AuthorsOptions = authors
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = a.FullName
                })
                .ToList();

            model.FeaturesOptions = features
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = a.Name
                })
                .ToList();

            return model;
        }

        private void AddCoverErrors(IFormFile? file)
        {
           foreach (var error in _covers.GetValidationErrors(file))
            {
                ModelState.AddModelError(nameof(BookEditViewModel.CoverFile), error);
            }
        }

    }
}
