using System.Text.Json.Serialization;

namespace IslamicApp.Application.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public T Data { get; set; }
    public ApiMeta Meta { get; set; } = new ApiMeta();

    public ApiResponse(T data)
    {
        Data = data;
    }
}

public class ApiListResponse<T>
{
    public bool Success { get; set; } = true;
    public IEnumerable<T> Data { get; set; }
    public ApiMeta Meta { get; set; }

    public ApiListResponse(IEnumerable<T> data, PaginationMetadata pagination = null)
    {
        Data = data;
        Meta = new ApiMeta { Pagination = pagination };
    }
}

public class ApiMeta
{
    public string Version { get; set; } = "1.0";
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PaginationMetadata Pagination { get; set; }
}

public class PaginationMetadata
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }

    public PaginationMetadata(int page, int pageSize, int total)
    {
        Page = page;
        PageSize = pageSize;
        Total = total;
    }
}
