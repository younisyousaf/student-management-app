namespace StudentManagementApp.WebApi.DTOs
{
    public class ApiResponse
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ApiResponse<T> : ApiResponse
    {
        public T? Data { get; set; }
    }
}