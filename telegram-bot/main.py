from __future__ import annotations

import json  # відповідь API — текст, з нього збираємо книжки
import os  # токен і адреса сайту лежать у змінних оточення
import sys  # аргументи командного рядка і вивід помилок
import time  # короткий кеш каталогу, щоб не бити API на кожну кнопку
import urllib.error  # сайт не відповів або віддав не 200
import urllib.request  # GET без додаткової бібліотеки
from pathlib import Path  # шлях до папки бота, щоб знайти .env

# Якщо бібліотек немає, не падаємо незрозумілою помилкою, а пишемо, що встановити
try:
    from dotenv import load_dotenv  # читає .env і кладе значення в оточення
    from telegram import (  # шматки повідомлення, які бачить людина в чаті
        InlineKeyboardButton,  # кнопка під повідомленням
        InlineKeyboardMarkup,  # набір таких кнопок
        KeyboardButton,  # звичайна кнопка клавіатури, вміє віддати контакт
        ReplyKeyboardMarkup,  # клавіатура знизу, замість звичайної
        ReplyKeyboardRemove,  # команда прибрати цю клавіатуру
        Update,  # усе, що прийшло від Telegram: повідомлення, натискання, контакт
    )
    from telegram.error import BadRequest, InvalidToken  # токен відхилено або повідомлення не змінилось
    from telegram.ext import (  # обв'язка: «якщо прийшло те-то, виклич таку функцію»
        Application,  # сам бот: токен, обробники, запуск
        CallbackQueryHandler,  # натискання кнопки під повідомленням
        CommandHandler,  # команди виду /start
        ContextTypes,  # тип службового аргумента context
        MessageHandler,  # звичайні повідомлення
        filters,  # умови: текст, контакт, не команда
    )
except ImportError:
    sys.stderr.write("Спочатку виконайте: python -m pip install -r requirements.txt\n")
    raise SystemExit(1)  # виходимо з кодом помилки, бот далі не запускаємо


# Папка, у якій лежить цей файл, поруч має бути .env
BASE_DIR = Path(__file__).resolve().parent
# Забираємо з .env токен і адресу сайту, без цього os.getenv їх не побачить
load_dotenv(BASE_DIR / ".env")

# Скільки видань на одній сторінці кнопок; більше — клавіатура не вміщається на екран
PAGE_SIZE = 5
# Скільки збігів пошуку показуємо кнопками, решта лишається в тексті «знайдено N»
SEARCH_LIMIT = 8
# Каталог не перечитуємо з API частіше, ніж раз на цей інтервал
CACHE_SECONDS = 15
# Анотація в чаті коротша, ніж у базі: інакше Telegram відхилить довге повідомлення
ANNOTATION_LIMIT = 700


class Book:
    """Одне видання з API або з демо-фонду"""

    def __init__(
        self,
        book_id: int,
        title: str,
        annotation: str,
        year: int,
        publisher: str | None,
        isbn: str | None,
        authors: list[str],
        features: list[str],
    ):
        self.id = book_id  # той самий id, що в базі; його ховаємо в кнопку
        self.title = title
        self.annotation = annotation
        self.year = year
        self.publisher = publisher
        self.isbn = isbn
        self.authors = authors
        self.features = features

    def button_text(self) -> str:
        # Напис на кнопці каталогу; довгі назви ріжемо, інакше Telegram їх не прийме
        title = self.title if len(self.title) <= 40 else self.title[:39] + "…"
        return f"{title} ({self.year})"

    def card(self) -> str:
        # Картка видання в чаті: хто написав, рік, ISBN і коротка анотація
        authors = ", ".join(self.authors) if self.authors else "автор не вказаний"
        publisher = self.publisher or "видавництво не вказане"
        lines = [
            self.title,
            authors,
            f"{self.year} · {publisher}",
        ]
        if self.isbn:
            lines.append(f"ISBN: {self.isbn}")
        if self.features:
            lines.append("Ознаки: " + ", ".join(self.features))
        annotation = " ".join(self.annotation.split())
        if len(annotation) > ANNOTATION_LIMIT:
            annotation = annotation[: ANNOTATION_LIMIT - 1] + "…"
        if annotation:
            lines.append("")
            lines.append(annotation)
        return "\n".join(lines)


