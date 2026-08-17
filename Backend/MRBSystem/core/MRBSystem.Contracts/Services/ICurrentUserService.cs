namespace MRBSystem.Contracts.Services;

public interface ICurrentUserService
{
    Guid UserId { get; }
    bool IsInRole(string role);
}
