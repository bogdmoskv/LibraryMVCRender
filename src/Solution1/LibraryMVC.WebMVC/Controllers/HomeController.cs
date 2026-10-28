using LibraryMVC.Infrastructure;
using LibraryMVC.WebMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace LibraryMVC.WebMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly LibraryContext _context;
        public HomeController(LibraryContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.BooksCount = await _context.Books.CountAsync();
            ViewBag.AuthorsCount = await _context.Authors.CountAsync();
            ViewBag.ReadersCount = await _context.Readers.CountAsync();
            ViewBag.ActiveLoansCount = await _context.Loans.CountAsync(l => l.ReturnDate == null);

            return View();
        }

        public IActionResult Analysis()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
