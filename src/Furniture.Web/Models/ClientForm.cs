using System.ComponentModel.DataAnnotations;

namespace Furniture.Web.Models;

public sealed class ClientForm
{
    [Display(Name = "ФИО")]
    [Required(ErrorMessage = "Укажите ФИО клиента.")]
    [StringLength(200, ErrorMessage = "ФИО должно содержать не более 200 символов.")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Телефон")]
    [Required(ErrorMessage = "Укажите телефон.")]
    [StringLength(30, ErrorMessage = "Телефон должен содержать не более 30 символов.")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Электронная почта")]
    [EmailAddress(ErrorMessage = "Укажите корректный адрес электронной почты.")]
    [StringLength(200, ErrorMessage = "Электронная почта должна содержать не более 200 символов.")]
    public string? Email { get; set; }

    [Display(Name = "Адрес")]
    [Required(ErrorMessage = "Укажите адрес.")]
    [StringLength(500, ErrorMessage = "Адрес должен содержать не более 500 символов.")]
    public string DefaultAddress { get; set; } = string.Empty;
}
