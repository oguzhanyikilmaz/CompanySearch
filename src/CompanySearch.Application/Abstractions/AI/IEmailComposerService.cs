using CompanySearch.Application.Common.Models;

namespace CompanySearch.Application.Abstractions.AI;

public interface IEmailComposerService
{
    Task<GeneratedEmailContent> GenerateAsync(EmailGenerationPrompt prompt, CancellationToken cancellationToken);
}
