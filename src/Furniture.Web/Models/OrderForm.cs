using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Furniture.Web.Models;

public sealed class OrderForm
{
    [Display(Name = "Клиент")]
    [Required(ErrorMessage = "Выберите клиента.")]
    public int? ClientId { get; set; }

    [Display(Name = "Адрес выполнения")]
    [Required(ErrorMessage = "Укажите адрес выполнения.")]
    [StringLength(500, ErrorMessage = "Адрес должен содержать не более 500 символов.")]
    public string ExecutionAddress { get; set; } = string.Empty;

    [Display(Name = "Пожелания клиента")]
    public string? CustomerWishes { get; set; }

    [Display(Name = "Плановый срок")]
    [Required(ErrorMessage = "Укажите плановый срок.")]
    [DataType(DataType.Date)]
    public DateOnly? PlannedCompletionDate { get; set; }

    public int? ParentOrderId { get; set; }
    [EnumDataType(typeof(SubOrderReason))]
    public SubOrderReason? SubOrderReason { get; set; }

    [BindNever]
    public string? ParentNumber { get; set; }

    [BindNever]
    public IReadOnlyList<ClientChoice> Clients { get; set; } = [];
}

public sealed record ClientChoice(int Id, string FullName, string Phone, string DefaultAddress);
