
namespace LibraryMVC.Domain.Entities;

//Це мітка/характеристика книги з умови "кваліфікаційні ознаки видання"
//Тобто це описує, ЯКА це книга за типом/статусом
//Приклади: Підручник/Навчальний посібник/Монографія і т.д
//Це довідник ярликів для книг, щоб сортувати/фільтрувати книжковий фонд
public class QualificationFeature : Entity, IAggregateRoot
{
    private readonly List<Book> _books = new(); //внутрішній список книг з цією ознакою

    public QualificationFeature(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }

    public string Name { get; private set; }                        //назва ознаки ("Підручник", "Гриф МОН"..)
    public string? Description { get; private set; }                //опис ознаки (може бути порожнім)
    public IReadOnlyCollection<Book> Books => _books.AsReadOnly();  //той самий список, але тільки для читання ззовні

    public void Update(string name, string ? description)
    {
        Name = name;
        Description = description;
    }
}
