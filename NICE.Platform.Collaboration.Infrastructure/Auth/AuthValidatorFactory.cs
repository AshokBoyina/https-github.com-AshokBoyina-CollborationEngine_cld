namespace NICE.Platform.Collaboration.Infrastructure.Auth;

using Microsoft.Extensions.DependencyInjection;
using NICE.Platform.Collaboration.Application.Interfaces.Auth;
using NICE.Platform.Collaboration.Core.Enums;
using NICE.Platform.Collaboration.Infrastructure.Auth.Validators;

/// <summary>
/// Resolves the correct <see cref="IAuthValidator"/> from the DI container
/// based on the <c>X-Access-Key</c> header value (<see cref="AuthProvider"/>).
/// </summary>
public sealed class AuthValidatorFactory(IServiceProvider serviceProvider) : IAuthValidatorFactory
{
    public IAuthValidator GetValidator(AuthProvider provider) =>
        provider switch
        {
            AuthProvider.READI     => serviceProvider.GetRequiredService<ReadiAuthValidator>(),
            AuthProvider.NICE      => serviceProvider.GetRequiredService<NiceAuthValidator>(),
            AuthProvider.ANON      => serviceProvider.GetRequiredService<AnonymousAuthValidator>(),
            AuthProvider.LOCAL_JWT => serviceProvider.GetRequiredService<LocalJwtAuthValidator>(),
            _ => throw new ArgumentOutOfRangeException(
                nameof(provider),
                $"No validator is registered for auth provider '{provider}'.")
        };
}
