namespace SoopWorkshop.Backend.Application.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Value { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;

        private Result() { }

        public static Result<T> Ok(T value) => new() { IsSuccess = true, Value = value };
        public static Result<T> Fail(string error) => new() { IsSuccess = false, ErrorMessage = error };
    }
}