using Microsoft.AspNetCore.Identity;
using WhatsAppBot.Api.Business.Abstractions;
using WhatsAppBot.Api.Models.Entities;

namespace WhatsAppBot.Api.Business.UseCases;

public sealed class BootstrapAdministrator(
    IAdministratorRepository repository,
    IPasswordHasher<Administrator> passwordHasher) : IAdministratorBootstrapper
{
    public async Task<bool> CreateFirstAdministratorAsync(
        string email, string password, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var administrator = new Administrator(0, normalizedEmail, string.Empty, true);
        var passwordHash = passwordHasher.HashPassword(administrator, password);
        return await repository.CreateFirstAdministratorAsync(
            normalizedEmail, passwordHash, cancellationToken);
    }
}
