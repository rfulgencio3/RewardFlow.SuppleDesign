namespace RewardFlow.SuppleDesign.Tests;

/// <summary>Serializes tests that share the local PostgreSQL database and its migration history.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgresDatabaseCollection
{
    /// <summary>Name used by PostgreSQL integration-test classes.</summary>
    public const string Name = "PostgreSQL database";
}
