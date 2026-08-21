using MyWealth.Application.Common.Security;

namespace MyWealth.Application.IdentityAuth.Logout;

[Authorize]
public record LogoutCommand : IRequest;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    public Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // JWTs are stateless. The client discards the token; the server does not blacklist it.
        return Task.CompletedTask;
    }
}
