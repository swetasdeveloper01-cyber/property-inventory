using Microsoft.EntityFrameworkCore;
using PropertyInventory.Application.Common.Exceptions;
using PropertyInventory.Application.Common.Interfaces;
using PropertyInventory.Application.Common.Models;
using PropertyInventory.Application.Common.Validation;
using PropertyInventory.Domain.Entities;

namespace PropertyInventory.Application.Contacts;

public class ContactService
{
    private readonly IApplicationDbContext _dbContext;

    public ContactService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ContactDto>> GetAsync(ContactQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<Contact> contacts = _dbContext.Contacts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.FirstName))
        {
            var firstName = query.FirstName.Trim().ToLower();
            contacts = contacts.Where(contact => contact.FirstName.ToLower().Contains(firstName));
        }

        if (!string.IsNullOrWhiteSpace(query.LastName))
        {
            var lastName = query.LastName.Trim().ToLower();
            contacts = contacts.Where(contact => contact.LastName.ToLower().Contains(lastName));
        }

        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            var email = query.Email.Trim().ToLower();
            contacts = contacts.Where(contact => contact.Email.ToLower().Contains(email));
        }

        if (!string.IsNullOrWhiteSpace(query.Phone))
        {
            var phone = query.Phone.Trim().ToLower();
            contacts = contacts.Where(contact => contact.PhoneNumber.ToLower().Contains(phone));
        }

        var totalCount = await contacts.CountAsync(cancellationToken);

        var items = await contacts
            .OrderBy(contact => contact.LastName)
            .ThenBy(contact => contact.FirstName)
            .ThenBy(contact => contact.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(contact => new ContactDto
            {
                Id = contact.Id,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                PhoneNumber = contact.PhoneNumber,
                Email = contact.Email
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ContactDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ContactDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contact = await _dbContext.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        if (contact is null)
        {
            throw new NotFoundException($"Contact '{id}' was not found.");
        }

        return ToDto(contact);
    }

    public async Task<ContactDto> CreateAsync(CreateContactRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCreate(request);
        var email = InputRules.NormalizeEmail(request.Email);
        await EnsureEmailIsUniqueAsync(email, excludeContactId: null, cancellationToken);

        var contact = MapNewContact(request, email);
        _dbContext.Contacts.Add(contact);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(contact);
    }

    public async Task<IReadOnlyList<ContactDto>> CreateBatchAsync(
        IReadOnlyList<CreateContactRequest> requests,
        CancellationToken cancellationToken = default)
    {
        if (requests is null || requests.Count == 0)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Requests"] = ["At least one contact is required."]
            });
        }

        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var emailsInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < requests.Count; index++)
        {
            var prefix = $"[{index}]";
            CollectCreateErrors(requests[index], prefix, errors);

            if (InputRules.IsValidEmail(requests[index].Email))
            {
                var email = InputRules.NormalizeEmail(requests[index].Email);
                if (!emailsInBatch.Add(email))
                {
                    errors[$"{prefix}.Email"] = ["Duplicate email in batch."];
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        foreach (var email in emailsInBatch)
        {
            await EnsureEmailIsUniqueAsync(email, excludeContactId: null, cancellationToken);
        }

        var created = new List<Contact>(requests.Count);
        foreach (var request in requests)
        {
            var contact = MapNewContact(request, InputRules.NormalizeEmail(request.Email));
            _dbContext.Contacts.Add(contact);
            created.Add(contact);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return created.Select(ToDto).ToList();
    }

    public async Task<ContactDto> UpdateAsync(Guid id, UpdateContactRequest request, CancellationToken cancellationToken = default)
    {
        ValidateUpdate(request);

        var contact = await _dbContext.Contacts
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        if (contact is null)
        {
            throw new NotFoundException($"Contact '{id}' was not found.");
        }

        var email = InputRules.NormalizeEmail(request.Email);
        await EnsureEmailIsUniqueAsync(email, excludeContactId: id, cancellationToken);

        ApplyUpdate(contact, request, email);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(contact);
    }

    public async Task<IReadOnlyList<ContactDto>> UpdateBatchAsync(
        IReadOnlyList<UpdateContactBatchItem> requests,
        CancellationToken cancellationToken = default)
    {
        if (requests is null || requests.Count == 0)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Requests"] = ["At least one contact update is required."]
            });
        }

        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var ids = new HashSet<Guid>();
        var emailsInBatch = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

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
                errors[$"{prefix}.Id"] = ["Duplicate contact Id in batch."];
            }

            CollectUpdateErrors(item, prefix, errors);

            if (InputRules.IsValidEmail(item.Email))
            {
                var email = InputRules.NormalizeEmail(item.Email);
                if (!emailsInBatch.TryAdd(email, item.Id))
                {
                    errors[$"{prefix}.Email"] = ["Duplicate email in batch."];
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var existing = await _dbContext.Contacts
            .Where(contact => ids.Contains(contact.Id))
            .ToListAsync(cancellationToken);

        if (existing.Count != ids.Count)
        {
            var missing = ids.Except(existing.Select(contact => contact.Id)).ToList();
            throw new NotFoundException(
                $"One or more contacts were not found: {string.Join(", ", missing)}.");
        }

        foreach (var pair in emailsInBatch)
        {
            await EnsureEmailIsUniqueAsync(pair.Key, excludeContactId: pair.Value, cancellationToken);
        }

        var byId = existing.ToDictionary(contact => contact.Id);
        foreach (var item in requests)
        {
            ApplyUpdate(byId[item.Id], new UpdateContactRequest
            {
                FirstName = item.FirstName,
                LastName = item.LastName,
                PhoneNumber = item.PhoneNumber,
                Email = item.Email
            }, InputRules.NormalizeEmail(item.Email));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return requests.Select(item => ToDto(byId[item.Id])).ToList();
    }

    private async Task EnsureEmailIsUniqueAsync(
        string email,
        Guid? excludeContactId,
        CancellationToken cancellationToken)
    {
        var normalized = email.ToLower();
        var exists = await _dbContext.Contacts.AnyAsync(
            contact => contact.Email.ToLower() == normalized &&
                       (!excludeContactId.HasValue || contact.Id != excludeContactId.Value),
            cancellationToken);

        if (exists)
        {
            throw new ConflictException($"A contact with email '{email}' already exists.");
        }
    }

    private static Contact MapNewContact(CreateContactRequest request, string email) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = request.FirstName.Trim(),
        LastName = request.LastName.Trim(),
        PhoneNumber = request.PhoneNumber.Trim(),
        Email = email
    };

    private static void ApplyUpdate(Contact contact, UpdateContactRequest request, string email)
    {
        contact.FirstName = request.FirstName.Trim();
        contact.LastName = request.LastName.Trim();
        contact.PhoneNumber = request.PhoneNumber.Trim();
        contact.Email = email;
    }

    private static void ValidateCreate(CreateContactRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        CollectCreateErrors(request, string.Empty, errors);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static void ValidateUpdate(UpdateContactRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        CollectUpdateErrors(request, string.Empty, errors);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static void CollectCreateErrors(
        CreateContactRequest request,
        string prefix,
        IDictionary<string, string[]> errors) =>
        CollectContactFieldErrors(
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.Email,
            prefix,
            errors);

    private static void CollectUpdateErrors(
        UpdateContactRequest request,
        string prefix,
        IDictionary<string, string[]> errors) =>
        CollectContactFieldErrors(
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.Email,
            prefix,
            errors);

    private static void CollectUpdateErrors(
        UpdateContactBatchItem request,
        string prefix,
        IDictionary<string, string[]> errors) =>
        CollectContactFieldErrors(
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.Email,
            prefix,
            errors);

    private static void CollectContactFieldErrors(
        string firstName,
        string lastName,
        string phoneNumber,
        string email,
        string prefix,
        IDictionary<string, string[]> errors)
    {
        string Key(string field) => string.IsNullOrEmpty(prefix) ? field : $"{prefix}.{field}";

        if (string.IsNullOrWhiteSpace(firstName))
        {
            errors[Key("FirstName")] = ["FirstName is required."];
        }
        else if (firstName.Trim().Length > 100)
        {
            errors[Key("FirstName")] = ["FirstName must be 100 characters or fewer."];
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            errors[Key("LastName")] = ["LastName is required."];
        }
        else if (lastName.Trim().Length > 100)
        {
            errors[Key("LastName")] = ["LastName must be 100 characters or fewer."];
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            errors[Key("PhoneNumber")] = ["PhoneNumber is required."];
        }
        else if (phoneNumber.Trim().Length > 30)
        {
            errors[Key("PhoneNumber")] = ["PhoneNumber must be 30 characters or fewer."];
        }

        if (!InputRules.IsValidEmail(email))
        {
            errors[Key("Email")] = ["Email must be a valid email address."];
        }
        else if (email.Trim().Length > 256)
        {
            errors[Key("Email")] = ["Email must be 256 characters or fewer."];
        }
    }

    private static ContactDto ToDto(Contact contact) => new()
    {
        Id = contact.Id,
        FirstName = contact.FirstName,
        LastName = contact.LastName,
        PhoneNumber = contact.PhoneNumber,
        Email = contact.Email
    };
}
