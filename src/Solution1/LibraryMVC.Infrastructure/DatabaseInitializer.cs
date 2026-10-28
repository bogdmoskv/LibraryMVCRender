using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace LibraryMVC.Infrastructure;

/// <summary>
/// Ініціалізатор бази даних при старті додатку.
/// Відповідає за створення БД (якщо її ще немає), застосування міграцій EF Core і початкове заповнення демо-даними
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(LibraryContext context)
    {
        //беремо інструмент, яким можна перевірити і створити базу даних
        var creator = context.Database.GetService < IRelationalDatabaseCreator>();
        if (!await creator.ExistsAsync()) //якщо БД ще немає
        {
            await creator.CreateAsync(); //створи порожню БД
        }

        await context.Database.MigrateAsync(); //застосовує міграції EF до БД: створюємо/оновлюємо таблиці
        //за моделлю (якщо ще не застосовані)
        await DbSeeder.SeedAsync(context); //заповнює БД демо-даними (книги, автори, читачі, видача), якщо база ще порожня
    }
}

