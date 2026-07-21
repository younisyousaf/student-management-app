namespace StudentManagementApp.WebApi.DTOs
{
    public class ApiResponse
    {
        public string Message { get; set; } = string.Empty;

        // Field-level validation errors
        // Populated for DataAnnotation failures; null/omitted for single-message domain errors.
        public IDictionary<string, string[]>? Errors { get; set; }
    }

    public class ApiResponse<T> : ApiResponse
    {
        public T? Data { get; set; }
    }
}