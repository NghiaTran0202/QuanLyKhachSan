using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using quanlykhachsan.Data;
using quanlykhachsan.Models;

namespace quanlykhachsan.Controllers
{
    public class ContactController : Controller
    {
        private readonly HotelDbContext _context;

        public ContactController(HotelDbContext context)
        {
            _context = context;
        }

      
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(Contact contact)
        {
            if (contact.IsAnonymous)
            {
                contact.FullName = "Ẩn danh";
                contact.Phone = "";
                contact.Email = "";
            }

            contact.CreatedDate = DateTime.Now;

            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Gửi liên hệ thành công!";

            return RedirectToAction(nameof(Index));
        }

        
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> List()
        {
            var contacts = await _context.Contacts
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return View(contacts);
        }

       
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var contact = await _context.Contacts.FindAsync(id);

            if (contact != null)
            {
                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(List));
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var contact = await _context.Contacts.FindAsync(id);

            if (contact == null)
                return NotFound();

            contact.IsRead = true;

            await _context.SaveChangesAsync();

            return View(contact);
        }
    }
}