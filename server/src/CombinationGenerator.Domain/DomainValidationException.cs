namespace CombinationGenerator.Domain;

// Raised when a request violates a business rule.
public sealed class DomainValidationException(string message) : Exception(message);
