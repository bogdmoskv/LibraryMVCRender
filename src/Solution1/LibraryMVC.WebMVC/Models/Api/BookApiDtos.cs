using System.ComponentModel.DataAnnotations;

namespace LibraryMVC.WebMVC.Models.Api;

public class BookJsonDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Annotation { get; set; } = string.Empty;
    public int PublicationYear { get; set; }
    public string ? Publisher { get; set; }
    public string ? Isbn { get; set; }
    public string ? CoverPath { get; set; }
    public DateTime AddedAt { get; set; }
    public List<NamedRefDto> Authors { get; set; } = [];
    public List<NamedRefDto> Features { get; set; } = [];
}

public class NamedRefDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class BookWriteDto
{
    [Required(ErrorMessage = "Вкажіть назву")]
    [StringLength(300, MinimumLength = 1, ErrorMessage = "Назва має містити від 1 до 300 символів")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть анотацію")]
    [StringLength(4000, MinimumLength = 1, ErrorMessage = "Анотація має містити від 1 до 4000 символів")]
    public string Annotation { get; set;  } = string.Empty;

    [Range(1450, 2100, ErrorMessage = "Рік видання має бути в межах 1450-2100")]
    public int PublicationYear { get; set; }

    [StringLength(200)]
    public string ? Publisher { get; set; }

    [StringLength(32)]
    public string ? Isbn { get; set; }

    public List<int> AuthorIds { get; set; } = [];
    public List<int> FeatureIds { get; set; } = [];

}

public class ApiMessageDto
{
    public string Message { get; set; } = string.Empty;
}