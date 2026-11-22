namespace CombinationGenerator.Domain.Sessions;

// Serializable state of a generation session.
public sealed record SessionSnapshot(Guid Id, string OwnerId, int Size, long Position, int[]? Current, long Version);
