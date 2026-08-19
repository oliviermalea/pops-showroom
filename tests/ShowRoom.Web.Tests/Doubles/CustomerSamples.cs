using ShowRoom.Web.Features.Customer.CustomerDetail;
using ShowRoom.Web.Features.Customer.CustomerList;
using ShowRoom.Web.Features.Customer.CustomerOrders;

namespace ShowRoom.Web.Tests.Doubles;

/// <summary>View models used by the component tests, already display-ready (as the mappers produce them).</summary>
public static class CustomerSamples
{
    public const string PublicId = "cus_0123456789abcdef0123456789abcdef";

    public static CustomerDetailView Detail(string phone = "+33123456789", bool active = true) => new(
        PublicId: PublicId,
        DisplayName: "Ada Lovelace",
        FirstName: "Ada",
        LastName: "Lovelace",
        Email: "ada@showroom.test",
        Phone: phone,
        Status: active ? "Active" : "Inactive",
        IsActive: active,
        RegisteredOn: "15 janvier 2026");

    public static CustomerListItemView ListItem(string suffix = "1", bool active = true) => new(
        PublicId: $"cus_{suffix.PadLeft(32, '0')}",
        DisplayName: $"Ada Lovelace {suffix}",
        Email: $"ada{suffix}@showroom.test",
        Status: active ? "Active" : "Inactive",
        IsActive: active,
        RegisteredOn: "15 janvier 2026");

    public static CustomerListView ListPage(
        int page = 1,
        int pageSize = 20,
        int totalItems = 2,
        int totalPages = 1,
        IReadOnlyList<CustomerListItemView>? items = null)
        => new(items ?? [ListItem("1"), ListItem("2", active: false)], page, pageSize, totalItems, totalPages);

    public static CustomerOrderView Order(string publicId = "ord_1") => new(
        PublicId: publicId,
        Status: "Confirmed",
        OrderDate: "01 février 2026",
        TotalAmount: "608,80 EUR",
        ItemCount: "3 articles",
        Lines:
        [
            new CustomerOrderLineView("prd_1", "Clavier mécanique", 2, "129,90 EUR", "259,80 EUR"),
            new CustomerOrderLineView("prd_2", "Écran 27 pouces", 1, "349,00 EUR", "349,00 EUR"),
        ]);

    public static CustomerOrdersView Orders(bool ordersAvailable = true, IReadOnlyList<CustomerOrderView>? orders = null)
        => new(
            PublicId: PublicId,
            DisplayName: "Ada Lovelace",
            Email: "ada@showroom.test",
            OrdersAvailable: ordersAvailable,
            Orders: orders ?? (ordersAvailable ? [Order()] : []));
}