class Catalog:
    """Список видань і кнопки, якими його показують"""

    def __init__(self, books: list[Book]):
        self.books = books

    @classmethod
    def demo(cls) -> Catalog:
        # Той самий набір, що сідає в базу при першому запуску сайту
        return cls(
            [
                Book(
                    1,
                    "Бази даних",
                    "Підручник з проектування реляційних БД для студентів IT-спеціальностей",
                    2021,
                    "Наука",
                    "978-617-123-4567",
                    ["Іваненко Олена Петрівна", "Коваленко Андрій Сергійович"],
                    ["Підручник", "Спеціальність 121"],
                ),
                Book(
                    2,
                    "Алгоритми та структури даних",
                    "Класичні алгоритми, оцінка складності, практичні приклади",
                    2019,
                    "Політехніка",
                    None,
                    ["Сидоренко Марія Іванівна"],
                    ["Підручник"],
                ),
                Book(
                    3,
                    "Інженерія програмного забезпечення",
                    "Методи проєктування, тестування та супроводу ПЗ",
                    2023,
                    "Університет",
                    None,
                    ["Іваненко Олена Петрівна"],
                    ["Спеціальність 121", "Гриф МОН"],
                ),
            ]
        )

    def get(self, book_id: int) -> Book | None:
        # Шукаємо видання за id з кнопки; якщо такого немає — None
        for book in self.books:
            if book.id == book_id:
                return book
        return None

    def search(self, query: str) -> list[Book]:
        # Шукаємо в назві, авторах і видавництві; регістр не важливий
        needle = query.casefold()
        found: list[Book] = []
        for book in self.books:
            haystack = " ".join([book.title, book.publisher or "", *book.authors]).casefold()
            if needle in haystack:
                found.append(book)
        return found

    @property
    def page_count(self) -> int:
        if not self.books:
            return 1
        return (len(self.books) + PAGE_SIZE - 1) // PAGE_SIZE

    def page_text(self, page: int, note: str | None) -> str:
        # Текст над кнопками каталогу; note — якщо зараз показуємо демо, а не живе API
        page = self._clamp_page(page)
        lines = ["Фонд кафедри, оберіть видання або напишіть частину назви чи автора"]
        if note:
            lines.insert(0, note)
        if not self.books:
            lines.append("\nФонд порожній")
            return "\n".join(lines)
        lines.append(f"\nСторінка {page + 1} з {self.page_count}, усього видань: {len(self.books)}")
        return "\n".join(lines)

    def keyboard(self, page: int) -> InlineKeyboardMarkup:
        # По одній кнопці в ряд; у callback_data кладемо "book:id":
        # Telegram надішле цей рядок назад, коли людина натисне кнопку
        page = self._clamp_page(page)
        start = page * PAGE_SIZE
        rows = [
            [InlineKeyboardButton(book.button_text(), callback_data=f"book:{book.id}")]
            for book in self.books[start : start + PAGE_SIZE]
        ]
        nav: list[InlineKeyboardButton] = []
        if page > 0:
            nav.append(InlineKeyboardButton("« Назад", callback_data=f"page:{page - 1}"))
        if page + 1 < self.page_count:
            nav.append(InlineKeyboardButton("Далі »", callback_data=f"page:{page + 1}"))
        if nav:
            rows.append(nav)
        return InlineKeyboardMarkup(rows)

    def _clamp_page(self, page: int) -> int:
        if page < 0:
            return 0
        last = self.page_count - 1
        return last if page > last else page


