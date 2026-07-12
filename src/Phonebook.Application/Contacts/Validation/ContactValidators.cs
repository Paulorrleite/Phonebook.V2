using FluentValidation;
using Phonebook.Domain.Contacts;

namespace Phonebook.Application.Contacts.Validation;

public sealed class CreateContactCommandValidator : AbstractValidator<CreateContactCommand>
{
    public CreateContactCommandValidator()
    {
        RuleFor(command => command.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Email)
            .EmailAddress()
            .MaximumLength(254)
            .When(command => !string.IsNullOrWhiteSpace(command.Email));

        RuleFor(command => command.PhoneNumbers)
            .NotNull()
            .Must(phoneNumbers => phoneNumbers is not null && phoneNumbers.Count is >= Contact.MinimumPhoneCount and <= Contact.MaximumPhoneCount)
            .WithMessage("A contact must have between 1 and 10 phone numbers.");

        RuleFor(command => command.PhoneNumbers)
            .Must(HaveUniquePhoneNumbers)
            .WithMessage("Phone numbers must be unique within a contact.");

        RuleFor(command => command.PhoneNumbers)
            .Must(phoneNumbers => phoneNumbers is not null && phoneNumbers.Count(phoneNumber => phoneNumber.IsFavorite) <= 1)
            .WithMessage("Only one phone number can be favorite.");

        RuleForEach(command => command.PhoneNumbers)
            .SetValidator(new PhoneNumberInputDtoValidator())
            .When(command => command.PhoneNumbers is not null);

        RuleFor(command => command.Address)
            .SetValidator(new AddressInputDtoValidator(new PostalCodeValidator())!)
            .When(command => command.Address is not null);

        RuleFor(command => command.Photo)
            .SetValidator(new PhotoInputDtoValidator()!)
            .When(command => command.Photo is not null);
    }

    private static bool HaveUniquePhoneNumbers(IReadOnlyCollection<PhoneNumberInputDto> phoneNumbers)
    {
        if (phoneNumbers is null)
        {
            return false;
        }

        var normalizedPhoneNumbers = phoneNumbers
            .Select(phoneNumber => PhoneNumber.Normalize(
                phoneNumber.CountryCode,
                phoneNumber.AreaCode,
                phoneNumber.Number));

        return normalizedPhoneNumbers.Distinct().Count() == phoneNumbers.Count;
    }
}

public sealed class UpdateContactCommandValidator : AbstractValidator<UpdateContactCommand>
{
    public UpdateContactCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();

        RuleFor(command => command.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Email)
            .EmailAddress()
            .MaximumLength(254)
            .When(command => !string.IsNullOrWhiteSpace(command.Email));

        RuleFor(command => command.PhoneNumbers)
            .NotNull()
            .Must(phoneNumbers => phoneNumbers is not null && phoneNumbers.Count is >= Contact.MinimumPhoneCount and <= Contact.MaximumPhoneCount)
            .WithMessage("A contact must have between 1 and 10 phone numbers.");

        RuleFor(command => command.PhoneNumbers)
            .Must(HaveUniquePhoneNumbers)
            .WithMessage("Phone numbers must be unique within a contact.");

        RuleFor(command => command.PhoneNumbers)
            .Must(phoneNumbers => phoneNumbers is not null && phoneNumbers.Count(phoneNumber => phoneNumber.IsFavorite) <= 1)
            .WithMessage("Only one phone number can be favorite.");

        RuleForEach(command => command.PhoneNumbers)
            .SetValidator(new PhoneNumberInputDtoValidator())
            .When(command => command.PhoneNumbers is not null);

        RuleFor(command => command.Address)
            .SetValidator(new AddressInputDtoValidator(new PostalCodeValidator())!)
            .When(command => command.Address is not null);

        RuleFor(command => command.Photo)
            .SetValidator(new PhotoInputDtoValidator()!)
            .When(command => command.Photo is not null);

        RuleFor(command => command)
            .Must(command => command.Photo is null || !command.RemovePhoto)
            .WithMessage("Cannot upload and remove a photo in the same request.");
    }

    private static bool HaveUniquePhoneNumbers(IReadOnlyCollection<PhoneNumberInputDto> phoneNumbers)
    {
        if (phoneNumbers is null)
        {
            return false;
        }

        var normalizedPhoneNumbers = phoneNumbers
            .Select(phoneNumber => PhoneNumber.Normalize(
                phoneNumber.CountryCode,
                phoneNumber.AreaCode,
                phoneNumber.Number));

        return normalizedPhoneNumbers.Distinct().Count() == phoneNumbers.Count;
    }
}

public sealed class DeleteContactCommandValidator : AbstractValidator<DeleteContactCommand>
{
    public DeleteContactCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class GetContactForEditQueryValidator : AbstractValidator<GetContactForEditQuery>
{
    public GetContactForEditQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}

public sealed class ListContactsQueryValidator : AbstractValidator<ListContactsQuery>
{
    public ListContactsQueryValidator()
    {
        RuleFor(query => query.Search)
            .MaximumLength(200)
            .When(query => query.Search is not null);

        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);
    }
}

public sealed class PhoneNumberInputDtoValidator : AbstractValidator<PhoneNumberInputDto>
{
    public PhoneNumberInputDtoValidator()
    {
        RuleFor(phone => phone.CountryCode)
            .NotEmpty()
            .Matches(@"^\+?\d{1,4}$")
            .WithMessage("Country code must contain 1 to 4 digits and may start with '+'.");

        RuleFor(phone => phone.AreaCode)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(phone => phone.Number)
            .NotEmpty()
            .Matches(@"^[\d\s()+\-.]+$")
            .WithMessage("Phone number contains invalid characters.")
            .Must(value => value.Any(char.IsDigit))
            .WithMessage("Phone number must contain digits.");

        RuleFor(phone => phone.Type)
            .IsInEnum();
    }
}

public sealed class AddressInputDtoValidator : AbstractValidator<AddressInputDto>
{
    private readonly IPostalCodeValidator _postalCodeValidator;

    public AddressInputDtoValidator(IPostalCodeValidator postalCodeValidator)
    {
        _postalCodeValidator = postalCodeValidator;

        RuleFor(address => address.Country).NotEmpty();
        RuleFor(address => address.State).NotEmpty();
        RuleFor(address => address.City).NotEmpty();
        RuleFor(address => address.Neighborhood).NotEmpty();
        RuleFor(address => address.PostalCode).NotEmpty();

        RuleFor(address => address)
            .Must(address => _postalCodeValidator.IsValid(address.Country!, address.PostalCode!))
            .When(address =>
                !string.IsNullOrWhiteSpace(address.Country) &&
                !string.IsNullOrWhiteSpace(address.PostalCode))
            .WithMessage("Postal code is invalid for the selected country.");
    }
}

public sealed class PhotoInputDtoValidator : AbstractValidator<PhotoInputDto>
{
    public PhotoInputDtoValidator()
    {
        RuleFor(photo => photo.Content)
            .NotEmpty();

        RuleFor(photo => photo.ContentType)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(photo => photo.FileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(photo => photo.Size)
            .InclusiveBetween(1, ContactPhoto.MaxSizeInBytes);

        RuleFor(photo => photo)
            .Must(photo => photo.Content.LongLength == photo.Size)
            .WithMessage("Photo size must match the content length.");
    }
}
