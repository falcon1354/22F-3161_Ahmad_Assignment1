namespace MarksApi.Helpers;

public static class PagingHelper
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;

    public static (int page, int pageSize) Normalize(int? page, int? pageSize)
    {
        var p = page is null or < 1 ? DefaultPage : page.Value;
        var size = pageSize is null or < 1 ? DefaultPageSize : pageSize.Value;
        if (size > MaxPageSize)
        {
            size = MaxPageSize;
        }

        return (p, size);
    }
}
