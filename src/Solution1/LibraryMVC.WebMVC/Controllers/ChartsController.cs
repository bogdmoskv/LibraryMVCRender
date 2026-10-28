using LibraryMVC.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryMVC.WebMVC.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChartsController : ControllerBase
{
    private readonly LibraryContext _libraryContext;

    private record CountByYearResponseItem(string Year, int Count);

    private record CountByNameResponseItem(string Name, int Count);

    private record CountByMonthResponseItem(string Month, int Count);

    public ChartsController(LibraryContext libraryContext)
    {
        _libraryContext = libraryContext;
    }

    /// <summary>
    /// Динаміка поповнення фонду за роками видання
    /// </summary>
    [HttpGet("booksByYear")]
    public async Task<JsonResult> GetBooksByYearAsync(CancellationToken cancellationToken)
    {
        var grouped = await _libraryContext.Books
            .GroupBy(book => book.PublicationYear)
            .Select(group => new { Year = group.Key, Count = group.Count() })
            .OrderBy(item => item.Year)
            .ToListAsync(cancellationToken);

        var responseItems = grouped
            .Select(item => new CountByYearResponseItem(item.Year.ToString(), item.Count))
            .ToList();

        return new JsonResult(responseItems);
    }

    /// <summary>
    /// Аналіз складу читачів за факультетами
    /// </summary>
    [HttpGet("readersByFaculty")]
    public async Task<JsonResult> GetReadersByFacultyAsync(CancellationToken cancellationToken)
    {
        var responseItems = await _libraryContext.Readers
            .GroupBy(reader => reader.Faculty)
            .Select(group => new CountByNameResponseItem(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

        responseItems = responseItems
            .OrderByDescending(item => item.Count)
            .ToList();

        return new JsonResult(responseItems);
    }

    /// <summary>
    /// Аналіз складу читачів за кафедрами
    /// </summary>
    [HttpGet("readersByDepartment")]
    public async Task<JsonResult> GetReadersByDepartmentAsync(CancellationToken cancellationToken)
    {
        var responseItems = await _libraryContext.Readers
            .GroupBy(reader => reader.Department)
            .Select(group => new CountByNameResponseItem(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

        responseItems = responseItems
            .OrderByDescending(item => item.Count)
            .ToList();

        return new JsonResult(responseItems);
    }

    /// <summary>
    /// Динаміка видач книг по місяцях
    /// </summary>
    [HttpGet("loansByMonth")]
    public async Task<JsonResult> GetLoansByMonthAsync(CancellationToken cancellationToken)
    {
        var loans = await _libraryContext.Loans
            .Select(loan => loan.LoanDate)
            .ToListAsync(cancellationToken);

        var responseItems = loans
            .GroupBy(date => new DateTime(date.Year, date.Month, 1))
            .OrderBy(group => group.Key)
            .Select(group => new CountByMonthResponseItem(
                group.Key.ToString("yyyy-MM"),
                group.Count()))
            .ToList();

        return new JsonResult(responseItems);
    }
}
