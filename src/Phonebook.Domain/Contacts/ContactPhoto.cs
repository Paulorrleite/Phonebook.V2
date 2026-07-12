namespace Phonebook.Domain.Contacts;

public sealed class ContactPhoto
{
    public const long MaxSizeInBytes = 750 * 1024;

    private ContactPhoto()
    {
        Content = [];
        ContentType = string.Empty;
        FileName = string.Empty;
    }

    public ContactPhoto(byte[] content, string contentType, string fileName, long size)
    {
        if (content.Length == 0)
        {
            throw new DomainException("Photo content is required.");
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new DomainException("Photo content type is required.");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DomainException("Photo file name is required.");
        }

        if (size <= 0 || size > MaxSizeInBytes)
        {
            throw new DomainException("Photo size must be between 1 byte and 750 KB.");
        }

        if (content.LongLength != size)
        {
            throw new DomainException("Photo size must match the content length.");
        }

        Id = Guid.NewGuid();
        Content = content.ToArray();
        ContentType = contentType.Trim();
        FileName = fileName.Trim();
        Size = size;
    }

    public Guid Id { get; private set; }

    public byte[] Content { get; private set; }

    public string ContentType { get; private set; }

    public string FileName { get; private set; }

    public long Size { get; private set; }
}