class LibraryClient:
    """Читає видання з сайту; запис у базу бот не робить: заявку лише друкує бібліотекарю"""

    def __init__(self, base_url: str):
        self.base_url = base_url.rstrip("/")

    def load(self) -> tuple[Catalog, str | None]:
        # Успіх: живий каталог і note=None; збій: демо-фонд і коротке пояснення в чат
        try:
            payload = self._get_json("/api/books")
            books = parse_books(payload)
        except (urllib.error.URLError, TimeoutError, json.JSONDecodeError, OSError, ValueError) as error:
            sys.stderr.write(f"API недоступне ({self.base_url}): {error}\n")
            return Catalog.demo(), "Сайт зараз недоступний, показую демо-фонд"
        if not books:
            return Catalog.demo(), "API повернуло порожній фонд, показую демо-каталог"
        return Catalog(books), None

    def get_book(self, book_id: int) -> Book | None:
        # Одне видання, якщо його немає в уже завантаженій сторінці каталогу
        try:
            payload = self._get_json(f"/api/books/{book_id}")
        except (urllib.error.URLError, TimeoutError, json.JSONDecodeError, OSError, ValueError):
            return None
        if not isinstance(payload, dict):
            return None
        return book_from_api(payload)

    def _get_json(self, path: str):
        # Accept: application/json — той самий формат, що й у розділі C5
        request = urllib.request.Request(
            self.base_url + path,
            headers={"Accept": "application/json"},
        )
        with urllib.request.urlopen(request, timeout=8) as response:
            raw = response.read().decode("utf-8")
        return json.loads(raw)


def parse_books(payload) -> list[Book]:
    """Розбирає і масив GET /api/books, і сторінку {items: ...} з skip/limit"""
    if isinstance(payload, dict):
        payload = payload.get("items") or payload.get("Items") or []
    if not isinstance(payload, list):
        raise ValueError("Очікувався список видань")
    books: list[Book] = []
    for item in payload:
        if not isinstance(item, dict):
            continue
        book = book_from_api(item)
        if book is not None:
            books.append(book)
    return books


def book_from_api(data: dict) -> Book | None:
    # Сайт віддає camelCase; PascalCase лишаємо, якщо формат відповіді змінять
    raw_id = _pick(data, "id", "Id")
    title = str(_pick(data, "title", "Title") or "").strip()
    if raw_id is None or not title:
        return None
    try:
        book_id = int(raw_id)
        year = int(_pick(data, "publicationYear", "PublicationYear") or 0)
    except (TypeError, ValueError):
        return None
    publisher = _blank_to_none(_pick(data, "publisher", "Publisher"))
    isbn = _blank_to_none(_pick(data, "isbn", "Isbn"))
    annotation = str(_pick(data, "annotation", "Annotation") or "")
    return Book(
        book_id,
        title,
        annotation,
        year,
        publisher,
        isbn,
        _names(_pick(data, "authors", "Authors")),
        _names(_pick(data, "features", "Features")),
    )


def _pick(data: dict, *keys: str):
    for key in keys:
        if key in data and data[key] is not None:
            return data[key]
    return None


def _blank_to_none(value) -> str | None:
    if value is None:
        return None
    text = str(value).strip()
    return text or None


def _names(raw) -> list[str]:
    # Автори і ознаки в API — об'єкти {id, name}; порожній список теж нормальний
    if not isinstance(raw, list):
        return []
    names: list[str] = []
    for item in raw:
        if isinstance(item, dict):
            name = str(_pick(item, "name", "Name") or "").strip()
        elif isinstance(item, str):
            name = item.strip()
        else:
            name = ""
        if name:
            names.append(name)
    return names


class LoanDraft:
    """Незавершена заявка однієї людини; живе в пам'яті, поки бот запущений"""

    def __init__(self, book: Book):
        self.book = book
        self.reader_name: str | None = None  # ПІБ; поки не написали — порожньо

    def ask_name(self) -> str:
        return (
            f"Заявка на видачу: {self.book.title}\n"
            f"{self.book.year}\n\n"
            "Напишіть ПІБ читача; скасувати: /cancel"
        )

    def ask_phone(self) -> str:
        return (
            f"{self.book.title}\n"
            f"Читач: {self.reader_name}\n\n"
            "Напишіть телефон або натисніть кнопку нижче"
        )


