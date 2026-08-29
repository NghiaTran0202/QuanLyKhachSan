using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using quanlykhachsan.Data;
using quanlykhachsan.Models;
using System.Diagnostics;

namespace quanlykhachsan.Controllers
{
    public class HomeController : Controller
    {
        private readonly HotelDbContext _context;

        public HomeController(HotelDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Booking(int? roomId)
        {
            ViewBag.RoomTypes = await _context.RoomTypes
                .OrderBy(rt => rt.TypeName)
                .ToListAsync();

            if (roomId.HasValue)
            {
                var room = await _context.Rooms
                    .Include(r => r.RoomType)
                    .FirstOrDefaultAsync(r =>
                        r.RoomId == roomId.Value);

                if (room == null)
                {
                    TempData["Error"] =
                        "Không tìm thấy phòng đã chọn.";

                    return RedirectToAction(nameof(Rooms));
                }

                DateTime scheduleStart = DateTime.Now;
                DateTime scheduleEnd = scheduleStart.AddDays(30);

                ViewBag.SelectedRoom = room;
                ViewBag.SelectedRoomBookings = await _context.Bookings
                    .Where(b =>
                        b.RoomId == room.RoomId &&
                        b.Status != "DaTraPhong" &&
                        b.CheckOutDate > scheduleStart &&
                        b.CheckInDate < scheduleEnd)
                    .OrderBy(b => b.CheckInDate)
                    .ToListAsync();

                ViewBag.LockRoom = true;

                var model = new BookingRequestModel
                {
                    RoomId = room.RoomId,
                    RoomTypeId = room.RoomTypeId,
                    BookingType = "Ngay",
                    HoursBooked = 1,
                    NumberOfGuests = 1
                };

                return View(model);
            }

            ViewBag.LockRoom = false;

            return View(new BookingRequestModel
            {
                BookingType = "Ngay",
                HoursBooked = 1,
                NumberOfGuests = 1
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableRooms(
            int roomTypeId,
            string? bookingType,
            DateTime? checkInDate,
            DateTime? checkOutDate,
            DateTime? hourlyCheckInDateTime,
            int hoursBooked = 1)
        {
            DateTime? requestedStart = null;
            DateTime? requestedEnd = null;

            if (bookingType == "Gio" &&
                hourlyCheckInDateTime.HasValue &&
                hoursBooked > 0)
            {
                requestedStart =
                    hourlyCheckInDateTime.Value;

                requestedEnd =
                    requestedStart.Value
                        .AddHours(hoursBooked);
            }
            else if (bookingType == "Ngay" &&
                     checkInDate.HasValue &&
                     checkOutDate.HasValue &&
                     checkOutDate.Value.Date >
                     checkInDate.Value.Date)
            {
                requestedStart =
                    checkInDate.Value.Date
                        .AddHours(14);

                requestedEnd =
                    checkOutDate.Value.Date
                        .AddHours(12);
            }

            var roomsQuery = _context.Rooms
                .Where(r =>
                    r.RoomTypeId == roomTypeId);

            if (requestedStart.HasValue &&
                requestedEnd.HasValue)
            {
                DateTime start =
                    requestedStart.Value;

                DateTime end =
                    requestedEnd.Value;

                roomsQuery =
                    roomsQuery.Where(r =>
                        !_context.Bookings.Any(b =>
                            b.RoomId == r.RoomId &&
                            b.Status != "DaTraPhong" &&
                            b.CheckInDate < end &&
                            b.CheckOutDate > start
                        )
                    );
            }

            var rooms = await roomsQuery
                .OrderBy(r => r.RoomNumber)
                .Select(r => new
                {
                    roomId = r.RoomId,
                    roomNumber = r.RoomNumber,
                    roomName = r.RoomName,
                    price = r.Price,
                    hourlyPrice = r.HourlyPrice
                })
                .ToListAsync();

            return Json(rooms);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Booking(
            BookingRequestModel model,
            bool lockRoom = false)
        {
            ViewBag.RoomTypes = await _context.RoomTypes
                .OrderBy(rt => rt.TypeName)
                .ToListAsync();

            ViewBag.LockRoom = lockRoom;

            if (lockRoom && model.RoomId > 0)
            {
                ViewBag.SelectedRoom =
                    await _context.Rooms
                        .Include(r => r.RoomType)
                        .FirstOrDefaultAsync(r =>
                            r.RoomId == model.RoomId);

                if (ViewBag.SelectedRoom != null)
                {
                    DateTime scheduleStart = DateTime.Now;
                    DateTime scheduleEnd = scheduleStart.AddDays(30);

                    ViewBag.SelectedRoomBookings =
                        await _context.Bookings
                            .Where(b =>
                                b.RoomId == model.RoomId &&
                                b.Status != "DaTraPhong" &&
                                b.CheckOutDate > scheduleStart &&
                                b.CheckInDate < scheduleEnd)
                            .OrderBy(b => b.CheckInDate)
                            .ToListAsync();
                }
            }

            if (model.BookingType != "Ngay" &&
                model.BookingType != "Gio")
            {
                ModelState.AddModelError(
                    nameof(model.BookingType),
                    "Hình thức thuê không hợp lệ."
                );
            }

            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r =>
                    r.RoomId == model.RoomId &&
                    r.RoomTypeId == model.RoomTypeId);

            if (room == null)
            {
                ModelState.AddModelError(
                    nameof(model.RoomId),
                    "Không tìm thấy phòng đã chọn."
                );
            }
            else if (room.RoomType != null &&
                     model.NumberOfGuests > room.RoomType.MaxGuests)
            {
                ModelState.AddModelError(
                    nameof(model.NumberOfGuests),
                    $"Loại phòng {room.RoomType.TypeName} chỉ cho phép tối đa {room.RoomType.MaxGuests} khách."
                );
            }

            DateTime plannedCheckIn =
                DateTime.MinValue;

            DateTime plannedCheckOut =
                DateTime.MinValue;

            decimal roomAmount = 0;

            int hoursBooked = 0;

            if (room != null &&
                model.BookingType == "Gio")
            {
                if (!model.HourlyCheckInDateTime.HasValue)
                {
                    ModelState.AddModelError(
                        nameof(model.HourlyCheckInDateTime),
                        "Vui lòng chọn ngày và giờ nhận phòng."
                    );
                }

                if (model.HoursBooked < 1 ||
                    model.HoursBooked > 24)
                {
                    ModelState.AddModelError(
                        nameof(model.HoursBooked),
                        "Số giờ thuê phải từ 1 đến 24."
                    );
                }

                if (room.HourlyPrice <= 0)
                {
                    ModelState.AddModelError(
                        nameof(model.RoomId),
                        "Phòng này chưa được thiết lập giá giờ đầu."
                    );
                }

                if (model.HourlyCheckInDateTime.HasValue &&
                    model.HoursBooked >= 1 &&
                    model.HoursBooked <= 24)
                {
                    plannedCheckIn =
                        model.HourlyCheckInDateTime.Value;

                    plannedCheckOut =
                        plannedCheckIn
                            .AddHours(model.HoursBooked);

                    hoursBooked =
                        model.HoursBooked;

                    roomAmount =
                        room.HourlyPrice +
                        Math.Max(
                            0,
                            model.HoursBooked - 1
                        ) * 100000m;

                    if (plannedCheckIn <= DateTime.Now)
                    {
                        ModelState.AddModelError(
                            nameof(model.HourlyCheckInDateTime),
                            "Thời gian nhận phòng phải ở tương lai."
                        );
                    }
                }
            }
            else if (room != null &&
                     model.BookingType == "Ngay")
            {
                if (!model.CheckInDate.HasValue)
                {
                    ModelState.AddModelError(
                        nameof(model.CheckInDate),
                        "Vui lòng chọn ngày nhận phòng."
                    );
                }

                if (!model.CheckOutDate.HasValue)
                {
                    ModelState.AddModelError(
                        nameof(model.CheckOutDate),
                        "Vui lòng chọn ngày trả phòng."
                    );
                }

                if (model.CheckInDate.HasValue &&
                    model.CheckOutDate.HasValue)
                {
                    plannedCheckIn =
                        model.CheckInDate.Value.Date
                            .AddHours(14);

                    plannedCheckOut =
                        model.CheckOutDate.Value.Date
                            .AddHours(12);

                    if (model.CheckOutDate.Value.Date <=
                        model.CheckInDate.Value.Date)
                    {
                        ModelState.AddModelError(
                            nameof(model.CheckOutDate),
                            "Ngày trả phòng phải sau ngày nhận phòng."
                        );
                    }
                    else
                    {
                        int nights =
                            (model.CheckOutDate.Value.Date -
                             model.CheckInDate.Value.Date).Days;

                        roomAmount =
                            room.Price * nights;
                    }

                    if (plannedCheckIn <= DateTime.Now)
                    {
                        ModelState.AddModelError(
                            nameof(model.CheckInDate),
                            "Ngày nhận phòng phải ở tương lai."
                        );
                    }
                }
            }

            if (room != null &&
                plannedCheckIn != DateTime.MinValue &&
                plannedCheckOut != DateTime.MinValue &&
                plannedCheckOut > plannedCheckIn)
            {
                bool hasScheduleConflict =
                    await _context.Bookings.AnyAsync(b =>
                        b.RoomId == room.RoomId &&
                        b.Status != "DaTraPhong" &&
                        b.CheckInDate < plannedCheckOut &&
                        b.CheckOutDate > plannedCheckIn
                    );

                if (hasScheduleConflict)
                {
                    ModelState.AddModelError(
                        nameof(model.RoomId),
                        "Phòng này đã có lịch đặt trùng với khoảng thời gian bạn chọn."
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var customer =
                    await _context.Customers
                        .FirstOrDefaultAsync(c =>
                            c.CCCD == model.CCCD);

                if (customer == null)
                {
                    customer = new Customer
                    {
                        FullName = model.FullName,
                        Phone = model.Phone,
                        Email = model.Email,
                        CCCD = model.CCCD,
                        Address = ""
                    };

                    _context.Customers.Add(customer);

                    await _context.SaveChangesAsync();
                }

                Booking booking = new Booking
                {
                    CustomerId = customer.CustomerId,
                    RoomId = room!.RoomId,
                    CheckInDate = plannedCheckIn,
                    ActualCheckInTime = null,
                    CheckOutDate = plannedCheckOut,
                    ActualCheckOutTime = null,
                    NumberOfGuests = model.NumberOfGuests,
                    Status = "Đã đặt",
                    BookingType = model.BookingType,
                    HoursBooked = hoursBooked,
                    TotalAmount = roomAmount
                };

                _context.Bookings.Add(booking);

                await _context.SaveChangesAsync();

                Invoice invoice = new Invoice
                {
                    BookingId = booking.BookingId,
                    InvoiceDate = DateTime.Now,
                    RoomAmount = roomAmount,
                    ServiceAmount = 0,
                    TotalAmount = roomAmount
                };

                _context.Invoices.Add(invoice);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return RedirectToAction(
                    nameof(Success),
                    new
                    {
                        id = booking.BookingId
                    });
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["Error"] =
                    "Không thể tạo đặt phòng. Vui lòng thử lại.";

                return View(model);
            }
        }

        public async Task<IActionResult> Success(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(
                    b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id ??
                    HttpContext.TraceIdentifier
            });
        }

        [HttpGet]
        public async Task<IActionResult> Rooms(
            int? roomTypeId,
            decimal? minPrice,
            decimal? maxPrice,
            string? availability)
        {
            var query = _context.Rooms
                .Include(r => r.RoomType)
                .AsQueryable();

            if (roomTypeId.HasValue)
            {
                query = query.Where(r =>
                    r.RoomTypeId == roomTypeId.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(r =>
                    r.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(r =>
                    r.Price <= maxPrice.Value);
            }

            if (!string.IsNullOrWhiteSpace(availability))
            {
                if (availability == "available")
                {
                    query = query.Where(r =>
                        r.Status == false);
                }
                else if (availability == "occupied")
                {
                    query = query.Where(r =>
                        r.Status == true);
                }
            }

            ViewBag.RoomTypes = await _context.RoomTypes
                .OrderBy(rt => rt.TypeName)
                .ToListAsync();

            ViewBag.SelectedRoomTypeId = roomTypeId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Availability = availability;

            var rooms = await query
                .OrderBy(r => r.RoomNumber)
                .ToListAsync();

            DateTime scheduleStart = DateTime.Now;
            DateTime scheduleEnd = scheduleStart.AddDays(30);

            var roomIds = rooms
                .Select(r => r.RoomId)
                .ToList();

            var bookings = await _context.Bookings
                .Where(b =>
                    roomIds.Contains(b.RoomId) &&
                    b.Status != "DaTraPhong" &&
                    b.CheckOutDate > scheduleStart &&
                    b.CheckInDate < scheduleEnd)
                .OrderBy(b => b.CheckInDate)
                .ToListAsync();

            ViewBag.RoomBookings = bookings
                .GroupBy(b => b.RoomId)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList());

            return View(rooms);
        }

        [HttpGet]
        public async Task<IActionResult> OrderService(int id)
        {
            var service =
                await _context.Services.FindAsync(id);

            if (service == null)
            {
                return NotFound();
            }

            ViewBag.Service = service;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrderService(
            int serviceId,
            string customerName,
            string roomNumber,
            int quantity = 1)
        {
            var service =
                await _context.Services.FindAsync(serviceId);

            if (service == null)
            {
                return NotFound();
            }

            ViewBag.Service = service;

            if (string.IsNullOrWhiteSpace(customerName) ||
                string.IsNullOrWhiteSpace(roomNumber))
            {
                ViewBag.Error =
                    "Vui lòng nhập đầy đủ tên khách hàng và số phòng.";

                return View();
            }

            if (quantity <= 0)
            {
                quantity = 1;
            }

            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .Where(b =>
                    b.Customer != null &&
                    b.Room != null &&
                    b.Status == "Đang thuê" &&
                    b.Customer.FullName == customerName &&
                    b.Room.RoomNumber == roomNumber)
                .OrderByDescending(b => b.BookingId)
                .FirstOrDefaultAsync();

            if (booking == null)
            {
                ViewBag.Error =
                    "Không tìm thấy khách hàng đang ở phòng này.";

                return View();
            }

            decimal serviceTotal =
                service.Price * quantity;

            BookingService bookingService =
                new BookingService
                {
                    BookingId = booking.BookingId,
                    ServiceId = service.ServiceId,
                    Quantity = quantity,
                    TotalPrice = serviceTotal
                };

            _context.BookingServices.Add(bookingService);

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(
                    i => i.BookingId ==
                         booking.BookingId);

            if (invoice == null)
            {
                ViewBag.Error =
                    "Không tìm thấy hóa đơn của phòng này.";

                return View();
            }

            invoice.ServiceAmount +=
                serviceTotal;

            invoice.TotalAmount =
                invoice.RoomAmount +
                invoice.ServiceAmount;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(ServiceSuccess),
                new
                {
                    id =
                        bookingService
                            .BookingServiceId
                });
        }

        public async Task<IActionResult> ServiceSuccess(int id)
        {
            var order =
                await _context.BookingServices
                    .Include(x => x.Service)
                    .Include(x => x.Booking)
                        .ThenInclude(x => x.Customer)
                    .Include(x => x.Booking)
                        .ThenInclude(x => x.Room)
                    .FirstOrDefaultAsync(
                        x =>
                            x.BookingServiceId == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        public async Task<IActionResult> Services()
        {
            var services =
                await _context.Services.ToListAsync();

            return View(services);
        }
    }
}