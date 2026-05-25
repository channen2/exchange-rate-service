using ExchangeRateService.Common.Errors;

namespace ExchangeRateService.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; }

        public T? Value { get; }

        public ErrorDefinition? Error { get; }

        public Dictionary<string, object> Details { get; }

        private Result(T value)
        {
            IsSuccess = true;
            Value = value;
            Details = new Dictionary<string, object>();
        }

        private Result(ErrorDefinition error, Dictionary<string, object>? details = null)
        {
            IsSuccess = false;
            Error = error;
            Details = details ?? new Dictionary<string, object>();
        }

        public static Result<T> Success(T value)
        {
            return new(value);
        }

        public static Result<T> Failure(
            ErrorDefinition error,
            Dictionary<string, object>? details = null
        )
        {
            return new(error, details);
        }
    }
}
