namespace HRS.API.Contracts.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public ICollection<string>? Errors { get; set; }

    public static ApiResponse<T> OkResponse(T? data, string message = "Success") =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> FailResponse(string message, ICollection<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}
