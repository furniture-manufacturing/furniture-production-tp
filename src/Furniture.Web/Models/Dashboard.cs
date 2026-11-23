namespace Furniture.Web.Models;

public sealed record Dashboard(int ClientCount, IReadOnlyList<Client> RecentClients,
    DashboardGroup MainOrders, DashboardGroup SubOrders, DateOnly From, DateOnly To);

public sealed record DashboardGroup(IReadOnlyDictionary<OrderStatus, int> Statuses, int Overdue, int Waiting,
    decimal Receipts, decimal Refunds, decimal Debt)
{
    public int Count => Statuses.Values.Sum();
}
