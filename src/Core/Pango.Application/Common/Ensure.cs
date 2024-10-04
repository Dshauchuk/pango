namespace Pango.Application.Common;

public static class Ensure
{
    public static void HasValue<T>(T value, string paramName, string? errorMessage = null) where T : class
    {
        if((typeof(T) == typeof(string) && string.IsNullOrEmpty(value as string)) || value is null)
        {
            errorMessage ??= $"Value of {paramName} cannot be null or empty";

            throw new ArgumentException(errorMessage);
        }
    }

    public static void AreEqual<T>(T value, T expectedValue, string paramName, string? errorMessage = null) where T : IComparable<T>
    {
        if ((value is null && expectedValue is null) || (value is null && expectedValue is not null) || (value != null && value.CompareTo(expectedValue) != 0))
        {
            errorMessage ??= $"Value of {paramName} is expected to be \"{expectedValue}\"";

            throw new ArgumentException(errorMessage);
        }
    }
}
