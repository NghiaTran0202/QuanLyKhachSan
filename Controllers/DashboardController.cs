using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quanlykhachsan.Data;

namespace quanlykhachsan.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly HotelDbContext _context;

        public DashboardController(HotelDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewBag.AdminName = User.Identity?.Name;

            ViewBag.TotalRooms = _context.Rooms.Count();

            ViewBag.EmptyRooms = _context.Rooms.Count(x => x.Status == false);

            ViewBag.UsingRooms = _context.Rooms.Count(x => x.Status == true);

            ViewBag.TotalCustomers = _context.Customers.Count();

            ViewBag.TotalBookings = _context.Bookings.Count();

            ViewBag.TotalInvoices = _context.Invoices.Count();

            ViewBag.TotalRevenue = _context.Invoices
                .Where(x => x.PaymentStatus == "DaThanhToan")
                .Sum(x => (decimal?)x.TotalAmount) ?? 0;

            ViewBag.UnreadContacts = _context.Contacts.Count(x => !x.IsRead);

            var revenue = new decimal[12];

            int currentYear = DateTime.Now.Year;

            for (int i = 1; i <= 12; i++)
            {
                revenue[i - 1] = _context.Invoices
                    .Where(x =>
                        x.PaymentStatus == "DaThanhToan" &&
                        x.PaymentDate.HasValue &&
                        x.PaymentDate.Value.Year == currentYear &&
                        x.PaymentDate.Value.Month == i)
                    .Sum(x => (decimal?)x.TotalAmount) ?? 0;
            }

            ViewBag.ChartData = JsonSerializer.Serialize(revenue);

            return View();
        }
    }
}
