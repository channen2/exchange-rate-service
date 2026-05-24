using ExchangeRateService.DTOs;

namespace ExchangeRateService.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public string? Error { get; }
        public T? Value { get; }

        private Result(T value)
        {
            IsSuccess = true;
            Value = value;
        }

        private Result(string error)
        {
            IsSuccess = false;
            Error = error;
        }

        public static Result<T> Success(T value)
        {
            return new(value);
        }

        public static Result<T> Failure(string error)
        {
            return new(error);
        }
    }
}
