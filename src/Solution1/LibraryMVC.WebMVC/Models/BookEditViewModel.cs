using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace LibraryMVC.WebMVC.Models
{
    public class BookEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Вкажіть назву")]
        [Display(Name = "Назва")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Вкажіть анотацію")]
        [Display(Name = "Анотація")]
        [StringLength(4000)]
        public string Annotation { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Рік видання")]
        [Range(1450, 2100)]
        public int PublicationYear { get; set; } = DateTime.Now.Year;

        [Display(Name ="Видавець")]
        [StringLength(200)]
        public string ? Publisher { get; set; }

        [Display(Name = "ISBN")]
        [StringLength(32)]
        public string ? Isbn { get; set; }

        [Display(Name = "Обкладинка (файл)")]
        public IFormFile ? CoverFile { get; set; }

        //Посилання на вже збережений файл. Саме цей рядок зберігається в таблиці Books
        public string? CoverPath { get; set; }

        [Display(Name = "Автори")]
        public List<int> SelectedAuthorIds { get; set; } = new();

        [Display(Name = "Кваліфікаційні ознаки")]
        public List<int> SelectedFeatureIds { get; set; } = new();

        public List<SelectListItem> AuthorsOptions { get; set; } = new();
        public List<SelectListItem> FeaturesOptions { get; set; } = new();

    }
}
