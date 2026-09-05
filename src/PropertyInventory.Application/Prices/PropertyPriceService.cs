using Microsoft.EntityFrameworkCore;
using PropertyInventory.Application.Common.Exceptions;
using PropertyInventory.Application.Common.Interfaces;
using PropertyInventory.Application.Common.Validation;
using PropertyInventory.Domain.Entities;

namespace PropertyInventory.Application.Prices;

/// <summary>
/// Single application path for asking-price history and current Property.Price/Currency updates.
/// Ownership acquisition prices are never modified here.
/// </summary>
public class PropertyPriceService
{
    private readonly IApplicationDbContext _dbContext;

    public PropertyPriceService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PropertyPriceDto>> GetByPropertyIdAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        await EnsurePropertyExistsAsync(propertyId, cancellationToken);

        return await _dbContext.PropertyPriceHistories
            .AsNoTracking()
            .Where(history => history.PropertyId == propertyId)
            .OrderBy(history => history.EffectiveDate)
            .ThenBy(history => history.Id)
            .Select(history => new PropertyPriceDto
            {
                Id = history.Id,
                PropertyId = history.PropertyId,
                Amount = history.Amount,
                Currency = history.Currency,
                EffectiveDate = history.EffectiveDate
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PropertyPriceDto> CreateAsync(
        Guid propertyId,
        CreatePropertyPriceRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCreateRequest(request);

        var property = await _dbContext.Properties
            .FirstOrDefaultAsync(entity => entity.Id == propertyId, cancellationToken);

        if (property is null)
        {
            throw new NotFoundException($"Property '{propertyId}' was not found.");
        }

        var currency = InputRules.NormalizeCurrency(request.Currency);
        var history = ApplyAskingPriceChange(
            property,
            request.Amount,
            currency,
            request.EffectiveDate,
            forceRecord: true);

        await using var transaction = await _dbContext.BeginTransactionAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return ToDto(history);
    }

    /// <summary>
    /// Records the initial asking price when a property is created.
    /// Always writes a history row (even if callers later change other fields without a price change).
    /// </summary>
    public PropertyPriceHistory RecordInitialAskingPrice(Property property)
    {
        ArgumentNullException.ThrowIfNull(property);

        var history = new PropertyPriceHistory
        {
            Id = Guid.NewGuid(),
            PropertyId = property.Id,
            Amount = property.Price,
            Currency = property.Currency,
            EffectiveDate = property.DateOfRegistration
        };

        _dbContext.PropertyPriceHistories.Add(history);
        return history;
    }

    /// <summary>
    /// Updates current asking price/currency on a tracked <see cref="Property"/> and appends history
    /// when the values actually change. Used by Property PUT and by POST /prices.
    /// </summary>
    /// <param name="forceRecord">
    /// When true (explicit price-change API), always write a history row after applying the values.
    /// When false (Property PUT), skip history if Price and Currency are unchanged.
    /// </param>
    public PropertyPriceHistory ApplyAskingPriceChange(
        Property property,
        decimal amount,
        string currency,
        DateOnly effectiveDate,
        bool forceRecord = false)
    {
        ArgumentNullException.ThrowIfNull(property);

        var normalizedCurrency = InputRules.NormalizeCurrency(currency);
        var priceChanged = property.Price != amount ||
                           !string.Equals(property.Currency, normalizedCurrency, StringComparison.Ordinal);

        property.Price = amount;
        property.Currency = normalizedCurrency;

        if (!forceRecord && !priceChanged)
        {
            // Caller (Property PUT) updated non-price fields only, or repeated the same price.
            return new PropertyPriceHistory
            {
                Id = Guid.Empty,
                PropertyId = property.Id,
                Amount = property.Price,
                Currency = property.Currency,
                EffectiveDate = effectiveDate
            };
        }

        var history = new PropertyPriceHistory
        {
            Id = Guid.NewGuid(),
            PropertyId = property.Id,
            Amount = amount,
            Currency = normalizedCurrency,
            EffectiveDate = effectiveDate
        };

        _dbContext.PropertyPriceHistories.Add(history);
        return history;
    }

    private async Task EnsurePropertyExistsAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Properties.AnyAsync(property => property.Id == propertyId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException($"Property '{propertyId}' was not found.");
        }
    }

    private static void ValidateCreateRequest(CreatePropertyPriceRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (request.Amount <= 0)
        {
            errors["Amount"] = ["Amount must be a positive decimal value."];
        }

        if (!InputRules.IsValidCurrency(request.Currency))
        {
            errors["Currency"] = ["Currency must be a 3-letter ISO 4217 code."];
        }

        if (request.EffectiveDate == default)
        {
            errors["EffectiveDate"] = ["EffectiveDate is required."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static PropertyPriceDto ToDto(PropertyPriceHistory history) => new()
    {
        Id = history.Id,
        PropertyId = history.PropertyId,
        Amount = history.Amount,
        Currency = history.Currency,
        EffectiveDate = history.EffectiveDate
    };
}
