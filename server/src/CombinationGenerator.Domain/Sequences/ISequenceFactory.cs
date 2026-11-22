namespace CombinationGenerator.Domain.Sequences;

// Creates the sequence a session iterates over.
public interface ISequenceFactory
{
    ICombinatorialSequence Create(int size);
}

public sealed class PermutationSequenceFactory : ISequenceFactory
{
    public ICombinatorialSequence Create(int size) => new PermutationSequence(size);
}
