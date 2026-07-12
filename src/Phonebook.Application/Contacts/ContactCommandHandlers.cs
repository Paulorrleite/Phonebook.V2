using FluentValidation;
using MediatR;
using Phonebook.Application.Abstractions;
using Phonebook.Domain.Contacts;

namespace Phonebook.Application.Contacts;

public sealed class CreateContactCommandHandler(
    IValidator<CreateContactCommand> validator,
    IContactRepository repository,
    IPhonebookUnitOfWork unitOfWork)
    : IRequestHandler<CreateContactCommand, Guid>
{
    public async Task<Guid> Handle(CreateContactCommand request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureEmailIsUniqueAsync(request.Email, null, cancellationToken).ConfigureAwait(false);

        var contact = new Contact(
            request.FirstName,
            request.LastName,
            request.Email,
            CreatePhones(request.PhoneNumbers),
            CreateAddress(request.Address),
            CreatePhoto(request.Photo));

        await repository.AddAsync(contact, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return contact.Id;
    }

    private async Task EnsureEmailIsUniqueAsync(string? email, Guid? excludingContactId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        if (await repository.EmailExistsAsync(email.Trim(), excludingContactId, cancellationToken).ConfigureAwait(false))
        {
            throw new ValidationException("Email is already used by another contact.");
        }
    }

    private static IReadOnlyCollection<PhoneNumber> CreatePhones(IReadOnlyCollection<PhoneNumberInputDto> phones)
    {
        return phones
            .Select(phone => new PhoneNumber(
                phone.CountryCode,
                phone.AreaCode,
                phone.Number,
                phone.Type,
                phone.IsFavorite))
            .ToList();
    }

    private static Address? CreateAddress(AddressInputDto? address)
    {
        return address is null
            ? null
            : new Address(
                address.Country!,
                address.State!,
                address.City!,
                address.Neighborhood!,
                address.PostalCode!);
    }

    private static ContactPhoto? CreatePhoto(PhotoInputDto? photo)
    {
        return photo is null
            ? null
            : new ContactPhoto(photo.Content, photo.ContentType, photo.FileName, photo.Size);
    }
}

public sealed class UpdateContactCommandHandler(
    IValidator<UpdateContactCommand> validator,
    IContactRepository repository,
    IPhonebookUnitOfWork unitOfWork)
    : IRequestHandler<UpdateContactCommand>
{
    public async Task Handle(UpdateContactCommand request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken).ConfigureAwait(false);

        var contact = await repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (contact is null)
        {
            throw new ContactNotFoundException(request.Id);
        }

        await EnsureEmailIsUniqueAsync(request.Email, request.Id, cancellationToken).ConfigureAwait(false);

        contact.UpdateDetails(request.FirstName, request.LastName, request.Email);
        contact.ReplacePhones(CreatePhones(request.PhoneNumbers));

        if (request.Address is null)
        {
            contact.RemoveAddress();
        }
        else
        {
            contact.SetAddress(new Address(
                request.Address.Country!,
                request.Address.State!,
                request.Address.City!,
                request.Address.Neighborhood!,
                request.Address.PostalCode!));
        }

        if (request.Photo is not null)
        {
            contact.SetPhoto(new ContactPhoto(
                request.Photo.Content,
                request.Photo.ContentType,
                request.Photo.FileName,
                request.Photo.Size));
        }
        else if (request.RemovePhoto)
        {
            contact.RemovePhoto();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureEmailIsUniqueAsync(string? email, Guid excludingContactId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        if (await repository.EmailExistsAsync(email.Trim(), excludingContactId, cancellationToken).ConfigureAwait(false))
        {
            throw new ValidationException("Email is already used by another contact.");
        }
    }

    private static IReadOnlyCollection<PhoneNumber> CreatePhones(IReadOnlyCollection<PhoneNumberInputDto> phones)
    {
        return phones
            .Select(phone => new PhoneNumber(
                phone.CountryCode,
                phone.AreaCode,
                phone.Number,
                phone.Type,
                phone.IsFavorite))
            .ToList();
    }
}

public sealed class DeleteContactCommandHandler(
    IValidator<DeleteContactCommand> validator,
    IContactRepository repository,
    IPhonebookUnitOfWork unitOfWork)
    : IRequestHandler<DeleteContactCommand>
{
    public async Task Handle(DeleteContactCommand request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken).ConfigureAwait(false);

        var contact = await repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (contact is null)
        {
            throw new ContactNotFoundException(request.Id);
        }

        repository.Delete(contact);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
