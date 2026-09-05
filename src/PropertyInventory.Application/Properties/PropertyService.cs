using Microsoft.EntityFrameworkCore;
using PropertyInventory.Application.Common.Exceptions;
using PropertyInventory.Application.Common.Interfaces;
using PropertyInventory.Application.Common.Models;
using PropertyInventory.Application.Common.Validation;
using PropertyInventory.Domain.Entities;

namespace PropertyInventory.Application.Properties;

public class PropertyService
{
    private readonly IApplicationDbContext _dbContext;

    public PropertyService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<PropertyDto>> GetAsync(PropertyQuery query, CancellationToken cancellationToken = default)
    {
        if (query.MinPrice is not null && query.MaxPrice is not null && query.MinPrice > query.MaxPrice)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["MinPrice"] = ["MinPrice cannot be greater than MaxPrice."]
            });
        }

        IQueryable<Property> properties = _dbContext.Properties.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var name = query.Name.Trim().ToLower();
            properties = properties.Where(property => property.Name.ToLower().Contains(name));
        }

        if (!string.IsNullOrWhiteSpace(query.Address))
        {
            var address = query.Address.Trim().ToLower();
            properties = properties.Where(property => property.Address.ToLower().Contains(address));
        }

        if (query.MinPrice is not null)
        {
            properties = properties.Where(property => property.Price >= query.MinPrice.Value);
        }

        if (query.MaxPrice is not null)
        {
            properties = properties.Where(property => property.Price <= query.MaxPrice.Value);
        }

        var totalCount = await properties.CountAsync(cancellationToken);

        var items = await properties
            .OrderBy(property => property.Name)
            .ThenBy(property => property.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(property => new PropertyDto
            {
                Id = property.Id,
                Name = property.Name,
                Address = property.Address,
                Price = property.Price,
                Currency = property.Currency,
                DateOfRegistration = property.DateOfRegistration
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<PropertyDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PropertyDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var property = await _dbContext.Properties
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        if (property is null)
        {
            throw new NotFoundException($"Property '{id}' was not found.");
        }

        return ToDto(property);
    }

    public async Task<PropertyDto> CreateAsync(CreatePropertyRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCreate(request);

        var property = MapNewProperty(request);
        _dbContext.Properties.Add(property);
        _dbContext.PropertyPriceHistories.Add(CreateInitialPriceHistory(property));

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(property);
    }

    public async Task<IReadOnlyList<PropertyDto>> CreateBatchAsync(
        IReadOnlyList<CreatePropertyRequest> requests,
        CancellationToken cancellationToken = default)
    {
        if (requests is null || requests.Count == 0)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Requests"] = ["At least one property is required."]
            });
        }

        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < requests.Count; index++)
        {
            CollectCreateErrors(requests[index], $"[{index}]", errors);
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var created = new List<Property>(requests.Count);
        foreach (var request in requests)
        {
            var property = MapNewProperty(request);
            _dbContext.Properties.Add(property);
            _dbContext.PropertyPriceHistories.Add(CreateInitialPriceHistory(property));
            created.Add(property);
        }

        // Single SaveChanges keeps the batch atomic on relational providers.
        await _dbContext.SaveChangesAsync(cancellationToken);
        return created.Select(ToDto).ToList();
    }

    public async Task<PropertyDto> UpdateAsync(Guid id, UpdatePropertyRequest request, CancellationToken cancellationToken = default)
    {
        ValidateUpdate(request);

        var property = await _dbContext.Properties
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        if (property is null)
        {
            throw new NotFoundException($"Property '{id}' was not found.");
        }

        ApplyUpdate(property, request);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(property);
    }

    public async Task<IReadOnlyList<PropertyDto>> UpdateBatchAsync(
        IReadOnlyList<UpdatePropertyBatchItem> requests,
        CancellationToken cancellationToken = default)
    {
        if (requests is null || requests.Count == 0)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Requests"] = ["At least one property update is required."]
            });
        }

        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var ids = new HashSet<Guid>();

        for (var index = 0; index < requests.Count; index++)
        {
            var prefix = $"[{index}]";
            var item = requests[index];

            if (item.Id == Guid.Empty)
            {
                errors[$"{prefix}.Id"] = ["Id is required."];
            }
            else if (!ids.Add(item.Id))
            {
                errors[$"{prefix}.Id"] = ["Duplicate property Id in batch."];
            }

            CollectUpdateErrors(item, prefix, errors);
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var existing = await _dbContext.Properties
            .Where(property => ids.Contains(property.Id))
            .ToListAsync(cancellationToken);

        if (existing.Count != ids.Count)
        {
            var missing = ids.Except(existing.Select(property => property.Id)).ToList();
            throw new NotFoundException(
                $"One or more properties were not found: {string.Join(", ", missing)}.");
        }

        var byId = existing.ToDictionary(property => property.Id);
        foreach (var item in requests)
        {
            ApplyUpdate(byId[item.Id], new UpdatePropertyRequest
            {
                Name = item.Name,
                Address = item.Address,
                Price = item.Price,
                Currency = item.Currency,
                DateOfRegistration = item.DateOfRegistration
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return requests.Select(item => ToDto(byId[item.Id])).ToList();
    }

    private void ApplyUpdate(Property property, UpdatePropertyRequest request)
    {
        var currency = InputRules.NormalizeCurrency(request.Currency);
        var priceChanged = property.Price != request.Price ||
                           !string.Equals(property.Currency, currency, StringComparison.Ordinal);

        property.Name = request.Name.Trim();
        property.Address = request.Address.Trim();
        property.Price = request.Price;
        property.Currency = currency;
        property.DateOfRegistration = request.DateOfRegistration;

        if (priceChanged)
        {
            _dbContext.PropertyPriceHistories.Add(new PropertyPriceHistory
            {
                Id = Guid.NewGuid(),
                PropertyId = property.Id,
                Amount = property.Price,
                Currency = property.Currency,
                EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow)
            });
        }
    }

    private static Property MapNewProperty(CreatePropertyRequest request) => new()
    {
        Id = Guid.NewGuid(),
        Name = request.Name.Trim(),
        Address = request.Address.Trim(),
        Price = request.Price,
        Currency = InputRules.NormalizeCurrency(request.Currency),
        DateOfRegistration = request.DateOfRegistration
    };

    private static PropertyPriceHistory CreateInitialPriceHistory(Property property) => new()
    {
        Id = Guid.NewGuid(),
        PropertyId = property.Id,
        Amount = property.Price,
        Currency = property.Currency,
        EffectiveDate = property.DateOfRegistration
    };

    private static void ValidateCreate(CreatePropertyRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        CollectCreateErrors(request, string.Empty, errors);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static void ValidateUpdate(UpdatePropertyRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        CollectUpdateErrors(request, string.Empty, errors);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static void CollectCreateErrors(
        CreatePropertyRequest request,
        string prefix,
        IDictionary<string, string[]> errors) =>
        CollectPropertyFieldErrors(
            request.Name,
            request.Address,
            request.Price,
            request.Currency,
            request.DateOfRegistration,
            prefix,
            errors);

    private static void CollectUpdateErrors(
        UpdatePropertyRequest request,
        string prefix,
        IDictionary<string, string[]> errors) =>
        CollectPropertyFieldErrors(
            request.Name,
            request.Address,
            request.Price,
            request.Currency,
            request.DateOfRegistration,
            prefix,
            errors);

    private static void CollectUpdateErrors(
        UpdatePropertyBatchItem request,
        string prefix,
        IDictionary<string, string[]> errors) =>
        CollectPropertyFieldErrors(
            request.Name,
            request.Address,
            request.Price,
            request.Currency,
            request.DateOfRegistration,
            prefix,
            errors);

    private static void CollectPropertyFieldErrors(
        string name,
        string address,
        decimal price,
        string currency,
        DateOnly dateOfRegistration,
        string prefix,
        IDictionary<string, string[]> errors)
    {
        string Key(string field) => string.IsNullOrEmpty(prefix) ? field : $"{prefix}.{field}";

        if (string.IsNullOrWhiteSpace(name))
        {
            errors[Key("Name")] = ["Name is required."];
        }
        else if (name.Trim().Length > 200)
        {
            errors[Key("Name")] = ["Name must be 200 characters or fewer."];
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            errors[Key("Address")] = ["Address is required."];
        }
        else if (address.Trim().Length > 500)
        {
            errors[Key("Address")] = ["Address must be 500 characters or fewer."];
        }

        if (price < 0)
        {
            errors[Key("Price")] = ["Price cannot be negative."];
        }

        if (!InputRules.IsValidCurrency(currency))
        {
            errors[Key("Currency")] = ["Currency must be a 3-letter ISO 4217 code."];
        }

        if (dateOfRegistration == default)
        {
            errors[Key("DateOfRegistration")] = ["DateOfRegistration is required."];
        }
    }

    private static PropertyDto ToDto(Property property) => new()
    {
        Id = property.Id,
        Name = property.Name,
        Address = property.Address,
        Price = property.Price,
        Currency = property.Currency,
        DateOfRegistration = property.DateOfRegistration
    };
}
