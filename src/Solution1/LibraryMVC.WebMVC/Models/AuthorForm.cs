using System.ComponentModel.DataAnnotations;

namespace LibraryMVC.WebMVC.Models
{
    public class AuthorForm
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Вкажіть П.І.Б. автора")]
        [Display(Name = "П.І.Б.")]
        [StringLength(200, ErrorMessage = "П.І.Б. не може перевищувати 200 символів")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Біографія")]
        [StringLength(200, ErrorMessage = "Біографія не може перевищувати 200 символів")]
        public string ? Biography {  get; set; }
    }
}
