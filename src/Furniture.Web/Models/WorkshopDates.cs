namespace Furniture.Web.Models;

public static class WorkshopDates
{
    private static readonly TimeZoneInfo WorkshopZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
    public static DateTime Local(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, WorkshopZone);
    public static DateOnly Today => DateOnly.FromDateTime(Local(DateTime.UtcNow));
    public static DateTime Start(DateOnly day) => TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), WorkshopZone);
}
