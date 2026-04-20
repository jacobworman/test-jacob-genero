public static class ErrorFactory
{
    public static ApiException Date(string message)
        => new ApiException("dateproblem", message);

    public static ApiException Currency(string message)
        => new ApiException("currencyproblem", message);
}