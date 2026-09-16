using ErrorOr;
using TeamTalk.Application.Common.Models;

namespace TeamTalk.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<ErrorOr<IdentityUserResult>> RegisterAsync(
        string email,
        string displayName,
        string password,
        CancellationToken cancellationToken);

    Task<ErrorOr<IdentityUserResult>> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken);
}
