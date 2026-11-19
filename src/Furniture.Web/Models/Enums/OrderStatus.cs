namespace Furniture.Web.Models.Enums;

public enum OrderStatus
{
    New,
    WaitingForMeasurement,
    MeasurementScheduled,
    InProduction,
    Completed,
    Cancelled
}