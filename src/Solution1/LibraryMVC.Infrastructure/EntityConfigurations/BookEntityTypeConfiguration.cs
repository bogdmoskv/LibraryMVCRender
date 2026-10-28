using LibraryMVC.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LibraryMVC.Infrastructure.EntityConfigurations
{
    internal class BookEntityTypeConfiguration : IEntityTypeConfiguration<Book>
    {
        public void Configure(EntityTypeBuilder<Book> builder)
        {
            builder.HasKey(book => book.Id);

            builder.Property(book => book.Title)
                .HasMaxLength(300)
                .IsRequired();

            builder.Property(book => book.Annotation)
                .HasMaxLength(4000)
                .IsRequired();

            builder.Property(book => book.PublicationYear)
                .IsRequired();

            builder.Property(book => book.Publisher)
                .HasMaxLength(200);

            builder.Property(book => book.Isbn)
                .HasMaxLength(32);

            builder.Property(book => book.CoverPath)
                .HasMaxLength(500);

            builder.Property(book => book.AddedAt)
                .IsRequired();

            builder.HasMany(book => book.Authors)
                .WithMany(author => author.Books)
                .UsingEntity(join => join.ToTable("BookAuthors"));

            builder.HasMany(book => book.Features)
                .WithMany(feature => feature.Books)
                .UsingEntity(join => join.ToTable("BookFeatures"));

            builder.Metadata
                .FindNavigation(nameof(Book.Authors))?
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.Metadata
                .FindNavigation(nameof(Book.Features))?
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
