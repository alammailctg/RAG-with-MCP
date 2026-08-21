using LocalRag.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalRag.Infrastructure.EntityTypeConfiguration
{
    public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
    {
        public void Configure(EntityTypeBuilder<DocumentChunk> builder)
        {
            builder.ToTable("DocumentChunks");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();


            builder.Property(x => x.DocumentId)
                .IsRequired()
                .HasMaxLength(100);


            builder.Property(x => x.Title)
                .HasMaxLength(250);


            builder.Property(x => x.Content)
                .IsRequired();


            builder.Property(x => x.Embedding)
                .HasColumnType("vector(768)")
                .IsRequired();


            builder.Property(x => x.Distance)
                .HasDefaultValue(0);


            builder.Property(x => x.CreatedAt)
                .IsRequired();
        }
    }
}
