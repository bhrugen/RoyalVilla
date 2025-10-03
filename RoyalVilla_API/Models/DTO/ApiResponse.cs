using System.Net;

namespace RoyalVilla_API.Models.DTO
{
    public class ApiResponse<TData>
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public TData? Data { get; set; }
        public object? Errors { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static  ApiResponse<TData> Create(bool success, int statusCode, string message, TData? data=default,
            object? errors= null)
        {
            return new ApiResponse<TData>
            {
                Success = success,
                StatusCode = statusCode,
                Message = message,
                Data = data,
                Errors = errors
            };
        }

        public static ApiResponse<TData> Ok(TData data,string message) =>
            Create(true, 200, message, data);

        public static ApiResponse<TData> CreatedAt(TData data, string message) =>
             Create(true, 201, message, data);
        public static ApiResponse<TData> NoContent(string message = "Operation completed successfully") =>
            Create(true, 204, message);

        public static ApiResponse<TData> NotFound(string message="Resource not found") =>
            Create(false, 404, message);

        public static ApiResponse<TData> BadRequest(string message, object? errors = null) =>
            Create(false, 400, message, errors:errors);

        public static ApiResponse<TData> Conflict(string message) =>
            Create(false, 409, message);


        public static ApiResponse<TData> Error(int statusCode, string message, object? errors = null) =>
            Create(false, statusCode, message,errors:errors);




    }
}
