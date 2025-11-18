using DeviceService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeviceService.Infrastructure.Persistence.Configurations
{
    public class DeviceConfiguration : IEntityTypeConfiguration<Device>
    {
        public void Configure(EntityTypeBuilder<Device> builder)
        {
            builder.HasKey(d => d.Id);

            builder.Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(d => d.Type)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(d => d.Location)
                .HasMaxLength(200);

             builder.Property(d => d.SerialNumber)
                .HasMaxLength(100);

            builder.Property(d => d.ThresholdWatts);

            builder.Property(d => d.IsOnline);

            builder.Property(d => d.RegisteredAt)
                .IsRequired();
        }
    }
}
