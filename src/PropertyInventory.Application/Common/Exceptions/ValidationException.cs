namespace PropertyInventory.Application.Common.Exceptions;

/// <summary>
/// Raised when request data fails application or field-level validation rules.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [string.Empty] = [message]
        };
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>(errors, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
