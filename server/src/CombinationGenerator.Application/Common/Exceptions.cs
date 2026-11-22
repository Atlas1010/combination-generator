namespace CombinationGenerator.Application.Common;

// The session does not exist, has expired, or belongs to another user.
public sealed class SessionNotFoundException(Guid sessionId)
    : Exception($"Session '{sessionId}' was not found or has expired.");

// The session store has reached its configured capacity.
public sealed class SessionCapacityExceededException()
    : Exception("The server is handling the maximum number of sessions. Please try again later.");

// The session kept changing under concurrent requests, even after retrying.
public sealed class SessionConflictException(Guid sessionId)
    : Exception($"Session '{sessionId}' is being updated by another request. Please try again.");
