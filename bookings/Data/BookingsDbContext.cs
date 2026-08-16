using Microsoft.EntityFrameworkCore;

namespace AzMicroApp.Bookings.Data;

public sealed class BookingsDbContext : DbContext
{
    public BookingsDbContext(DbContextOptions<BookingsDbContext> options) : base(options) { }

    public DbSet<BookingEntity> Bookings => Set<BookingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookingEntity>(e =>
        {
            e.ToTable("bookings");
            e.HasKey(b => b.Id);
            e.Property(b => b.Id).HasColumnName("id");
            e.Property(b => b.UserId).HasColumnName("user_id").IsRequired();
            e.Property(b => b.HotelId).HasColumnName("hotel_id").IsRequired();
            e.Property(b => b.CheckIn).HasColumnName("check_in").IsRequired();
            e.Property(b => b.CheckOut).HasColumnName("check_out").IsRequired();
            e.Property(b => b.Status).HasColumnName("status").IsRequired();
        });
    }
}
