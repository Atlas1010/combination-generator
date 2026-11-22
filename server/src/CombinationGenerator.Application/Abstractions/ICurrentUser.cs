namespace CombinationGenerator.Application.Abstractions;

// The authenticated caller.
public interface ICurrentUser
{
    string Id { get; }
}
