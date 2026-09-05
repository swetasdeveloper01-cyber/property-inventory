using Microsoft.EntityFrameworkCore;
using PropertyInventory.Application.Common.Exceptions;
using PropertyInventory.Application.Common.Interfaces;
using PropertyInventory.Application.Common.Validation;
using PropertyInventory.Domain.Entities;

namespace PropertyInventory.Application.Ownerships;

public class OwnershipService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IExchangeRateService _exchangeRateService;

    public OwnershipService(IApplicationDbContext dbContext, IExchangeRateService exchangeRateService)
    {
        _dbContext = dbContext;
        _exchangeRateService = exchangeRateService;
    }

    public async Task<IReadOnlyList<OwnershipDto>> GetByPropertyIdAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        await EnsurePropertyExistsAsync(propertyId, cancellationToken);

        return await _dbContext.PropertyOwnerships
            .AsNoTracking()
            .Where(ownership => ownership.PropertyId == propertyId)
            .OrderBy(ownership => ownership.EffectiveFrom)
            .ThenBy(ownership => ownership.Id)
            .Select(ownership => new OwnershipDto
            {
                Id = ownership.Id,
                PropertyId = ownership.PropertyId,
                ContactId = ownership.ContactId,
                OwnerFirstName = ownership.Contact.FirstName,
                OwnerLastName = ownership.Contact.LastName,
                OwnerEmail = ownership.Contact.Email,
                EffectiveFrom = ownership.EffectiveFrom,
                EffectiveTill = ownership.EffectiveTill,
                AcquisitionPrice = ownership.AcquisitionPrice,
                AcquisitionCurrency = ownership.AcquisitionCurrency,
                AcquisitionPriceUsd = ownership.AcquisitionPriceUsd
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<OwnershipDto> CreateAsync(
        Guid propertyId,
        CreateOwnershipRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        await EnsurePropertyExistsAsync(propertyId, cancellationToken);
        await EnsureContactExistsAsync(request.ContactId, cancellationToken);

        var currency = InputRules.NormalizeCurrency(request.AcquisitionCurrency);
        var acquisitionPriceUsd = _exchangeRateService.ConvertToUsd(request.AcquisitionPrice, currency);

        var existing = await _dbContext.PropertyOwnerships
            .Where(ownership => ownership.PropertyId == propertyId)
            .ToListAsync(cancellationToken);

        var becomesCurrent = request.EffectiveTill is null;
        PropertyOwnership? currentOwner = existing.SingleOrDefault(ownership => ownership.EffectiveTill is null);

        if (becomesCurrent && currentOwner is not null)
        {
            if (request.EffectiveFrom < currentOwner.EffectiveFrom)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["EffectiveFrom"] =
                    [
                        "A new current ownership cannot start before the existing current ownership began."
                    ]
                });
            }
        }

        // Project the post-transfer timeline before validating overlap (closing current is half-open at EffectiveFrom).
        var projectedPeriods = existing
            .Select(ownership =>
            {
                if (becomesCurrent && currentOwner is not null && ownership.Id == currentOwner.Id)
                {
                    return (ownership.EffectiveFrom, (DateOnly?)request.EffectiveFrom);
                }

                return (ownership.EffectiveFrom, ownership.EffectiveTill);
            })
            .ToList();

        EnsureNoOverlap(projectedPeriods, request.EffectiveFrom, request.EffectiveTill);

        if (becomesCurrent && currentOwner is not null)
        {
            // Half-open handoff: previous Till == new From is contiguous, not overlapping.
            currentOwner.EffectiveTill = request.EffectiveFrom;
        }

        var ownership = new PropertyOwnership
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            ContactId = request.ContactId,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTill = request.EffectiveTill,
            AcquisitionPrice = request.AcquisitionPrice,
            AcquisitionCurrency = currency,
            AcquisitionPriceUsd = acquisitionPriceUsd
        };

        await using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);

        _dbContext.PropertyOwnerships.Add(ownership);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        var contact = await _dbContext.Contacts
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == request.ContactId, cancellationToken);

        return new OwnershipDto
        {
            Id = ownership.Id,
            PropertyId = ownership.PropertyId,
            ContactId = ownership.ContactId,
            OwnerFirstName = contact.FirstName,
            OwnerLastName = contact.LastName,
            OwnerEmail = contact.Email,
            EffectiveFrom = ownership.EffectiveFrom,
            EffectiveTill = ownership.EffectiveTill,
            AcquisitionPrice = ownership.AcquisitionPrice,
            AcquisitionCurrency = ownership.AcquisitionCurrency,
            AcquisitionPriceUsd = ownership.AcquisitionPriceUsd
        };
    }

    /// <summary>
    /// Half-open intervals: [EffectiveFrom, EffectiveTill). Null EffectiveTill means open-ended.
    /// Contiguous periods that meet at the same boundary date do not overlap.
    /// </summary>
    public static bool PeriodsOverlap(
        DateOnly leftFrom,
        DateOnly? leftTill,
        DateOnly rightFrom,
        DateOnly? rightTill)
    {
        var leftEnd = leftTill ?? DateOnly.MaxValue;
        var rightEnd = rightTill ?? DateOnly.MaxValue;
        return leftFrom < rightEnd && rightFrom < leftEnd;
    }

    private static void EnsureNoOverlap(
        IReadOnlyList<(DateOnly EffectiveFrom, DateOnly? EffectiveTill)> existing,
        DateOnly effectiveFrom,
        DateOnly? effectiveTill)
    {
        foreach (var ownership in existing)
        {
            if (PeriodsOverlap(ownership.EffectiveFrom, ownership.EffectiveTill, effectiveFrom, effectiveTill))
            {
                throw new ConflictException(
                    "The ownership period overlaps an existing ownership period for this property.");
            }
        }
    }

    private static void ValidateRequest(CreateOwnershipRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (request.ContactId == Guid.Empty)
        {
            errors["ContactId"] = ["ContactId is required."];
        }

        if (request.EffectiveFrom == default)
        {
            errors["EffectiveFrom"] = ["EffectiveFrom is required."];
        }

        if (request.EffectiveTill is not null && request.EffectiveTill.Value <= request.EffectiveFrom)
        {
            errors["EffectiveTill"] = ["EffectiveTill must be after EffectiveFrom."];
        }

        if (request.AcquisitionPrice < 0)
        {
            errors["AcquisitionPrice"] = ["AcquisitionPrice cannot be negative."];
        }

        if (!InputRules.IsValidCurrency(request.AcquisitionCurrency))
        {
            errors["AcquisitionCurrency"] = ["AcquisitionCurrency must be a 3-letter ISO 4217 code."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private async Task EnsurePropertyExistsAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Properties.AnyAsync(property => property.Id == propertyId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException($"Property '{propertyId}' was not found.");
        }
    }

    private async Task EnsureContactExistsAsync(Guid contactId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Contacts.AnyAsync(contact => contact.Id == contactId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException($"Contact '{contactId}' was not found.");
        }
    }
}
