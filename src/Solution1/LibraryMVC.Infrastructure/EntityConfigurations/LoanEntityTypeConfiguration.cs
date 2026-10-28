using LibraryMVC.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LibraryMVC.Infrastructure.EntityConfigurations
{
    internal class LoanEntityTypeConfiguration : IEntityTypeConfiguration<Loan>
    {
        public void Configure(EntityTypeBuilder<Loan> builder)
        {
            builder.HasKey(loan => loan.Id);

            builder.Property(loan => loan.LoanDate)
                .IsRequired();

            builder.HasOne(loan => loan.Book)           //у Loan є одна Book
                .WithMany()                             //у Book багато Loan
                .HasForeignKey(loan => loan.BookId)     //зовнішній ключ - BookId
                .OnDelete(DeleteBehavior.Restrict);     //при видаленні книги видачі не чіпаємо;
                                                        //не можна видалити книгу, якщо є записи видач (БД заборонить)

            builder.HasOne(loan => loan.Reader)         //у видачі один читач
                .WithMany()                             //у читача може бути багато видач
                .HasForeignKey(loan => loan.ReaderId)
                .OnDelete(DeleteBehavior.Restrict);     //не можна видалити читача з історією видач


        }   
    }
}
