namespace Rubik.Application.Cubes;

/// <summary>The result of <see cref="ICubeRepository.TryUpdateAsync"/> (AD-5).</summary>
public enum UpdateOutcome
{
    /// <summary>The stored session matched the expected revision and now holds the new moves.</summary>
    Updated,

    /// <summary>The stored session has another revision than the caller expected; nothing was written.</summary>
    RevisionConflict,

    /// <summary>No session exists with the given id.</summary>
    NotFound,
}
