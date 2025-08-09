namespace marketplace_practice.Services.service_models
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T? Value { get; }
        public IEnumerable<string> Errors { get; }

        private Result(bool isSuccess, T? value, IEnumerable<string> errors)
        {
            IsSuccess = isSuccess;
            Value = value;
            Errors = errors ?? Enumerable.Empty<string>();
        }

        // Успешный результат
        public static Result<T> Success(T value) =>
            new Result<T>(true, value, Enumerable.Empty<string>());

        // Ошибка с одним сообщением
        public static Result<T> Failure(string error) =>
            new Result<T>(false, default, new[] { error });

        // Ошибка с несколькими сообщениями
        public static Result<T> Failure(IEnumerable<string> errors) =>
            new Result<T>(false, default, errors);

        // Оператор неявного преобразования
        public static implicit operator Result<T>(T value) => Success(value);
    }
}
