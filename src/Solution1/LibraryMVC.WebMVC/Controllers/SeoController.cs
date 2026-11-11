using System.Text;
using System.Xml.Linq;
using LibraryMVC.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryMVC.WebMVC.Controllers;

public class SeoController : Controller
{
    private readonly LibraryContext _context;

    public SeoController(LibraryContext context)
    {
        _context = context;
    }

    [HttpGet("/robots.txt")]
    public ContentResult Robots()
    {
        var sitemap = $"{Request.Scheme}://{Request.Host}/sitemap.xml";
        var body =
            $"User-agent: *{Environment.NewLine}" +
            $"Allow: /{Environment.NewLine}{Environment.NewLine}" +
            $"Sitemap: {sitemap}{Environment.NewLine}";

        return Content(body, "text/plain", Encoding.UTF8);
    }

    [HttpGet("/sitemap.xml")]
    public async Task<ContentResult> Sitemap()
    {
        var books = await _context.Books
            .OrderBy(b => b.Id)
            .Select(b => new { b.Id, b.AddedAt })
            .ToListAsync();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urlset = new XElement(ns + "urlset",
            books.Select(book =>
                new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/Books/Details/{book.Id}"),
                    new XElement(ns + "lastmod", book.AddedAt.ToString("yyyy-MM-dd")))));

        var document = new XDocument(urlset);
        var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine + document;

        return Content(xml, "application/xml", Encoding.UTF8);
    }
}
