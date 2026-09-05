namespace PropertyInventory.Application.Common.Models;

public class PaginationParameters
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    private int _page = DefaultPage;
    private int _pageSize = DefaultPageSize;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? DefaultPage : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (value < 1)
            {
                _pageSize = DefaultPageSize;
                return;
            }

            _pageSize = Math.Min(value, MaxPageSize);
        }
    }
}
