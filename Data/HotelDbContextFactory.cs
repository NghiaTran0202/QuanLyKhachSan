using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace quanlykhachsan.Data
{
    public class HotelDbContextFactory : IDesignTimeDbContextFactory<HotelDbContext>
    {
        public HotelDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<HotelDbContext>();

            optionsBuilder.UseSqlServer(
    "Server=ARTHUR-TUF\\SQLEXPRESS;Database=QuanLyKhachSanDB;Trusted_Connection=True;TrustServerCertificate=True");
            return new HotelDbContext(optionsBuilder.Options);
        }
    }
}