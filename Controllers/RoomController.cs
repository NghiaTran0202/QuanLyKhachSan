using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using quanlykhachsan.Data;
using quanlykhachsan.Models;

namespace quanlykhachsan.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoomController : Controller
    {
        private readonly HotelDbContext _context;
        private readonly IWebHostEnvironment _env;

        public RoomController(HotelDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index(string search)
        {
            ViewBag.Search = search;

            var rooms = _context.Rooms
                .Include(r => r.RoomType)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                rooms = rooms.Where(r =>
                    r.RoomNumber.Contains(search) ||
                    r.RoomName.Contains(search) ||
                    r.RoomType.TypeName.Contains(search));
            }

            return View(await rooms.ToListAsync());
        }

        public IActionResult Create()
        {
            ViewBag.RoomTypes = _context.RoomTypes.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Room room)
        {
            room.Status = false;

            if (room.Price <= 0)
                ModelState.AddModelError(nameof(Room.Price), "Giá theo ngày phải lớn hơn 0.");

            if (room.HourlyPrice <= 0)
                ModelState.AddModelError(nameof(Room.HourlyPrice), "Giá giờ đầu phải lớn hơn 0.");

            if (room.ImageFile != null)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(room.ImageFile.FileName);
                string folder = Path.Combine(_env.WebRootPath, "uploads");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string path = Path.Combine(folder, fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                    await room.ImageFile.CopyToAsync(stream);

                room.Image = fileName;
            }

            if (ModelState.IsValid)
            {
                _context.Rooms.Add(room);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đã thêm phòng {room.RoomNumber} thành công.";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.RoomTypes = _context.RoomTypes.ToList();
            return View(room);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var room = await _context.Rooms.FindAsync(id);

            if (room == null)
                return NotFound();

            ViewBag.RoomTypes = _context.RoomTypes.ToList();
            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Room room)
        {
            if (id != room.RoomId)
                return NotFound();

            var oldRoom = await _context.Rooms
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.RoomId == id);

            if (oldRoom == null)
                return NotFound();

            room.Status = oldRoom.Status;

            if (room.Price <= 0)
                ModelState.AddModelError(nameof(Room.Price), "Giá theo ngày phải lớn hơn 0.");

            if (room.HourlyPrice <= 0)
                ModelState.AddModelError(nameof(Room.HourlyPrice), "Giá giờ đầu phải lớn hơn 0.");

            if (room.ImageFile != null)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(room.ImageFile.FileName);
                string folder = Path.Combine(_env.WebRootPath, "uploads");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string path = Path.Combine(folder, fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                    await room.ImageFile.CopyToAsync(stream);

                room.Image = fileName;
            }
            else
            {
                room.Image = oldRoom.Image;
            }

            if (ModelState.IsValid)
            {
                _context.Update(room);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đã cập nhật phòng {room.RoomNumber} thành công.";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.RoomTypes = _context.RoomTypes.ToList();
            return View(room);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(x => x.RoomId == id);

            if (room == null)
                return NotFound();

            return View(room);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(x => x.RoomId == id);

            if (room == null)
                return NotFound();

            return View(room);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.FindAsync(id);

            if (room == null)
            {
                TempData["Error"] = "Không tìm thấy phòng.";
                return RedirectToAction(nameof(Index));
            }

            bool hasBookings = await _context.Bookings
                .AnyAsync(b => b.RoomId == id);

            if (hasBookings)
            {
                TempData["Error"] =
                    $"Không thể xóa phòng {room.RoomNumber} vì phòng này đã có dữ liệu đặt phòng.";

                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrEmpty(room.Image))
            {
                string imagePath = Path.Combine(
                    _env.WebRootPath,
                    "uploads",
                    room.Image);

                if (System.IO.File.Exists(imagePath))
                    System.IO.File.Delete(imagePath);
            }

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Đã xóa phòng {room.RoomNumber} thành công.";

            return RedirectToAction(nameof(Index));
        }
    }
}