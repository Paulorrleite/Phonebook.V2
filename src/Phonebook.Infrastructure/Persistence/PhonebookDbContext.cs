using Microsoft.EntityFrameworkCore;
using Phonebook.Application.Abstractions;
using Phonebook.Domain.Contacts;

namespace Phonebook.Infrastructure.Persistence;

public sealed class PhonebookDbContext(DbContextOptions<PhonebookDbContext> options)
    : DbContext(options), IPhonebookUnitOfWork
{
    public DbSet<Contact> Contacts => Set<Contact>();

    public DbSet<PhoneNumber> PhoneNumbers => Set<PhoneNumber>();

    public DbSet<Address> Addresses => Set<Address>();

    public DbSet<ContactPhoto> ContactPhotos => Set<ContactPhoto>();

    async Task IPhonebookUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureContact(modelBuilder);
        ConfigurePhoneNumber(modelBuilder);
        ConfigureAddress(modelBuilder);
        ConfigureContactPhoto(modelBuilder);
    }

    private static void ConfigureContact(ModelBuilder modelBuilder)
    {
        var contact = modelBuilder.Entity<Contact>();

        contact.ToTable("contacts");
        contact.HasKey(entity => entity.Id);

        contact.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        contact.Property(entity => entity.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();
        contact.Property(entity => entity.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100);
        contact.Property(entity => entity.Email)
            .HasColumnName("email")
            .HasMaxLength(254);
        contact.Property(entity => entity.CreatedAtUtc).HasColumnName("created_at_utc");
        contact.Property(entity => entity.UpdatedAtUtc).HasColumnName("updated_at_utc");

        contact.HasIndex(entity => entity.Email)
            .IsUnique()
            .HasFilter("email IS NOT NULL");

        contact.Navigation(entity => entity.PhoneNumbers)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        contact.HasMany(entity => entity.PhoneNumbers)
            .WithOne()
            .HasForeignKey("contact_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        contact.HasOne(entity => entity.Address)
            .WithOne()
            .HasForeignKey<Address>("contact_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        contact.HasOne(entity => entity.Photo)
            .WithOne()
            .HasForeignKey<ContactPhoto>("contact_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigurePhoneNumber(ModelBuilder modelBuilder)
    {
        var phoneNumber = modelBuilder.Entity<PhoneNumber>();

        phoneNumber.ToTable("phone_numbers");
        phoneNumber.HasKey(entity => entity.Id);

        phoneNumber.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        phoneNumber.Property<Guid>("contact_id").HasColumnName("contact_id");
        phoneNumber.Property(entity => entity.CountryCode)
            .HasColumnName("country_code")
            .HasMaxLength(8)
            .IsRequired();
        phoneNumber.Property(entity => entity.AreaCode)
            .HasColumnName("area_code")
            .HasMaxLength(10)
            .IsRequired();
        phoneNumber.Property(entity => entity.Number)
            .HasColumnName("number")
            .HasMaxLength(50)
            .IsRequired();
        phoneNumber.Property(entity => entity.NormalizedNumber)
            .HasColumnName("normalized_number")
            .HasMaxLength(64)
            .IsRequired();
        phoneNumber.Property(entity => entity.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        phoneNumber.Property(entity => entity.IsFavorite).HasColumnName("is_favorite");

        phoneNumber.HasIndex("contact_id", nameof(PhoneNumber.NormalizedNumber)).IsUnique();
        phoneNumber.HasIndex("contact_id");
    }

    private static void ConfigureAddress(ModelBuilder modelBuilder)
    {
        var address = modelBuilder.Entity<Address>();

        address.ToTable("addresses");
        address.HasKey(entity => entity.Id);

        address.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        address.Property<Guid>("contact_id").HasColumnName("contact_id");
        address.Property(entity => entity.Country)
            .HasColumnName("country")
            .HasMaxLength(100)
            .IsRequired();
        address.Property(entity => entity.State)
            .HasColumnName("state")
            .HasMaxLength(100)
            .IsRequired();
        address.Property(entity => entity.City)
            .HasColumnName("city")
            .HasMaxLength(100)
            .IsRequired();
        address.Property(entity => entity.Neighborhood)
            .HasColumnName("neighborhood")
            .HasMaxLength(100)
            .IsRequired();
        address.Property(entity => entity.PostalCode)
            .HasColumnName("postal_code")
            .HasMaxLength(32)
            .IsRequired();

        address.HasIndex("contact_id").IsUnique();
    }

    private static void ConfigureContactPhoto(ModelBuilder modelBuilder)
    {
        var photo = modelBuilder.Entity<ContactPhoto>();

        photo.ToTable("contact_photos");
        photo.HasKey(entity => entity.Id);

        photo.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        photo.Property<Guid>("contact_id").HasColumnName("contact_id");
        photo.Property(entity => entity.Content)
            .HasColumnName("content")
            .IsRequired();
        photo.Property(entity => entity.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(100)
            .IsRequired();
        photo.Property(entity => entity.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(255)
            .IsRequired();
        photo.Property(entity => entity.Size).HasColumnName("size");

        photo.HasIndex("contact_id").IsUnique();
    }
}
