using CompanySearch.Application.Abstractions.AI;
using CompanySearch.Application.Abstractions.Persistence;
using CompanySearch.Application.Businesses.Queries.GetBusinessById;
using CompanySearch.Application.Common.Exceptions;
using CompanySearch.Application.Common.Models;
using CompanySearch.Domain.Entities;
using MediatR;

namespace CompanySearch.Application.Emails.Commands.GenerateEmail;

public sealed record GenerateEmailCommand(Guid BusinessId) : IRequest<SalesEmailDto>;

public sealed class GenerateEmailCommandHandler(
    IBusinessRepository businessRepository,
    IWebsiteAnalysisRepository websiteAnalysisRepository,
    IEmailRepository emailRepository,
    IEmailComposerService emailComposerService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GenerateEmailCommand, SalesEmailDto>
{
    public async Task<SalesEmailDto> Handle(GenerateEmailCommand request, CancellationToken cancellationToken)
    {
        var business = await businessRepository.GetByIdAsync(request.BusinessId, includeDetails: false, cancellationToken)
            ?? throw new NotFoundException($"Business '{request.BusinessId}' was not found.");

        var latestAnalysis = await websiteAnalysisRepository.GetLatestByBusinessIdAsync(business.Id, cancellationToken);

        var prompt = new EmailGenerationPrompt(
            business.Name,
            business.Website,
            latestAnalysis?.Score ?? 0,
            latestAnalysis?.Summary ?? "No automated website audit has been run yet. Use the available business profile to draft a value-driven outbound email.",
            latestAnalysis?.Issues ?? [],
            business.Email);

        var generatedContent = await emailComposerService.GenerateAsync(prompt, cancellationToken);

        var email = SalesEmail.Create(
            business.Id,
            generatedContent.Subject,
            generatedContent.Body,
            business.Email,
            generatedContent.Model);

        await emailRepository.AddAsync(email, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return GetBusinessByIdQueryHandler.MapEmail(email);
    }
}
