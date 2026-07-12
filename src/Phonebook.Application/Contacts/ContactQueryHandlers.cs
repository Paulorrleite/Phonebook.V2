using FluentValidation;
using MediatR;
using Phonebook.Application.Abstractions;

namespace Phonebook.Application.Contacts;

public sealed class GetContactForEditQueryHandler(
    IValidator<GetContactForEditQuery> validator,
    IContactReadService readService)
    : IRequestHandler<GetContactForEditQuery, ContactEditDto?>
{
    public async Task<ContactEditDto?> Handle(GetContactForEditQuery request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken).ConfigureAwait(false);

        return await readService.GetForEditAsync(request.Id, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class ListContactsQueryHandler(
    IValidator<ListContactsQuery> validator,
    IContactReadService readService)
    : IRequestHandler<ListContactsQuery, PagedResult<ContactListItemDto>>
{
    public async Task<PagedResult<ContactListItemDto>> Handle(ListContactsQuery request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken).ConfigureAwait(false);

        return await readService.ListAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