class LoanRequest:
    """Уже зібрана заявка: хто в Telegram, яке видання, ПІБ і телефон"""

    def __init__(self, user_id: int, full_name: str, username: str | None, book: Book, reader_name: str, phone: str):
        self.user_id = user_id  # числовий id Telegram, він не змінюється
        self.full_name = full_name  # ім'я з профілю
        self.username = username  # @нік, його може не бути
        self.book = book
        self.reader_name = reader_name
        self.phone = phone

    def for_client(self) -> str:
        # Коротке підтвердження в чат; у базу видачу бот не записує
        return (
            "Заявку прийнято\n"
            f"{self.book.title}\n"
            f"Читач: {self.reader_name}\n"
            f"Телефон: {self.phone}\n\n"
            "Бібліотекар побачить її в консолі бота; новий перегляд — /start"
        )

    def for_admin(self) -> str:
        # Та сама заявка для консолі: хто написав і як його знайти
        login = f"@{self.username}" if self.username else "без username"
        return (
            "Нова заявка на видачу\n"
            f"{self.full_name} ({login}), id {self.user_id}\n"
            f"{self.book.title} (id {self.book.id})\n"
            f"Читач: {self.reader_name}\n"
            f"Телефон: {self.phone}"
        )


class LibraryBot:
    """Зв'язує каталог сайту, незавершені заявки і відповіді на повідомлення"""

    def __init__(self, token: str, client: LibraryClient):
        self.token = token  # ключ бота від BotFather
        self.client = client
        # Словник «id людини -> її чернетка»; після перезапуску бота він порожній
        self.drafts: dict[int, LoanDraft] = {}
        self._cached: tuple[Catalog, str | None] | None = None
        self._cached_at = 0.0

    def run(self) -> None:
        # Збираємо застосунок і кажемо, яку функцію кликати на кожну подію
        # Порядок важливий: команда перевіряється раніше за звичайний текст
        app = Application.builder().token(self.token).post_init(self.on_ready).build()
        app.add_handler(CommandHandler("start", self.start))
        app.add_handler(CommandHandler("books", self.start))
        app.add_handler(CommandHandler("help", self.help))
        app.add_handler(CommandHandler("cancel", self.cancel))
        app.add_handler(CallbackQueryHandler(self.on_page, pattern=r"^page:"))
        app.add_handler(CallbackQueryHandler(self.on_book, pattern=r"^book:"))
        app.add_handler(CallbackQueryHandler(self.on_loan, pattern=r"^loan:"))
        app.add_handler(MessageHandler(filters.CONTACT, self.on_contact))  # кнопка з номером
        # Звичайний текст, але не команди; сюди потрапляють ПІБ, телефон і пошуковий запит
        app.add_handler(MessageHandler(filters.TEXT & ~filters.COMMAND, self.on_text))
        # Слухаємо Telegram, поки вікно не закриють
        # Старі повідомлення, що прийшли поки бот був вимкнений, пропускаємо
        app.run_polling(drop_pending_updates=True)

    def catalog(self, refresh: bool = False) -> tuple[Catalog, str | None]:
        now = time.monotonic()
        if not refresh and self._cached is not None and now - self._cached_at < CACHE_SECONDS:
            return self._cached
        loaded = self.client.load()
        self._cached = loaded
        self._cached_at = now
        return loaded

    def find(self, book_id: int) -> Book | None:
        catalog, _note = self.catalog()
        book = catalog.get(book_id)
        if book is not None:
            return book
        return self.client.get_book(book_id)

    async def on_ready(self, app: Application) -> None:
        # Викликається один раз після успішного підключення
        me = await app.bot.get_me()  # питаємо в Telegram, під яким іменем запущений бот
        print(f"Бот запущений: @{me.username}")
        print(f"Каталог: {self.client.base_url}/api/books")
        print("Вікно не закривати, зупинити: Ctrl+C")

    async def start(self, update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
        # Новий /start скидає минулу недороблену заявку цієї людини
        if not update.message or not self._is_private(update):
            return
        user = update.effective_user
        if user:
            self.drafts.pop(user.id, None)
        catalog, note = self.catalog(refresh=True)
        await update.message.reply_text(
            catalog.page_text(0, note),
            reply_markup=catalog.keyboard(0),
        )

    async def help(self, update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
        if not update.message or not self._is_private(update):
            return
        await update.message.reply_text(
            "Команди:\n"
            "/start — каталог видань\n"
            "/books — те саме\n"
            "/cancel — скасувати заявку\n\n"
            "Можна написати частину назви або прізвище автора"
        )

    async def cancel(self, update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
        if not update.message or not self._is_private(update):
            return
        user = update.effective_user
        if user:
            self.drafts.pop(user.id, None)  # чернетка більше не потрібна
        await update.message.reply_text(
            "Скасував; каталог: /start",
            reply_markup=ReplyKeyboardRemove(),  # прибираємо кнопку «Надіслати номер», якщо вона висіла
        )

    async def on_page(self, update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
        query = update.callback_query
        if query is None or query.data is None:
            return
        if not self._is_private(update):
            await query.answer()
            return
        page_text = query.data.split(":", 1)[1]
        if not page_text.isdigit():
            await query.answer()
            return
        await query.answer()
        catalog, note = self.catalog()
        text = catalog.page_text(int(page_text), note)
        markup = catalog.keyboard(int(page_text))
        await self._show(query, text, markup)

    async def on_book(self, update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
        # Сюди потрапляємо, коли натиснули кнопку видання
        query = update.callback_query
        if query is None or query.data is None:
            return
        if not self._is_private(update):
            await query.answer()
            return
        book_text = query.data.split(":", 1)[1]
        if not book_text.isdigit():
            await query.answer()
            return
        book = self.find(int(book_text))
        if book is None:
            await query.answer("Такого видання немає", show_alert=True)  # вікно з помилкою
            return
        await query.answer()
        markup = InlineKeyboardMarkup(
            [
                [InlineKeyboardButton("Замовити видачу", callback_data=f"loan:{book.id}")],
                [InlineKeyboardButton("До каталогу", callback_data="page:0")],
            ]
        )
        if query.message:
            await query.message.reply_text(book.card(), reply_markup=markup)

    async def on_loan(self, update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
        query = update.callback_query
        if query is None or query.data is None:
            return
        if not self._is_private(update) or update.effective_user is None:
            await query.answer()
            return
        book_text = query.data.split(":", 1)[1]
        if not book_text.isdigit():
            await query.answer()
            return
        book = self.find(int(book_text))
        if book is None:
            await query.answer("Такого видання немає", show_alert=True)
            return
        await query.answer()
        draft = LoanDraft(book)
        self.drafts[update.effective_user.id] = draft
        if query.message:
            await query.message.reply_text(draft.ask_name())

    async def on_text(self, update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
        # Спочатку ПІБ, потім телефон; якщо заявки немає, це пошук по фонду
        message = update.message
        user = update.effective_user
        if message is None or user is None or not message.text or not self._is_private(update):
            return
        draft = self.drafts.get(user.id)
        text = message.text.strip()
        if not text:
            return
        if draft is None:
            await self._search(message, text)
            return
        if draft.reader_name is None:
            await self._read_name(message, user.id, draft, text)
            return
        phone = normalize_phone(text)
        if phone is None:
            await message.reply_text("Не схоже на номер; приклад: +380501234567")
            return
        await self._finish(message, user.id, user.full_name, user.username, draft, phone)

    async def on_contact(self, update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
        # Номер прийшов кнопкою «Надіслати номер», а не текстом
        message = update.message
        user = update.effective_user
        if message is None or user is None or message.contact is None or not self._is_private(update):
            return
        draft = self.drafts.get(user.id)
        if draft is None or draft.reader_name is None:
            await message.reply_text("Спочатку оберіть видання: /start")
            return
        # Контакт можна переслати чужий; беремо лише номер самого відправника
        if message.contact.user_id and message.contact.user_id != user.id:
            await message.reply_text("Потрібен ваш номер")
            return
        phone = normalize_phone(message.contact.phone_number or "")
        if phone is None:
            await message.reply_text("Не вдалося прочитати номер; напишіть його текстом")
            return
        await self._finish(message, user.id, user.full_name, user.username, draft, phone)

    async def _search(self, message, text: str) -> None:
        catalog, note = self.catalog()
        found = catalog.search(text)
        if not found:
            await message.reply_text("Нічого не знайшов; спробуйте інше слово або /start")
            return
        shown = found[:SEARCH_LIMIT]
        extra = ""
        if len(found) > len(shown):
            extra = f", показано перші {len(shown)}"
        prefix = f"{note}\n\n" if note else ""
        rows = [
            [InlineKeyboardButton(book.button_text(), callback_data=f"book:{book.id}")]
            for book in shown
        ]
        rows.append([InlineKeyboardButton("Увесь фонд", callback_data="page:0")])
        await message.reply_text(
            f"{prefix}Знайдено: {len(found)}{extra}",
            reply_markup=InlineKeyboardMarkup(rows),
        )

    async def _read_name(self, message, user_id: int, draft: LoanDraft, text: str) -> None:
        name = normalize_name(text)
        if name is None:
            await message.reply_text("Напишіть ім'я та прізвище, без цифр")
            return
        draft.reader_name = name
        self.drafts[user_id] = draft
        await message.reply_text(
            draft.ask_phone(),
            reply_markup=ReplyKeyboardMarkup(
                # request_contact=True: за натисканням Telegram сам надішле номер з профілю
                [[KeyboardButton("Надіслати номер", request_contact=True)]],
                resize_keyboard=True,  # кнопки вужчі, не на весь екран
                one_time_keyboard=True,  # після натискання клавіатура сама сховається
            ),
        )

    async def _finish(
        self,
        message,
        user_id: int,
        full_name: str,
        username: str | None,
        draft: LoanDraft,
        phone: str,
    ) -> None:
        # Заявка зібрана: друкуємо її в консоль і підтверджуємо в чаті
        if draft.reader_name is None:
            return
        request = LoanRequest(user_id, full_name, username, draft.book, draft.reader_name, phone)
        self.drafts.pop(user_id, None)  # чернетка більше не потрібна
        print(request.for_admin())
        print()
        await message.reply_text(request.for_client(), reply_markup=ReplyKeyboardRemove())

    async def _show(self, query, text: str, markup: InlineKeyboardMarkup) -> None:
        # Сторінку каталогу підміняємо в тому самому повідомленні; якщо Telegram відмовив — пишемо нове
        try:
            await query.edit_message_text(text, reply_markup=markup)
        except BadRequest:
            if query.message:
                await query.message.reply_text(text, reply_markup=markup)

    @staticmethod
    def _is_private(update: Update) -> bool:
        # True лише для особистого чату; у групі бот мовчить
        chat = update.effective_chat
        return bool(chat and chat.type == "private")


def normalize_phone(raw: str) -> str | None:
    """Приводить номер до виду +380... Якщо це не схоже на телефон — повертає None"""
    digits = "".join(char for char in raw if char.isdigit())  # викидаємо пробіли, дужки, плюс
    # Український мобільний без коду країни: 050... це той самий номер, що 38050...
    if len(digits) == 10 and digits.startswith("0"):
        digits = "38" + digits
    # Дев'ять цифр без нуля і без 380 — дописуємо код України
    if len(digits) == 9:
        digits = "380" + digits
    # Міжнародний номер буває від 11 до 15 цифр; коротший або довший — сміття
    if not 11 <= len(digits) <= 15:
        return None
    return "+" + digits


def normalize_name(raw: str) -> str | None:
    """ПІБ: щонайменше два слова і жодної цифри"""
    name = " ".join(raw.split())
    parts = name.split(" ")
    letters = [char for char in name if char.isalpha()]
    if len(parts) < 2 or len(letters) < 5:
        return None
    if any(char.isdigit() for char in name):
        return None
    return name


def token_ok(token: str) -> bool:
    """Груба перевірка токена до запиту в Telegram; справжній токен виглядає як 123456:секрет"""
    # Рівно одна двокрапка і жодних пробілів; інакше це не токен
    if token.count(":") != 1 or any(char.isspace() for char in token):
        return False
    bot_id, secret = token.split(":", 1)
    # Зліва числовий id бота, справа довгий секрет; рядок із самих A — заглушка з прикладу
    return bot_id.isdigit() and len(secret) >= 30 and set(secret) != {"A"}


def run_check() -> None:
    """Перевірка без Telegram: розбір API, пошук, телефон і текст заявки; запуск: python main.py --check"""
    catalog = Catalog.demo()
    book = catalog.get(1)
    assert book is not None
    assert catalog.search("іваненко")[0].id == 1
    assert catalog.search("немає такого") == []
    assert "Бази даних" in book.card()

    parsed = parse_books(
        [
            {
                "id": 7,
                "title": "REST-тест",
                "annotation": "З API",
                "publicationYear": 2024,
                "publisher": "Кафедра",
                "isbn": "978-617-000-0001",
                "authors": [{"id": 1, "name": "Іваненко Олена Петрівна"}],
                "features": [{"id": 1, "name": "Підручник"}],
            }
        ]
    )
    assert parsed[0].authors == ["Іваненко Олена Петрівна"]
    paged = parse_books(
        {
            "skip": 0,
            "limit": 1,
            "count": 1,
            "items": [{"id": 8, "title": "Сторінка", "publicationYear": 2020}],
            "nextLink": None,
        }
    )
    assert paged[0].id == 8
    assert parse_books([]) == []

    request = LoanRequest(1, "Іван", "ivan", book, "Мельник Ірина", "+380501234567")
    assert "Мельник Ірина" in request.for_client()
    assert "id 1" in request.for_admin()
    assert normalize_phone("+380 (50) 123-45-67") == "+380501234567"
    assert normalize_phone("0501234567") == "+380501234567"
    assert normalize_phone("123") is None
    assert normalize_name("Мельник Ірина") == "Мельник Ірина"
    assert normalize_name("Ірина") is None
    assert catalog.keyboard(0).inline_keyboard[0][0].callback_data == "book:1"

    print("Каталог:")
    for item in catalog.books:
        print(f"- {item.id}: {item.button_text()}")
    print()
    print(request.for_client())
    print("CHECK OK")


def main() -> None:
    # --check не ходить у Telegram, лише ганяє run_check і виходить
    if "--check" in sys.argv:
        run_check()
        return
    # Токен з .env; зрізаємо пробіли і лапки, якщо їх випадково залишили навколо значення
    token = os.getenv("BOT_TOKEN", "").strip().strip('"').strip("'")
    if not token_ok(token):
        sys.stderr.write(
            "У файлі .env немає робочого BOT_TOKEN\n"
            "Скопіюйте .env.example у .env і вставте токен від @BotFather\n"
        )
        raise SystemExit(1)
    base_url = os.getenv("LIBRARY_API_URL", "http://localhost:5221").strip().strip('"').strip("'")
    if not base_url:
        base_url = "http://localhost:5221"
    try:
        LibraryBot(token, LibraryClient(base_url)).run()  # далі бот висить і слухає повідомлення
    except InvalidToken:
        sys.stderr.write("Telegram не прийняв токен, перевірте BOT_TOKEN у .env\n")
        raise SystemExit(1)


# Цей блок виконується лише коли файл запустили напряму, а не імпортували з іншого
if __name__ == "__main__":
    main()
