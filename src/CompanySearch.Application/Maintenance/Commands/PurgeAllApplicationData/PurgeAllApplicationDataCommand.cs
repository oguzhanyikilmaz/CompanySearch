using CompanySearch.Application.Abstractions.Persistence;
using CompanySearch.Application.Common.Exceptions;
using MediatR;

namespace CompanySearch.Application.Maintenance.Commands.PurgeAllApplicationData;

public sealed record PurgeAllApplicationDataCommand(string Confirmation) : IRequest;

public sealed class PurgeAllApplicationDataCommandHandler(IApplicationDataPurge purge)
    : IRequestHandler<PurgeAllApplicationDataCommand>
{
    public const string RequiredConfirmation = "TÜM VERİYİ SİL";

    public Task Handle(PurgeAllApplicationDataCommand request, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.Confirmation?.Trim(), RequiredConfirmation, StringComparison.Ordinal))
        {
            throw new BusinessRuleException($"Onay için tam metin yazın: {RequiredConfirmation}");
        }

        return purge.PurgeAllAsync(cancellationToken);
    }
}
