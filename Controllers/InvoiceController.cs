using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using quanlykhachsan.Data;
using quanlykhachsan.Models;
using System.Collections.Generic;
using System.Linq;

namespace quanlykhachsan.Controllers
{
    [Authorize(Roles = "Admin")]
    public class InvoiceController : Controller
    {
        private readonly HotelDbContext _context;

        public InvoiceController(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var invoices = await _context.Invoices
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Customer)
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Room)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            return View(invoices);
        }

        public IActionResult Create()
        {
            ViewBag.BookingId = new SelectList(
                _context.Bookings,
                "BookingId",
                "BookingId");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Invoice invoice)
        {
            if (ModelState.IsValid)
            {
                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.BookingId = new SelectList(
                _context.Bookings,
                "BookingId",
                "BookingId",
                invoice.BookingId);

            return View(invoice);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var invoice = await _context.Invoices.FindAsync(id);

            if (invoice == null)
                return NotFound();

            if (invoice.PaymentStatus == "DaThanhToan")
            {
                TempData["Error"] =
                    "Hóa đơn đã thanh toán không thể chỉnh sửa.";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.BookingId = new SelectList(
                _context.Bookings,
                "BookingId",
                "BookingId",
                invoice.BookingId);

            return View(invoice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Invoice invoice)
        {
            if (id != invoice.InvoiceId)
                return NotFound();

            var existingInvoice = await _context.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (existingInvoice == null)
                return NotFound();

            if (existingInvoice.PaymentStatus == "DaThanhToan")
            {
                TempData["Error"] =
                    "Hóa đơn đã thanh toán không thể chỉnh sửa.";

                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                invoice.PaymentStatus = existingInvoice.PaymentStatus;
                invoice.PaymentDate = existingInvoice.PaymentDate;

                _context.Update(invoice);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.BookingId = new SelectList(
                _context.Bookings,
                "BookingId",
                "BookingId",
                invoice.BookingId);

            return View(invoice);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.Booking)
                .FirstOrDefaultAsync(
                    i => i.InvoiceId == id);

            if (invoice == null)
                return NotFound();

            if (invoice.PaymentStatus == "DaThanhToan")
            {
                TempData["Error"] =
                    "Hóa đơn đã thanh toán không thể xóa.";

                return RedirectToAction(nameof(Index));
            }

            return View(invoice);
        }

        public async Task<IActionResult> Search(
            string keyword)
        {
            keyword = keyword?.Trim() ?? "";

            var invoices = await _context.Invoices
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Customer)
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Room)
                .Where(i =>
                    string.IsNullOrEmpty(keyword) ||
                    i.Booking.Customer.FullName.Contains(keyword) ||
                    i.Booking.Room.RoomNumber.Contains(keyword))
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            ViewBag.Keyword = keyword;

            return View("Index", invoices);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Booking)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null)
                return NotFound();

            if (invoice.PaymentStatus == "DaThanhToan")
            {
                TempData["Error"] = "Hóa đơn này đã được thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            if (invoice.Booking == null ||
                invoice.Booking.Status != "DaTraPhong")
            {
                TempData["Error"] =
                    "Chỉ có thể thanh toán sau khi khách đã trả phòng.";

                return RedirectToAction(nameof(Index));
            }

            invoice.PaymentStatus = "DaThanhToan";
            invoice.PaymentDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xác nhận thanh toán hóa đơn.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Customer)
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Room)
                .Include(i => i.Booking)
                    .ThenInclude(
                        b => b.RoomChangeHistories)
                .FirstOrDefaultAsync(
                    i => i.InvoiceId == id);

            if (invoice == null)
                return NotFound();

            var services = await _context.BookingServices
                .Include(x => x.Service)
                .Where(
                    x => x.BookingId ==
                         invoice.BookingId)
                .ToListAsync();

            ViewBag.Services = services;

            decimal baseRoomAmount =
                invoice.RoomAmount;

            decimal lateCheckoutFee = 0;

            if (invoice.Booking != null &&
                invoice.Booking.BookingType != "Gio")
            {
                var roomChanges =
                    invoice.Booking
                        .RoomChangeHistories
                        .OrderBy(h => h.ChangedAt)
                        .ThenBy(
                            h =>
                            h.RoomChangeHistoryId)
                        .ToList();

                baseRoomAmount =
                    CalculateDailyRoomAmount(
                        invoice.Booking,
                        roomChanges);

                if (invoice.Booking
                        .ActualCheckOutTime != null)
                {
                    lateCheckoutFee =
                        invoice.RoomAmount -
                        baseRoomAmount;

                    if (lateCheckoutFee < 0)
                        lateCheckoutFee = 0;
                }
            }

            ViewBag.BaseRoomAmount =
                baseRoomAmount;

            ViewBag.LateCheckoutFee =
                lateCheckoutFee;

            return View(invoice);
        }

        public async Task<IActionResult> ServiceHistory()
        {
            var data = await _context.BookingServices
                .Include(x => x.Booking)
                    .ThenInclude(x => x.Customer)
                .Include(x => x.Booking)
                    .ThenInclude(x => x.Room)
                .Include(x => x.Service)
                .OrderByDescending(
                    x => x.BookingServiceId)
                .ToListAsync();

            return View(data);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var invoice =
                await _context.Invoices.FindAsync(id);

            if (invoice == null)
                return RedirectToAction(nameof(Index));

            if (invoice.PaymentStatus == "DaThanhToan")
            {
                TempData["Error"] =
                    "Hóa đơn đã thanh toán không thể xóa.";

                return RedirectToAction(nameof(Index));
            }

            _context.Invoices.Remove(invoice);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private static decimal CalculateDailyRoomAmount(
            Booking booking,
            List<RoomChangeHistory> roomChanges)
        {
            int totalDays =
                (booking.CheckOutDate.Date -
                 booking.CheckInDate.Date).Days;

            if (totalDays <= 0)
                totalDays = 1;

            var correction =
                roomChanges.FirstOrDefault(
                    h => h.PriceMode ==
                         "CorrectWrongRoom");

            decimal initialPrice;

            if (correction != null)
            {
                initialPrice =
                    GetAppliedPrice(correction);
            }
            else if (roomChanges.Count > 0)
            {
                initialPrice =
                    roomChanges[0].OldRoomPrice;
            }
            else
            {
                initialPrice =
                    booking.Room?.Price ?? 0;
            }

            decimal totalAmount = 0;

            for (int i = 0;
                 i < totalDays;
                 i++)
            {
                DateTime currentDate =
                    booking.CheckInDate
                        .Date
                        .AddDays(i);

                decimal currentPrice =
                    initialPrice;

                foreach (var roomChange
                         in roomChanges)
                {
                    if (roomChange.PriceMode ==
                        "CorrectWrongRoom")
                    {
                        continue;
                    }

                    if (roomChange
                            .ChangedAt.Date <=
                        currentDate)
                    {
                        currentPrice =
                            GetAppliedPrice(
                                roomChange);
                    }
                    else
                    {
                        break;
                    }
                }

                totalAmount += currentPrice;
            }

            return totalAmount;
        }

        private static decimal GetAppliedPrice(
            RoomChangeHistory roomChange)
        {
            if (roomChange.AppliedPrice > 0)
                return roomChange.AppliedPrice;

            return roomChange.NewRoomPrice;
        }
    }
}
