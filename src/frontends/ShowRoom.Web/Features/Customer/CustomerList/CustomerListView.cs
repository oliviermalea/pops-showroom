namespace ShowRoom.Web.Features.Customer.CustomerList;

/// <summary>
/// View model of the customer list screen: the page's items plus the navigation state the component
/// needs, already derived (no computation left in the markup).
/// </summary>
public sealed record CustomerListView(
    IReadOnlyList<CustomerListItemView> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages)
{
    public bool IsEmpty => Items.Count == 0;

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    /// <summary>1-based index of the first item shown, for the "x–y sur z" caption.</summary>
    public int FirstItemIndex => IsEmpty ? 0 : ((Page - 1) * PageSize) + 1;

    /// <summary>1-based index of the last item shown.</summary>
    public int LastItemIndex => IsEmpty ? 0 : FirstItemIndex + Items.Count - 1;
}

/// <summary>One row of the list, display-ready.</summary>
public sealed record CustomerListItemView(
    string PublicId,
    string DisplayName,
    string Email,
    string Status,
    bool IsActive,
    string RegisteredOn);
