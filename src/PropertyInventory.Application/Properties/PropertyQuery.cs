using PropertyInventory.Application.Common.Models;

namespace PropertyInventory.Application.Properties;

public sealed class PropertyQuery : PaginationParameters
{
    public string? Name { get; set; }

    public string? Address { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }
}
