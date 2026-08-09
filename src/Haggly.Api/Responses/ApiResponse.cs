namespace Haggly.Api.Responses;

public sealed record ApiResponse<T>(
    bool Success,
    string Message,
    T Data)
{
    public static ApiResponse<T> Create(T data, string message)
        => new(true, message, data);
}
