# ІС «Кафедральна бібліотека»

Облік фонду: видання, автори, читачі, ознаки, видачі. Аналітика — Google Charts.

**Стек:** C# / .NET 10, ASP.NET Core MVC, EF Core 10.0.10, PostgreSQL.

```
LibraryMVC.Domain          сутності
LibraryMVC.Infrastructure  БД, seed
LibraryMVC.WebMVC          сайт
```

## Запуск

Потрібні .NET 10 SDK і PostgreSQL (порт 5432).

Рядок підключення: `LibraryMVC.WebMVC/appsettings.json`  
(`TarasLibraryDb`, `postgres` / `postgres`).

```bash
cd src/Solution1/LibraryMVC.WebMVC
dotnet run
```

https://localhost:7134

При старті база створюється, міграції застосовуються, демо-дані підставляються самі.
