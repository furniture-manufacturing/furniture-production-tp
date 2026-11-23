namespace Furniture.Web.Models;

public sealed class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string FurnitureType { get; set; } = "";
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal Depth { get; set; }
    public string Material { get; set; } = "";
    public string Color { get; set; } = "";
    public string? AdditionalCharacteristics { get; set; }
    public decimal? AgreedUnitPrice { get; set; }
    public List<ProductElement> Elements { get; set; } = [];
}

public sealed class ProductElement
{
    public int Id { get; set; }
    public int OrderItemId { get; set; }
    public string Name { get; set; } = "";
    public int RequiredQuantity { get; set; }
    public int ProducedQuantity { get; set; }
    public int DeliveredQuantity { get; set; }
    public int InstalledQuantity { get; set; }
    public string? ProblemComment { get; set; }
}

public sealed class Measurement
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int MeasurerId { get; set; }
    public Employee Measurer { get; set; } = null!;
    public DateOnly? PlannedDate { get; set; }
    public DateOnly? ActualDate { get; set; }
    public string? RoomDimensions { get; set; }
    public string? PlacementFeatures { get; set; }
    public string? CustomerWishes { get; set; }
    public string? Comment { get; set; }
    public MeasurementStatus Status { get; set; }
}

public sealed class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentType Type { get; set; }
    public int AdministratorId { get; set; }
    public Employee Administrator { get; set; } = null!;
    public string? Comment { get; set; }
}
