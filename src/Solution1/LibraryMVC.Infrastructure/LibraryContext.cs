using LibraryMVC.Domain.Entities;
using LibraryMVC.Infrastructure.EntityConfigurations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace LibraryMVC.Infrastructure
{
    public class LibraryContext : DbContext
    {
        public LibraryContext(DbContextOptions<LibraryContext> options) : base(options) { }

        public DbSet<Author> Authors => Set<Author>();
        public DbSet<Book> Books => Set<Book>();
        public DbSet<Reader> Readers => Set<Reader>();
        public DbSet<QualificationFeature> QualificationFeatures => Set<QualificationFeature>();
        public DbSet<Loan> Loans => Set<Loan>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new AuthorEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new BookEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new ReaderEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new QualificationFeatureEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new LoanEntityTypeConfiguration());
        }
    }
}
