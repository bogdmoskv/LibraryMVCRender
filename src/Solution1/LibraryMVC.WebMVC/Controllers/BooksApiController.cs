using LibraryMVC.Domain.Entities;
using LibraryMVC.Infrastructure;
using LibraryMVC.WebMVC.Models.Api;
using LibraryMVC.WebMVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryMVC.WebMVC.Controllers;

[ApiController]
[Route("api/books")]
[Produces("application/json", "application/xml")]
public class BooksApiController : ControllerBase
{
    private readonly LibraryContext _context;
    private readonly ICoverFileService _covers;
    public BooksApiController(LibraryContext context, ICoverFileService covers)
    {
        _context = context;
        _covers = covers;
    }

    [HttpGet]
    public async Task<ActionResult<List<BookJsonDto>>> GetAll()
    {
        var books = await _context.Books
            .AsNoTracking()
            .Include(book => book.Authors)
            .Include(book => book.Features)
            .OrderBy(book => book.Title)
            .ToListAsync();

        return Ok(books.Select(ToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookJsonDto>> GetById(int id)
    {
        var book = await LoadBookAsync(id, tracking: false);
        if (book == null)
        {
            return NotFound(new ApiMessageDto { Message = "Видання не знайдено" });
        }

        return Ok(ToDto(book));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BookWriteDto model)
    {
        var book = new Book(
            model.Title,
            model.Annotation,
            model.PublicationYear,
            model.Publisher,
            model.Isbn);

        var linkError = await ApplyLinksAsync(book, model);
        if (linkError != null) {
            return linkError;
        }

        _context.Books.Add(book);
        await _context.SaveChangesAsync();
        return Ok(ToDto(book));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] BookWriteDto model)
    {
        var book = await LoadBookAsync(id, tracking: true);
        if (book == null)
        {
            return NotFound(new ApiMessageDto { Message = "Видання не знайдено" });
        }

        book.Update(
            model.Title,
            model.Annotation,
            model.PublicationYear,
            model.Publisher,
            model.Isbn);

        var linkError = await ApplyLinksAsync(book, model);
        if (linkError != null)
        {
            return linkError;
        }

        await _context.SaveChangesAsync();
        return Ok(ToDto(book));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await _context.Books.FirstOrDefaultAsync(item => item.Id == id);
        if (book == null)
        {
            return NotFound(new ApiMessageDto { Message = "Видання не знайдено!" });
        }

        var hasLoans = await _context.Loans.AnyAsync(loan => loan.BookId == id);
        if (hasLoans)
        {
            return Conflict(new ApiMessageDto
            {
                Message = "Неможливо видалити видання з історією видач"
            });
        }

        var coverPath = book.CoverPath;
        _context.Books.Remove(book);
        await _context.SaveChangesAsync();
        _covers.Delete(coverPath);
        return Ok(new ApiMessageDto { Message = "Видання видалено!" });
    }

    private static BookJsonDto ToDto(Book book)
    {
        return new BookJsonDto
        {
            Id = book.Id,
            Title = book.Title,
            Annotation = book.Annotation,
            PublicationYear = book.PublicationYear,
            Publisher = book.Publisher,
            Isbn = book.Isbn,
            CoverPath = book.CoverPath,
            AddedAt = book.AddedAt,
            Authors = book.Authors
                    .OrderBy(author => author.FullName)
                    .Select(author => new NamedRefDto { Id = author.Id, Name = author.FullName })
                    .ToList(),
            Features = book.Features
                    .OrderBy(feature => feature.Name)
                    .Select(feature => new NamedRefDto { Id = feature.Id, Name = feature.Name })
                    .ToList()

        };
    }

    private async Task<Book?> LoadBookAsync(int id, bool tracking)
    {
        var query = _context.Books
            .Include(book => book.Authors)
            .Include(book => book.Features)
            .AsQueryable();

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(book => book.Id == id);
    }

    //метод прив'язує до видання авторів і кваліфікаційні ознаки з тіла запиту
    private async Task<IActionResult?> ApplyLinksAsync(Book book, BookWriteDto model)
    {
        //[1, 2,2,3]
        var authorIds = (model.AuthorIds ?? []).Distinct().ToList();
        var featureIds = (model.FeatureIds ?? []).Distinct().ToList();

        var authors = authorIds.Count == 0
            ? []
            : await _context.Authors.Where(author => authorIds.Contains(author.Id)).ToListAsync();
        if (authors.Count != authorIds.Count)
        {
            return BadRequest(new ApiMessageDto { Message = "Вказано неіснуючого автора" });
        }

        var features = featureIds.Count == 0
            ? []
            : await _context.QualificationFeatures
                .Where(feature => featureIds.Contains(feature.Id))
                .ToListAsync();

        if (features.Count != featureIds.Count)
        {
            return BadRequest(new ApiMessageDto { Message = "Вказано неіснуючу кваліфікаційну ознаку" });
        }

        book.SetAuthors(authors);
        book.SetFeatures(features);
        return null;
    }
}
