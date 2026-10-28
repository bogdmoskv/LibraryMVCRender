namespace LibraryMVC.Domain.Entities;

public class Author : Entity, IAggregateRoot
{
    private readonly List<Book> _books = new(); //внутрішній список 

    public Author(string fullName, string? biography = null)
    {
        FullName = fullName;
        Biography = biography;
    }

    public string FullName { get; private set; }
    public string? Biography { get; private set; }
    public IReadOnlyCollection<Book> Books => _books.AsReadOnly();

    //Метод оновлення даних автора. Його викликають, коли бібліотекар
    //натискає "Змінити" і зберігає форму: передають новий
    //ПІБ і біографію - і поля FullName / Biography перезаписуються
    public void Update(string fullName, string ? biography)
    {
        FullName = fullName;
        Biography = biography;
    }
}

