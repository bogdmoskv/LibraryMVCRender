namespace LibraryMVC.Domain.Entities;

public class Book : Entity, IAggregateRoot
{
    private readonly List<Author> _authors = new();
    private readonly List<QualificationFeature> _features = new();

    public Book(string title, string annotation, int publicationYear, string? publisher = null, string? isbn = null)
    {
        Title = title;
        Annotation = annotation;
        PublicationYear = publicationYear;
        Publisher = publisher;
        Isbn = isbn;
        AddedAt = DateTime.UtcNow;
    }

    public string Title { get; private set; }
    public string Annotation { get; private set; }
    public int PublicationYear { get; private set; }
    public string ? Publisher { get; private set; }
    public string ? Isbn { get; private set; }

    //Посилання на файл обкладинки (шлях у wwwroot, наприклад /covers/abc.jpg)
    //У базі зберігається не сам файл, а посилання на нього
    public string ? CoverPath { get; private set; }

    public DateTime AddedAt { get; private set; }
    public IReadOnlyCollection<Author> Authors => _authors.AsReadOnly();
    public IReadOnlyCollection<QualificationFeature> Features => _features.AsReadOnly();

    public void SetCoverPath(string? coverPath)
    {
        CoverPath = coverPath;
    }
    
    public void Update(string title, string annotation, int publicationYear, string ? publisher, string ? isbn)
    {
        Title = title;
        Annotation = annotation;
        PublicationYear = publicationYear;
        Publisher = publisher;
        Isbn = isbn;
    }

    //Методу не важливо, що ми передамо - List, або масив, або результат LINQ. 
    //IEnumerable<Author>  = "будь-яка послідованість авторів"
    //Це більш гнучко, ніж писати жорстко List<Author>
    public void SetAuthors(IEnumerable<Author> authors)
    {
        _authors.Clear();
        _authors.AddRange(authors);
    }

    public void SetFeatures(IEnumerable<QualificationFeature> features)
    {
        _features.Clear();
        _features.AddRange(features);
    }
}
