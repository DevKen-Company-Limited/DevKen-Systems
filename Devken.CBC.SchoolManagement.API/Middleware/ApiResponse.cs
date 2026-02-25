//namespace Devken.CBC.SchoolManagement.API.Middleware
//{
//    public class ApiResponse<T>
//    {
//        public bool Success { get; set; }
//        public string Message { get; set; } = string.Empty;
//        public T? Data { get; set; }
//        public IDictionary<string, string[]>? Errors { get; set; }
//        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

//        public static ApiResponse<T> SuccessResponse(T data, string message = "")
//        {
//            return new ApiResponse<T>
//            {
//                Success = true,
//                Message = message,
//                Data = data
//            };
//        }

//        public static ApiResponse<T> FailureResponse(
//            string message,
//            IDictionary<string, string[]>? errors = null)
//        {
//            return new ApiResponse<T>
//            {
//                Success = false,
//                Message = message,
//                Errors = errors
//            };
//        }
//    }
//}
