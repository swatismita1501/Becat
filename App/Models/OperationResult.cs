namespace EcatDesktop.Models
{
    public sealed class OperationResult
    {
        public bool Success { get; set; }

        public string Title { get; set; }

        public string Message { get; set; }

        public static OperationResult Ok(string title, string message)
        {
            return new OperationResult
            {
                Success = true,
                Title = title,
                Message = message
            };
        }

        public static OperationResult Fail(string title, string message)
        {
            return new OperationResult
            {
                Success = false,
                Title = title,
                Message = message
            };
        }
    }
}
