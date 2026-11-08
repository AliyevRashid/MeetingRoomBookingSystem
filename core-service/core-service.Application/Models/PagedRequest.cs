namespace core_service.Application.Models;

public record PagedRequest
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 5;
    private const int MaxPageSize = 50;

    private readonly int _pageNumber = DefaultPageNumber;
    private readonly int _pageSize = DefaultPageSize;

    public int PageNumber
    {
        get => _pageNumber;
        init => _pageNumber = value < 1 ? DefaultPageNumber : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? DefaultPageSize : (value > MaxPageSize ? MaxPageSize : value);
    }

    public int Offset => (PageNumber - 1) * PageSize;
}
