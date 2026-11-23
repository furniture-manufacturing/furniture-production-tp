using System.ComponentModel.DataAnnotations;

namespace Furniture.Web.Models;

public sealed class MeasurementForm
{
    public DateOnly? PlannedDate { get; set; }
    public DateOnly? ActualDate { get; set; }
    [StringLength(2000)]
    public string? RoomDimensions { get; set; }
    [StringLength(2000)]
    public string? PlacementFeatures { get; set; }
    [StringLength(2000)]
    public string? CustomerWishes { get; set; }
    [StringLength(2000)]
    public string? Comment { get; set; }
}

public sealed class ItemForm
{
    [Required, StringLength(200)]
    public string FurnitureType { get; set; } = "";
    [Required, StringLength(200)]
    public string Name { get; set; } = "";
    [Range(1, 1000000)]
    public int Quantity { get; set; }
    [Range(typeof(decimal), "0.01", "1000000", ParseLimitsInInvariantCulture = true)]
    public decimal Width { get; set; }
    [Range(typeof(decimal), "0.01", "1000000", ParseLimitsInInvariantCulture = true)]
    public decimal Height { get; set; }
    [Range(typeof(decimal), "0.01", "1000000", ParseLimitsInInvariantCulture = true)]
    public decimal Depth { get; set; }
    [Required, StringLength(200)]
    public string Material { get; set; } = "";
    [Required, StringLength(200)]
    public string Color { get; set; } = "";
    [StringLength(2000)]
    public string? AdditionalCharacteristics { get; set; }
    [Range(typeof(decimal), "0", "9999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal? AgreedUnitPrice { get; set; }
}
