namespace HangScheduler.Api.Middleware
{
    public class GlobalExceptionCatcher
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionCatcher> _logger;

        public GlobalExceptionCatcher(RequestDelegate next, ILogger<GlobalExceptionCatcher> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Pass the request to the next middleware
                await _next(context);
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "An unhandled exception occurred.");

                // Handle the exception
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            // Set the response status code and content type
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            // Return the error details (you can customize this as needed)
            var response = new
            {
                context.Response.StatusCode,
                Message = "Internal Server Error. Please try again later.",
                Detailed = ex.Message // You may choose not to expose the detailed error message in production
            };
            _logger.LogError(ex, ex.Message);
            return context.Response.WriteAsJsonAsync(response);
        }
    }

}
