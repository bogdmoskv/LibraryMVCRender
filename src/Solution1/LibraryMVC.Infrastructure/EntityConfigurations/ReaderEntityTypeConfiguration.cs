using LibraryMVC.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LibraryMVC.Infrastructure.EntityConfigurations
{
    internal class ReaderEntityTypeConfiguration : IEntityTypeConfiguration<Reader>
    {
        public void Configure(EntityTypeBuilder<Reader> builder)
        {
            builder.HasKey(reader => reader.Id);

            builder.Property(reader => reader.FullName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(reader => reader.Faculty)
               .HasMaxLength(150)
               .IsRequired();

            builder.Property(reader => reader.Department)
               .HasMaxLength(150)
               .IsRequired();

            builder.Property(reader => reader.Position)
               .HasMaxLength(100)
               .IsRequired();

            builder.Property(reader => reader.RegisteredAt)
                .IsRequired();
        }
    }
}
