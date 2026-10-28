namespace LibraryMVC.Domain.Entities;

/// <summary>
/// Факт видачі книги читачеві (хто взяв, яку книгу, коли). Потрібно для журналу видач і діаграми динаміки
/// (скільки видач по місяцях, аналітики і т.д)
/// </summary>
public class Loan : Entity, IAggregateRoot
{
    public Loan(int bookId, int readerId, DateTime loanDate, DateTime? dueDate = null)
    {
        BookId = bookId;
        ReaderId = readerId;
        LoanDate = loanDate;
        DueDate = dueDate;
    }

    public int BookId { get; private set; }             //Id книги, яку видали
    public Book? Book { get; private set; }             //Сама книга (для EF, підвантажується зі зв'язку)
    public int ReaderId { get; private set; }           //Id читача, якому видали
    public Reader? Reader { get; private set; }         //Сам читач (зв'язок EF)
    public DateTime LoanDate { get; private set; }      //Дата видачі
    public DateTime ? DueDate {  get; private set; }    //Термін повернення (до якого числа)
    public DateTime ? ReturnDate { get; private set; }  //Дата фактичного повернення: якщо null - значить, ще не повернули
    public bool IsActive => ReturnDate == null;         //Не поле в БД, а обчислення. Видача АКТИВНА, якщо ReturnDate == null
                                                        //Тобто IsActive ==true -> КНИГА ще на руках у читача (ReturnDate порожній)
                                                        //А якшо IsActive == false -> КНИГУ ВЖЕ ПОВЕРНУЛИ

    //Метод "повернути книгу". Викликаємо його, коли читач віддав книгу - і записуємо дату повернення в ReturnDate
    //Після цього видача вже не активна (IsActive стає false, бо ReturnDate більше не null)
    public void Return(DateTime returnDate)
    {
        ReturnDate = returnDate;
    }

    public void Update(int bookId, int readerId, DateTime loanDate, DateTime? dueDate, DateTime? returnDate)
    {
        BookId = bookId;
        ReaderId = readerId;
        LoanDate = loanDate;
        DueDate = dueDate;
        ReturnDate = returnDate;
    }
}
