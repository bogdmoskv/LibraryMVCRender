using LibraryMVC.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace LibraryMVC.Infrastructure
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(LibraryContext context)
        {
            //Перевірка, чи БД уже не порожня
            if (await context.Books.AnyAsync()) //якщо є хоча б одна книга в таблиці, то
            {
                return;   //виходимо, seed не запускаємо
            }

            var author1 = new Author("Іваненко Олена Петрівна", "Кандидат технічних наук, доцент");
            var author2 = new Author("Коваленко Андрій Сергійович");
            var author3 = new Author("Сидоренко Марія Іванівна");

            var feature1 = new QualificationFeature("Підручник", "Навчальне видання для ЗВО");
            var feature2 = new QualificationFeature("Спеціальність 121", "Інженерія програмного забезпечення");
            var feature3 = new QualificationFeature("Гриф МОН", "Рекомендовано МОН України");

            var book1 = new Book(
                "Бази даних",
                "Підручник з проектування реляційних БД для студентів IT-спеціальностей",
                2021,
                "Наука",
                "978-617-123-4567");
            book1.SetAuthors(new[] { author1, author2 });
            book1.SetFeatures(new[] { feature1, feature2 });

            var book2 = new Book(
                "Алгоритми та структури даних",
                "Класичні алгоритми, оцінка складності, практичні приклади",
                2019,
                "Політехніка");
            book2.SetAuthors(new[] { author3 });
            book2.SetFeatures(new[] { feature1 });

            var book3 = new Book(
                "Інженерія програмного забезпечення",
                "Методи проєктування, тестування та супроводу ПЗ",
                2023,
                "Університет");
            book3.SetAuthors(new[] { author1 });
            book3.SetFeatures(new[] { feature2, feature3 });

            var readers = new[]
            {
                new Reader("Мельник Ірина Олександрівна", "ФІОТ", "Інформаційних систем", "студент"),
                new Reader("Бондаренко Олег Васильович", "ФІОТ", "Програмного забезпечення", "аспірант"),
                new Reader("Ткаченко Наталія Сергіївна", "ФПМ", "Прикладної математики", "викладач"),
                new Reader("Шевченко Дмитро Андрійович", "ФІОТ", "Інформаційних систем", "доцент"),
                new Reader("Кравчук Анна Вікторівна", "ФЕЛ", "Електронної інженерії", "студент")
            };

            //EF помічає в трекері змін, що стан об'єктів Added ("треба вставити")
            context.AddRange(author1, author2, author3, feature1, feature2, feature3, book1, book2, book3);
            context.AddRange(readers);

            //Реальна вставка в Postgres (реально виконуватися INSERT)
            await context.SaveChangesAsync();

            //Допоміжна функція, яка бере поточну дату в UTC, зсуває на daysOffset днів і повертає нову дату
            //в UTC
            static DateTime UtcDate(int daysOffset) =>
                DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(daysOffset), DateTimeKind.Utc);

            var loanReturned = new Loan(
                book1.Id,
                readers[0].Id,
                UtcDate(-90), //коли книгу видали (90 днів тому)
                UtcDate(-76)); //до якого дня треба було повернути (через 14 днів після видачі, тобто 76 днів тому)
            loanReturned.Return(UtcDate(-80)); //втановлюємо дату, коли книгу реально повернули. Тут 80 днів тому,
            //тобто раніше за строк DueDate

            context.Loans.AddRange(
                loanReturned,
                new Loan(book2.Id, readers[1].Id, UtcDate(-60), UtcDate(-46)),
                new Loan(book3.Id, readers[3].Id, UtcDate(-30), UtcDate(13)),
                new Loan(book1.Id, readers[2].Id, UtcDate(-5), UtcDate(9)));

            await context.SaveChangesAsync();
        }
    }
}
