using LibraryMVC.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LibraryMVC.Infrastructure.EntityConfigurations
{
    internal class QualificationFeatureEntityTypeConfiguration : IEntityTypeConfiguration<QualificationFeature>
    {
        public void Configure(EntityTypeBuilder<QualificationFeature> builder)
        {
            builder.HasKey(feature => feature.Id);

            builder.Property(feature => feature.Name)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(feature => feature.Description)
                .HasMaxLength(1000);

            builder.Metadata
                .FindNavigation(nameof(QualificationFeature.Books))?
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
