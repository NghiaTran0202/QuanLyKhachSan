using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using quanlykhachsan.Data;
using quanlykhachsan.Models;

namespace quanlykhachsan.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BookingController : Controller
    {
        private readonly HotelDbContext _context;

        public BookingController(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .Where(b => b.Status != "DaTraPhong")
                .OrderByDescending(b => b.CheckInDate)
                .ToListAsync();

            return View(bookings);
        }

        public IActionResult Create()
        {
            ViewBag.Customers = _context.Customers
                .OrderBy(c => c.FullName)
                .ToList();

            ViewBag.Rooms = _context.Rooms
                .OrderBy(r => r.RoomNumber)
                .ToList();

            var model = new BookingCreateViewModel
            {
                BookingType = "Ngay",
                HoursBooked = 1,
                NumberOfGuests = 1,
                CheckInDate = DateTime.Today,
                CheckOutDate = DateTime.Today.AddDays(1)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingCreateViewModel model)
        {
            if (model.IsNewCustomer)
            {
                if (string.IsNullOrWhiteSpace(model.FullName))
                {
                    ModelState.AddModelError(
                        nameof(model.FullName),
                        "Vui lòng nhập họ tên khách hàng."
                    );
                }

                if (string.IsNullOrWhiteSpace(model.Phone))
                {
                    ModelState.AddModelError(
                        nameof(model.Phone),
                        "Vui lòng nhập số điện thoại."
                    );
                }

                if (string.IsNullOrWhiteSpace(model.CCCD))
                {
                    ModelState.AddModelError(
                        nameof(model.CCCD),
                        "Vui lòng nhập CCCD."
                    );
                }
            }
            else
            {
                if (!model.CustomerId.HasValue ||
                    model.CustomerId.Value <= 0)
                {
                    ModelState.AddModelError(
                        nameof(model.CustomerId),
                        "Vui lòng chọn khách hàng."
                    );
                }
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.RoomId == model.RoomId);

            if (room == null)
            {
                ModelState.AddModelError(
                    nameof(model.RoomId),
                    "Không tìm thấy phòng."
                );
            }
            else if (model.CheckInNow &&
                     room.Status)
            {
                ModelState.AddModelError(
                    nameof(model.RoomId),
                    "Phòng này hiện đang có khách sử dụng."
                );
            }

            if (model.BookingType == "Gio")
            {
                if (model.HoursBooked <= 0)
                {
                    ModelState.AddModelError(
                        nameof(model.HoursBooked),
                        "Số giờ thuê phải lớn hơn 0."
                    );
                }

                if (room != null &&
                    room.HourlyPrice <= 0)
                {
                    ModelState.AddModelError(
                        nameof(model.RoomId),
                        "Phòng này chưa được thiết lập giá giờ đầu."
                    );
                }

                if (!model.CheckInNow)
                {
                    if (!model.HourlyCheckInDateTime.HasValue)
                    {
                        ModelState.AddModelError(
                            nameof(model.HourlyCheckInDateTime),
                            "Vui lòng chọn ngày và giờ nhận phòng dự kiến."
                        );
                    }
                    else if (model.HourlyCheckInDateTime.Value <=
                             DateTime.Now)
                    {
                        ModelState.AddModelError(
                            nameof(model.HourlyCheckInDateTime),
                            "Thời gian nhận phòng đặt trước phải ở tương lai."
                        );
                    }
                }
            }
            else
            {
                if (model.CheckOutDate.Date <=
                    model.CheckInDate.Date)
                {
                    ModelState.AddModelError(
                        nameof(model.CheckOutDate),
                        "Ngày trả phòng phải sau ngày nhận phòng."
                    );
                }

                if (!model.CheckInNow)
                {
                    DateTime plannedCheckIn =
                        model.CheckInDate.Date.AddHours(14);

                    if (plannedCheckIn <= DateTime.Now)
                    {
                        ModelState.AddModelError(
                            nameof(model.CheckInDate),
                            "Thời gian nhận phòng đặt trước phải ở tương lai."
                        );
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Customers = _context.Customers
                    .OrderBy(c => c.FullName)
                    .ToList();

                ViewBag.Rooms = _context.Rooms
                    .OrderBy(r => r.RoomNumber)
                    .ToList();

                return View(model);
            }

            Customer customer;

            if (model.IsNewCustomer)
            {
                var existingCustomer =
                    await _context.Customers
                        .FirstOrDefaultAsync(c =>
                            c.CCCD == model.CCCD);

                if (existingCustomer != null)
                {
                    ModelState.AddModelError(
                        nameof(model.CCCD),
                        "CCCD này đã thuộc về một khách hàng có trong hệ thống."
                    );

                    ViewBag.Customers = _context.Customers
                        .OrderBy(c => c.FullName)
                        .ToList();

                    ViewBag.Rooms = _context.Rooms
                        .Where(r => !r.Status ||
                                    r.RoomId == model.RoomId)
                        .OrderBy(r => r.RoomNumber)
                        .ToList();

                    return View(model);
                }

                customer = new Customer
                {
                    FullName = model.FullName!.Trim(),
                    Phone = model.Phone!.Trim(),
                    Email = model.Email?.Trim(),
                    CCCD = model.CCCD!.Trim(),
                    Address = ""
                };

                _context.Customers.Add(customer);

                await _context.SaveChangesAsync();
            }
            else
            {
                customer = await _context.Customers
                    .FirstOrDefaultAsync(c =>
                        c.CustomerId == model.CustomerId!.Value);

                if (customer == null)
                {
                    ModelState.AddModelError(
                        nameof(model.CustomerId),
                        "Không tìm thấy khách hàng."
                    );

                    ViewBag.Customers = _context.Customers
                        .OrderBy(c => c.FullName)
                        .ToList();

                    ViewBag.Rooms = _context.Rooms
                        .Where(r => !r.Status ||
                                    r.RoomId == model.RoomId)
                        .OrderBy(r => r.RoomNumber)
                        .ToList();

                    return View(model);
                }
            }

            var booking = new Booking
            {
                CustomerId = customer.CustomerId,
                RoomId = model.RoomId,
                BookingType = model.BookingType,
                HoursBooked = model.BookingType == "Gio"
        ? model.HoursBooked
        : 0,
                NumberOfGuests = model.NumberOfGuests
            };

            if (model.BookingType == "Gio")
            {
                if (model.CheckInNow)
                {
                    booking.CheckInDate = DateTime.Now;
                    booking.ActualCheckInTime = booking.CheckInDate;
                    booking.Status = "Đang thuê";

                    booking.CheckOutDate =
                        booking.CheckInDate.AddHours(
                            model.HoursBooked
                        );
                }
                else
                {
                    if (!model.HourlyCheckInDateTime.HasValue)
                    {
                        ModelState.AddModelError(
                            nameof(model.HourlyCheckInDateTime),
                            "Vui lòng chọn ngày và giờ nhận phòng dự kiến."
                        );

                        ViewBag.Customers = _context.Customers
                            .OrderBy(c => c.FullName)
                            .ToList();

                        ViewBag.Rooms = _context.Rooms
                            .Where(r => !r.Status ||
                                        r.RoomId == model.RoomId)
                            .OrderBy(r => r.RoomNumber)
                            .ToList();

                        return View(model);
                    }

                    booking.CheckInDate =
                        model.HourlyCheckInDateTime.Value;

                    booking.CheckOutDate =
                        booking.CheckInDate.AddHours(
                            model.HoursBooked
                        );

                    booking.ActualCheckInTime = null;
                    booking.Status = "Đã đặt";
                }

                booking.TotalAmount =
                    CalculateHourlyPrice(
                        room!.HourlyPrice,
                        model.HoursBooked
                    );
            }
            else
            {
                booking.CheckInDate =
                    model.CheckInDate.Date.AddHours(14);

                booking.CheckOutDate =
                    model.CheckOutDate.Date.AddHours(12);

                int days =
                    (booking.CheckOutDate.Date -
                     booking.CheckInDate.Date).Days;

                if (days <= 0)
                    days = 1;

                booking.TotalAmount =
                    room!.Price * days;

                if (model.CheckInNow)
                {
                    booking.ActualCheckInTime = DateTime.Now;
                    booking.Status = "Đang thuê";
                }
                else
                {
                    booking.ActualCheckInTime = null;
                    booking.Status = "Đã đặt";
                }
            }

            var hasScheduleConflict = await _context.Bookings
                .AnyAsync(b =>
                    b.RoomId == model.RoomId &&
                    b.Status != "DaTraPhong" &&
                    b.CheckInDate < booking.CheckOutDate &&
                    b.CheckOutDate > booking.CheckInDate
                );

            if (hasScheduleConflict)
            {
                ModelState.AddModelError(
                    nameof(model.RoomId),
                    "Phòng này đã có lịch đặt trùng với khoảng thời gian bạn chọn."
                );

                ViewBag.Customers = _context.Customers
                    .OrderBy(c => c.FullName)
                    .ToList();

                ViewBag.Rooms = _context.Rooms
                    .OrderBy(r => r.RoomNumber)
                    .ToList();

                return View(model);
            }

            if (model.CheckInNow)
            {
                room!.Status = true;

                _context.Rooms.Update(room);
            }

            _context.Bookings.Add(booking);

            await _context.SaveChangesAsync();
            var invoice = new Invoice
            {
                BookingId = booking.BookingId,
                InvoiceDate = DateTime.Now,
                RoomAmount = booking.TotalAmount,
                ServiceAmount = 0,
                TotalAmount = booking.TotalAmount
            };

            _context.Invoices.Add(invoice);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
                return NotFound();

            if (booking.Status == "DaTraPhong")
            {
                TempData["Error"] = "Booking đã trả phòng nên không thể chỉnh sửa.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Customers = _context.Customers
                .OrderBy(c => c.FullName)
                .ToList();

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            int CustomerId,
            DateTime CheckInDate,
            DateTime CheckOutDate,
            int NumberOfGuests,
            int HoursBooked)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
                return NotFound();

            if (booking.Status == "DaTraPhong")
            {
                TempData["Error"] = "Booking đã trả phòng nên không thể chỉnh sửa.";
                return RedirectToAction(nameof(Index));
            }

            if (NumberOfGuests < 1 || NumberOfGuests > 20)
            {
                ModelState.AddModelError(
                    nameof(NumberOfGuests),
                    "Số người phải từ 1 đến 20."
                );
            }

            if (booking.Room == null)
            {
                ModelState.AddModelError(
                    "",
                    "Không tìm thấy phòng của booking."
                );
            }

            DateTime newCheckInDate;
            DateTime newCheckOutDate;
            decimal newTotalAmount = booking.TotalAmount;

            if (booking.BookingType == "Gio")
            {
                if (HoursBooked <= 0)
                {
                    ModelState.AddModelError(
                        nameof(HoursBooked),
                        "Số giờ thuê phải lớn hơn 0."
                    );
                }

                newCheckInDate = CheckInDate;
                newCheckOutDate = newCheckInDate.AddHours(HoursBooked);

                if (booking.Room != null)
                {
                    if (booking.Room.HourlyPrice <= 0)
                    {
                        ModelState.AddModelError(
                            "",
                            "Phòng này chưa được thiết lập giá giờ đầu."
                        );
                    }
                    else if (HoursBooked > 0)
                    {
                        newTotalAmount = CalculateHourlyPrice(
                            booking.Room.HourlyPrice,
                            HoursBooked
                        );
                    }
                }
            }
            else
            {
                newCheckInDate = CheckInDate.Date.AddHours(14);
                newCheckOutDate = CheckOutDate.Date.AddHours(12);

                if (newCheckOutDate <= newCheckInDate)
                {
                    ModelState.AddModelError(
                        nameof(CheckOutDate),
                        "Ngày trả phòng phải sau ngày nhận phòng."
                    );
                }

                if (booking.Room != null &&
                    newCheckOutDate > newCheckInDate)
                {
                    int days =
                        (newCheckOutDate.Date -
                         newCheckInDate.Date).Days;

                    if (days <= 0)
                        days = 1;

                    newTotalAmount =
                        booking.Room.Price * days;
                }
            }

            var hasScheduleConflict = await _context.Bookings
                .AnyAsync(b =>
                    b.BookingId != booking.BookingId &&
                    b.RoomId == booking.RoomId &&
                    b.Status != "DaTraPhong" &&
                    b.CheckInDate < newCheckOutDate &&
                    b.CheckOutDate > newCheckInDate
                );

            if (hasScheduleConflict)
            {
                ModelState.AddModelError(
                    "",
                    "Phòng này đã có lịch đặt trùng với khoảng thời gian bạn chọn."
                );
            }

            if (!ModelState.IsValid)
            {
                booking.CustomerId = CustomerId;
                booking.CheckInDate = CheckInDate;
                booking.CheckOutDate = CheckOutDate;
                booking.NumberOfGuests = NumberOfGuests;
                booking.HoursBooked = HoursBooked;

                ViewBag.Customers = _context.Customers
                    .OrderBy(c => c.FullName)
                    .ToList();

                return View(booking);
            }

            booking.CustomerId = CustomerId;
            booking.CheckInDate = newCheckInDate;
            booking.CheckOutDate = newCheckOutDate;
            booking.NumberOfGuests = NumberOfGuests;
            booking.HoursBooked =
                booking.BookingType == "Gio"
                    ? HoursBooked
                    : 0;
            booking.TotalAmount = newTotalAmount;

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(
                    x => x.BookingId == booking.BookingId
                );

            if (invoice != null)
            {
                invoice.RoomAmount = booking.TotalAmount;
                invoice.TotalAmount =
                    invoice.RoomAmount +
                    invoice.ServiceAmount;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật booking thành công.";

            return RedirectToAction(nameof(Details), new { id = booking.BookingId });
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .Include(b => b.RoomChangeHistories)
                    .ThenInclude(h => h.OldRoom)
                .Include(b => b.RoomChangeHistories)
                    .ThenInclude(h => h.NewRoom)
                .FirstOrDefaultAsync(
                    b => b.BookingId == id
                );

            if (booking == null)
                return NotFound();

            return View(booking);
        }

        public async Task<IActionResult> ChangeRoom(int? id)
        {
            if (id == null)
                return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(
                    b => b.BookingId == id
                );

            if (booking == null)
                return NotFound();

            if (booking.Status == "DaTraPhong")
            {
                TempData["Error"] =
                    "Booking này đã trả phòng, không thể đổi phòng.";

                return RedirectToAction(nameof(Index));
            }

            if (booking.Status != "Đang thuê")
            {
                TempData["Error"] =
                    "Chỉ booking đang thuê mới có thể đổi phòng.";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.AvailableRooms =
                await _context.Rooms
                    .Where(r =>
                        !r.Status &&
                        r.RoomId != booking.RoomId)
                    .OrderBy(r => r.RoomNumber)
                    .ToListAsync();

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRoom(
            int bookingId,
            int newRoomId,
            string priceMode,
            string? reason)
        {
            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var booking =
                    await _context.Bookings
                        .Include(b => b.Room)
                        .FirstOrDefaultAsync(
                            b => b.BookingId ==
                                 bookingId
                        );

                if (booking == null)
                    return NotFound();

                if (booking.Status == "DaTraPhong")
                {
                    TempData["Error"] =
                        "Booking này đã trả phòng, không thể đổi phòng.";

                    return RedirectToAction(nameof(Index));
                }

                if (booking.Status != "Đang thuê")
                {
                    TempData["Error"] =
                        "Chỉ booking đang thuê mới có thể đổi phòng.";

                    return RedirectToAction(nameof(Index));
                }

                if (booking.RoomId == newRoomId)
                {
                    TempData["Error"] =
                        "Phòng mới phải khác phòng hiện tại.";

                    return RedirectToAction(
                        nameof(ChangeRoom),
                        new { id = bookingId }
                    );
                }

                if (priceMode != "KeepCurrent" &&
                    priceMode != "UseNewRoomPrice" &&
                    priceMode != "CorrectWrongRoom")
                {
                    TempData["Error"] =
                        "Cách áp dụng giá không hợp lệ.";

                    return RedirectToAction(
                        nameof(ChangeRoom),
                        new { id = bookingId }
                    );
                }

                var oldRoom =
                    await _context.Rooms
                        .FirstOrDefaultAsync(
                            r => r.RoomId ==
                                 booking.RoomId
                        );

                var newRoom =
                    await _context.Rooms
                        .FirstOrDefaultAsync(
                            r => r.RoomId ==
                                 newRoomId
                        );

                if (oldRoom == null ||
                    newRoom == null)
                {
                    TempData["Error"] =
                        "Không tìm thấy phòng.";

                    return RedirectToAction(
                        nameof(ChangeRoom),
                        new { id = bookingId }
                    );
                }

                if (newRoom.Status)
                {
                    TempData["Error"] =
                        $"Phòng {newRoom.RoomNumber} hiện đang được sử dụng.";

                    return RedirectToAction(
                        nameof(ChangeRoom),
                        new { id = bookingId }
                    );
                }

                if (booking.BookingType == "Gio" &&
                    newRoom.HourlyPrice <= 0)
                {
                    TempData["Error"] =
                        $"Phòng {newRoom.RoomNumber} chưa được thiết lập giá giờ đầu.";

                    return RedirectToAction(
                        nameof(ChangeRoom),
                        new { id = bookingId }
                    );
                }

                var existingRoomChanges =
                    await _context
                        .RoomChangeHistories
                        .Where(
                            h => h.BookingId ==
                                 booking.BookingId
                        )
                        .OrderBy(h => h.ChangedAt)
                        .ThenBy(
                            h => h.RoomChangeHistoryId
                        )
                        .ToListAsync();

                if (priceMode ==
                        "CorrectWrongRoom" &&
                    existingRoomChanges.Count > 0)
                {
                    TempData["Error"] =
                        "Chỉ có thể dùng 'Sửa phòng đặt nhầm' khi booking chưa từng đổi phòng.";

                    return RedirectToAction(
                        nameof(ChangeRoom),
                        new { id = bookingId }
                    );
                }

                decimal oldRoomPrice;
                decimal newRoomPrice;

                if (booking.BookingType == "Gio")
                {
                    oldRoomPrice =
                        oldRoom.HourlyPrice;

                    newRoomPrice =
                        newRoom.HourlyPrice;
                }
                else
                {
                    oldRoomPrice =
                        oldRoom.Price;

                    newRoomPrice =
                        newRoom.Price;
                }

                var lastRoomChange =
                    existingRoomChanges
                        .LastOrDefault();

                decimal currentAppliedPrice;

                if (lastRoomChange != null)
                {
                    currentAppliedPrice =
                        GetAppliedPrice(
                            lastRoomChange
                        );
                }
                else
                {
                    currentAppliedPrice =
                        oldRoomPrice;
                }

                decimal appliedPrice;

                if (priceMode ==
                        "UseNewRoomPrice" ||
                    priceMode ==
                        "CorrectWrongRoom")
                {
                    appliedPrice =
                        newRoomPrice;
                }
                else
                {
                    appliedPrice =
                        currentAppliedPrice;
                }

                oldRoom.Status = false;
                newRoom.Status = true;

                booking.RoomId =
                    newRoom.RoomId;

                var history =
                    new RoomChangeHistory
                    {
                        BookingId =
                            booking.BookingId,

                        OldRoomId =
                            oldRoom.RoomId,

                        NewRoomId =
                            newRoom.RoomId,

                        OldRoomPrice =
                            oldRoomPrice,

                        NewRoomPrice =
                            newRoomPrice,

                        PriceMode =
                            priceMode,

                        AppliedPrice =
                            appliedPrice,

                        ChangedAt =
                            DateTime.Now,

                        Reason =
                            string.IsNullOrWhiteSpace(
                                reason
                            )
                                ? "Không nhập lý do"
                                : reason.Trim()
                    };

                _context
                    .RoomChangeHistories
                    .Add(history);

                if (priceMode ==
                    "CorrectWrongRoom")
                {
                    if (booking.BookingType ==
                        "Gio")
                    {
                        int hours =
                            booking.HoursBooked;

                        if (hours <= 0)
                            hours = 1;

                        booking.TotalAmount =
                            CalculateHourlyPrice(
                                appliedPrice,
                                hours
                            );
                    }
                    else
                    {
                        int days =
                            (booking.CheckOutDate.Date -
                             booking.CheckInDate.Date)
                            .Days;

                        if (days <= 0)
                            days = 1;

                        booking.TotalAmount =
                            appliedPrice * days;
                    }

                    var invoice =
                        await _context.Invoices
                            .FirstOrDefaultAsync(
                                i => i.BookingId ==
                                     booking.BookingId
                            );

                    if (invoice != null)
                    {
                        invoice.RoomAmount =
                            booking.TotalAmount;

                        invoice.TotalAmount =
                            invoice.RoomAmount +
                            invoice.ServiceAmount;
                    }
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                string priceMessage;

                if (priceMode ==
                    "CorrectWrongRoom")
                {
                    priceMessage =
                        $"Đã sửa phòng đặt nhầm. Giá được tính lại từ đầu theo phòng mới: {appliedPrice:N0} VNĐ.";
                }
                else if (priceMode ==
                         "UseNewRoomPrice")
                {
                    priceMessage =
                        $"Áp dụng giá phòng mới {appliedPrice:N0} VNĐ.";
                }
                else
                {
                    priceMessage =
                        $"Giữ giá hiện tại {appliedPrice:N0} VNĐ.";
                }

                TempData["Success"] =
                    $"Đã đổi phòng {oldRoom.RoomNumber} sang phòng {newRoom.RoomNumber}. {priceMessage}";

                return RedirectToAction(
                    nameof(Index)
                );
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["Error"] =
                    "Có lỗi xảy ra trong quá trình đổi phòng.";

                return RedirectToAction(
                    nameof(Index)
                );
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var booking =
                await _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Room)
                    .FirstOrDefaultAsync(
                        b => b.BookingId == id
                    );

            if (booking == null)
                return NotFound();

            return View(booking);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var booking =
                await _context.Bookings
                    .FirstOrDefaultAsync(
                        b => b.BookingId == id
                    );

            if (booking == null)
            {
                return RedirectToAction(
                    nameof(Index)
                );
            }

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                if (booking.Status == "Đang thuê")
                {
                    var room =
                        await _context.Rooms
                            .FindAsync(
                                booking.RoomId
                            );

                    if (room != null)
                    {
                        room.Status = false;
                    }
                }

                var roomChangeHistories =
                    await _context
                        .RoomChangeHistories
                        .Where(
                            x => x.BookingId ==
                                 booking.BookingId
                        )
                        .ToListAsync();

                if (roomChangeHistories.Any())
                {
                    _context
                        .RoomChangeHistories
                        .RemoveRange(
                            roomChangeHistories
                        );
                }

                var bookingServices =
                    await _context
                        .BookingServices
                        .Where(
                            x => x.BookingId ==
                                 booking.BookingId
                        )
                        .ToListAsync();

                if (bookingServices.Any())
                {
                    _context
                        .BookingServices
                        .RemoveRange(
                            bookingServices
                        );
                }

                var invoice =
                    await _context.Invoices
                        .FirstOrDefaultAsync(
                            i => i.BookingId ==
                                 booking.BookingId
                        );

                if (invoice != null)
                {
                    _context.Invoices
                        .Remove(invoice);
                }

                _context.Bookings
                    .Remove(booking);

                await _context
                    .SaveChangesAsync();

                await transaction
                    .CommitAsync();

                TempData["Success"] =
                    "Đã xóa booking thành công.";
            }
            catch
            {
                await transaction
                    .RollbackAsync();

                TempData["Error"] =
                    "Không thể xóa booking vì dữ liệu liên quan chưa được xử lý đầy đủ.";
            }

            return RedirectToAction(
                nameof(Index)
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status == "DaTraPhong")
            {
                TempData["Error"] =
                    "Booking này đã trả phòng, không thể nhận phòng.";

                return RedirectToAction(nameof(Index));
            }

            if (booking.Status == "Đang thuê")
            {
                TempData["Error"] =
                    "Booking này đã nhận phòng.";

                return RedirectToAction(nameof(Index));
            }

            if (booking.Status != "Đã đặt")
            {
                TempData["Error"] =
                    "Trạng thái booking không hợp lệ để nhận phòng.";

                return RedirectToAction(nameof(Index));
            }

            if (booking.Room == null)
            {
                TempData["Error"] =
                    "Không tìm thấy phòng của booking.";

                return RedirectToAction(nameof(Index));
            }

            if (booking.Room.Status)
            {
                TempData["Error"] =
                    $"Phòng {booking.Room.RoomNumber} hiện đang được sử dụng.";

                return RedirectToAction(nameof(Index));
            }

            DateTime now = DateTime.Now;

            if (now < booking.CheckInDate)
            {
                TempData["Error"] =
                    $"Chưa đến thời gian nhận phòng. Thời gian nhận phòng dự kiến là {booking.CheckInDate:dd/MM/yyyy HH:mm}.";

                return RedirectToAction(nameof(Index));
            }

            booking.ActualCheckInTime = now;
            booking.Status = "Đang thuê";
            booking.Room.Status = true;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Đã nhận phòng {booking.Room.RoomNumber} thành công.";

            return RedirectToAction(
                nameof(Details),
                new { id = booking.BookingId }
            );
        }
        public async Task<IActionResult> CheckOut(int id)
        {
            var booking =
                await _context.Bookings
                    .Include(x => x.Room)
                    .Include(
                        x => x.RoomChangeHistories
                    )
                    .FirstOrDefaultAsync(
                        x => x.BookingId == id
                    );

            if (booking == null)
                return NotFound();

            if (booking.Status ==
                "DaTraPhong")
            {
                TempData["Error"] =
                    "Booking này đã trả phòng.";

                return RedirectToAction(
                    nameof(Index)
                );
            }

            if (booking.Status !=
                "Đang thuê")
            {
                TempData["Error"] =
                    "Chỉ booking đang thuê mới có thể trả phòng.";

                return RedirectToAction(
                    nameof(Index)
                );
            }

            booking.ActualCheckOutTime =
                DateTime.Now;

            var roomChanges =
                booking.RoomChangeHistories
                    .OrderBy(h => h.ChangedAt)
                    .ThenBy(
                        h => h.RoomChangeHistoryId
                    )
                    .ToList();

            decimal tongTienPhong;

            if (booking.BookingType == "Gio")
            {
                DateTime actualCheckOut =
                    booking.ActualCheckOutTime.Value;

                DateTime graceLimit =
                    booking.CheckOutDate.AddMinutes(15);

                DateTime billingEnd;

                if (actualCheckOut <= graceLimit)
                {
                    billingEnd =
                        booking.CheckOutDate;
                }
                else
                {
                    billingEnd =
                        actualCheckOut;
                }

                tongTienPhong =
                    CalculateHourlyRoomAmount(
                        booking,
                        roomChanges,
                        billingEnd
                    );
            }
            else
            {
                tongTienPhong =
                    CalculateDailyRoomAmount(
                        booking,
                        roomChanges
                    );

                decimal lateCheckoutFee =
                    CalculateLateCheckoutFee(
                        booking,
                        roomChanges,
                        booking
                            .ActualCheckOutTime
                            .Value
                    );

                tongTienPhong +=
                    lateCheckoutFee;
            }

            booking.TotalAmount =
                tongTienPhong;

            var invoice =
                await _context.Invoices
                    .FirstOrDefaultAsync(
                        i => i.BookingId ==
                             booking.BookingId
                    );

            if (invoice != null)
            {
                invoice.RoomAmount =
                    tongTienPhong;

                invoice.TotalAmount =
                    invoice.RoomAmount +
                    invoice.ServiceAmount;
            }

            if (booking.Room != null)
            {
                booking.Room.Status =
                    false;
            }

            booking.Status =
                "DaTraPhong";

            await _context.SaveChangesAsync();

            if (invoice == null)
            {
                return RedirectToAction(
                    nameof(Index)
                );
            }

            return RedirectToAction(
                "Details",
                "Invoice",
                new
                {
                    id = invoice.InvoiceId
                }
            );
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
                         "CorrectWrongRoom"
                );

            decimal initialPrice;

            if (correction != null)
            {
                initialPrice =
                    GetAppliedPrice(
                        correction
                    );
            }
            else if (roomChanges.Count > 0)
            {
                initialPrice =
                    roomChanges[0]
                        .OldRoomPrice;
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

                foreach (
                    var roomChange
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
                                roomChange
                            );
                    }
                    else
                    {
                        break;
                    }
                }

                totalAmount +=
                    currentPrice;
            }

            return totalAmount;
        }

        private static decimal CalculateLateCheckoutFee(
            Booking booking,
            List<RoomChangeHistory> roomChanges,
            DateTime actualCheckOutTime)
        {
            if (actualCheckOutTime <=
                booking.CheckOutDate)
            {
                return 0;
            }

            double lateHours =
                (actualCheckOutTime -
                 booking.CheckOutDate)
                .TotalHours;

            if (lateHours <= 1)
                return 0;

            decimal currentPrice =
                GetCurrentAppliedPrice(
                    booking,
                    roomChanges,
                    booking.CheckOutDate
                );

            if (lateHours <= 3)
            {
                return currentPrice *
                       0.30m;
            }

            if (lateHours <= 6)
            {
                return currentPrice *
                       0.50m;
            }

            return currentPrice;
        }

        private static decimal GetCurrentAppliedPrice(
            Booking booking,
            List<RoomChangeHistory> roomChanges,
            DateTime time)
        {
            var correction =
                roomChanges.FirstOrDefault(
                    h => h.PriceMode ==
                         "CorrectWrongRoom"
                );

            decimal currentPrice;

            if (correction != null)
            {
                currentPrice =
                    GetAppliedPrice(
                        correction
                    );
            }
            else if (roomChanges.Count > 0)
            {
                currentPrice =
                    roomChanges[0]
                        .OldRoomPrice;
            }
            else
            {
                currentPrice =
                    booking.Room?.Price ?? 0;
            }

            foreach (
                var roomChange
                in roomChanges)
            {
                if (roomChange.PriceMode ==
                    "CorrectWrongRoom")
                {
                    continue;
                }

                if (roomChange.ChangedAt <=
                    time)
                {
                    currentPrice =
                        GetAppliedPrice(
                            roomChange
                        );
                }
                else
                {
                    break;
                }
            }

            return currentPrice;
        }

        private static decimal CalculateHourlyRoomAmount(
            Booking booking,
            List<RoomChangeHistory> roomChanges,
            DateTime billingEndTime)
        {
            var correction =
                roomChanges.FirstOrDefault(
                    h => h.PriceMode ==
                         "CorrectWrongRoom"
                );

            decimal initialPrice;

            if (correction != null)
            {
                initialPrice =
                    GetAppliedPrice(
                        correction
                    );
            }
            else if (roomChanges.Count > 0)
            {
                initialPrice =
                    roomChanges[0]
                        .OldRoomPrice;
            }
            else
            {
                initialPrice =
                    booking.Room
                        ?.HourlyPrice ?? 0;
            }

            DateTime segmentStart =
                booking.CheckInDate;

            decimal currentPrice =
                initialPrice;

            decimal totalAmount = 0;

            foreach (
                var roomChange
                in roomChanges)
            {
                if (roomChange.PriceMode ==
                    "CorrectWrongRoom")
                {
                    continue;
                }

                if (roomChange.ChangedAt <=
                    booking.CheckInDate)
                {
                    currentPrice =
                        GetAppliedPrice(
                            roomChange
                        );

                    continue;
                }

                if (roomChange.ChangedAt >=
                    billingEndTime)
                {
                    break;
                }

                totalAmount +=
                    CalculateHourlySegment(
                        segmentStart,
                        roomChange.ChangedAt,
                        currentPrice
                    );

                segmentStart =
                    roomChange.ChangedAt;

                currentPrice =
                    GetAppliedPrice(
                        roomChange
                    );
            }

            totalAmount +=
                CalculateHourlySegment(
                    segmentStart,
                    billingEndTime,
                    currentPrice
                );

            return totalAmount;
        }

        private static decimal CalculateHourlySegment(
            DateTime startTime,
            DateTime endTime,
            decimal hourlyPrice)
        {
            double totalHours =
                (endTime - startTime)
                .TotalHours;

            if (totalHours <= 0)
                return 0;

            int hours =
                (int)Math.Ceiling(
                    totalHours
                );

            return CalculateHourlyPrice(
                hourlyPrice,
                hours
            );
        }

        private static decimal CalculateHourlyPrice(
            decimal hourlyPrice,
            int hours)
        {
            if (hours <= 1)
                return hourlyPrice;

            return hourlyPrice +
                   ((hours - 1) *
                    100000m);
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