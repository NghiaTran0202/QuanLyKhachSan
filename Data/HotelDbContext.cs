using Microsoft.EntityFrameworkCore;
using quanlykhachsan.Models;

namespace quanlykhachsan.Data
{
    public class HotelDbContext : DbContext
    {
        public HotelDbContext(DbContextOptions<HotelDbContext> options)
            : base(options)
        {
        }

        public DbSet<Admin> Admins { get; set; }

        public DbSet<Customer> Customers { get; set; }

        public DbSet<Room> Rooms { get; set; }

        public DbSet<RoomType> RoomTypes { get; set; }

        public DbSet<Booking> Bookings { get; set; }

        public DbSet<Service> Services { get; set; }

        public DbSet<Invoice> Invoices { get; set; }

        public DbSet<BookingService> BookingServices { get; set; }

        public DbSet<Contact> Contacts { get; set; }

        public DbSet<RoomChangeHistory> RoomChangeHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RoomChangeHistory>()
                .HasOne(r => r.Booking)
                .WithMany(b => b.RoomChangeHistories)
                .HasForeignKey(r => r.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoomChangeHistory>()
                .HasOne(r => r.OldRoom)
                .WithMany()
                .HasForeignKey(r => r.OldRoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoomChangeHistory>()
                .HasOne(r => r.NewRoom)
                .WithMany()
                .HasForeignKey(r => r.NewRoomId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}