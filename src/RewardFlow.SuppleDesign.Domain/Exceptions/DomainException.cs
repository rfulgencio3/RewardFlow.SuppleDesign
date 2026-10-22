namespace RewardFlow.SuppleDesign.Domain.Exceptions;

/// <summary>Assertions: reports a violated reward-domain rule in Ubiquitous Language.</summary>
public sealed class DomainException(string code, string message) : Exception(message)
{
    /// <summary>Intention-Revealing Interfaces: identifies the violated business rule.</summary>
    public string Code { get; } = code;
}
