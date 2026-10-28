using LibraryMVC.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LibraryMVC.Infrastructure.EntityConfigurations
{
    internal class AuthorEntityTypeConfiguration : IEntityTypeConfiguration<Author>
    {
        public void Configure(EntityTypeBuilder<Author> builder)
        {
            builder.HasKey(author => author.Id);

            builder.Property(author => author.FullName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(author => author.Biography)
                .HasMaxLength(2000);

            builder.Metadata
                .FindNavigation(nameof(Author.Books))?
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
