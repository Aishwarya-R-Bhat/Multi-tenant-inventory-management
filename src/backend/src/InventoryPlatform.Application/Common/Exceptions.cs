namespace InventoryPlatform.Application.Common;

public class ValidationFailedException(IDictionary<string, string[]> errors) : Exception("One or more validation errors occurred.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}

public class UnauthorizedException(string message) : Exception(message);

public class ConflictException(string message) : Exception(message);
