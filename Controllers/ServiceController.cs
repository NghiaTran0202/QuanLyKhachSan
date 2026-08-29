using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using quanlykhachsan.Data;
using quanlykhachsan.Models;

namespace quanlykhachsan.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ServiceController : Controller
    {
        private readonly HotelDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ServiceController(
            HotelDbContext context,
            IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }
              
        public async Task<IActionResult> Index()
        {
            return View(await _context.Services.ToListAsync());
        }

      
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Service service)
        {
            
            if (service.ImageFile != null)
            {
                
                string fileName =
                    Guid.NewGuid().ToString() +
                    Path.GetExtension(service.ImageFile.FileName);
                                
                string folder = Path.Combine(
                    _env.WebRootPath,
                    "uploads",
                    "services"
                );

              
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string path = Path.Combine(folder, fileName);

               
                using (var stream = new FileStream(
                    path,
                    FileMode.Create))
                {
                    await service.ImageFile.CopyToAsync(stream);
                }

               
                service.Image = fileName;
            }

            if (ModelState.IsValid)
            {
                _context.Services.Add(service);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(service);
        }

       
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var service =
                await _context.Services.FindAsync(id);

            if (service == null)
                return NotFound();

            return View(service);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Service service)
        {
            if (id != service.ServiceId)
                return NotFound();

            
            var oldService =
                await _context.Services
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        s => s.ServiceId == id);

            if (oldService == null)
                return NotFound();

            
            if (service.ImageFile != null)
            {
                string fileName =
                    Guid.NewGuid().ToString() +
                    Path.GetExtension(service.ImageFile.FileName);

                string folder = Path.Combine(
                    _env.WebRootPath,
                    "uploads",
                    "services"
                );

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string path =
                    Path.Combine(folder, fileName);

                using (var stream = new FileStream(
                    path,
                    FileMode.Create))
                {
                    await service.ImageFile
                        .CopyToAsync(stream);
                }

               
                service.Image = fileName;
            }
            else
            {
                
                service.Image = oldService.Image;
            }

            if (ModelState.IsValid)
            {
                _context.Update(service);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(service);
        }

      
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var service =
                await _context.Services.FindAsync(id);

            if (service == null)
                return NotFound();

            return View(service);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var service =
                await _context.Services.FindAsync(id);

            if (service != null)
            {
                _context.Services.Remove(service);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}