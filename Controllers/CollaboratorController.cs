using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;
using OnlineShop.Models;

namespace OnlineShop.Controllers
{
    [Authorize(Roles = "Collaborator")]
    public class CollaboratorController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IWebHostEnvironment _env;

        public CollaboratorController(ApplicationDbContext db, UserManager<ApplicationUser> users, IWebHostEnvironment env)
        {
            _db = db;
            _users = users;
            _env = env;
        }

        public async Task<IActionResult> Dashboard()
        {
            var uid = _users.GetUserId(User);
            var produse = await _db.Products.Where(p => p.CreatedByUserId == uid).ToListAsync();
            var prodIds = produse.Select(p => p.Id).ToList();
            var comenzi = await _db.OrderItems.Include(oi => oi.Order).Where(oi => prodIds.Contains(oi.ProductId)).Select(oi => oi.Order).Distinct().ToListAsync();

            ViewBag.PersonalSales = comenzi.Sum(o => o!.TotalAmount);
            ViewBag.MyProducts = produse.Count;
            ViewBag.MyOrders = comenzi.Count;
            return View();
        }

        public async Task<IActionResult> MyProducts()
        {
            var uid = _users.GetUserId(User);
            return View(await _db.Products.Where(p => p.CreatedByUserId == uid).Include(p => p.Category).OrderByDescending(p => p.Id).ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> CreateProduct()
        {
            ViewBag.Categories = await _db.Categories.ToListAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product p, IFormFile? imageFile)
        {
            ModelState.Remove("ImagePath");
            ModelState.Remove("Category");
            ModelState.Remove("CreatedByUser");
            ModelState.Remove("Reviews");
            ModelState.Remove("CartItems");
            ModelState.Remove("WishlistItems");
            ModelState.Remove("OrderItems");
            ModelState.Remove("FAQs");
            
            p.CreatedByUserId = _users.GetUserId(User);
            p.Status = ProductStatus.Pending;
            p.CreatedAt = DateTime.Now;
            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError("", "Imaginea este obligatorie pentru un produs nou.");
                ViewBag.Categories = await _db.Categories.ToListAsync();
                return View(p);
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                var ext = Path.GetExtension(imageFile.FileName).ToLower();
                if (!new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(ext))
                {
                    ModelState.AddModelError("ImageFile", "Format invalid.");
                    ViewBag.Categories = await _db.Categories.ToListAsync();
                    return View(p);
                }
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ImageFile", "Max 5MB.");
                    ViewBag.Categories = await _db.Categories.ToListAsync();
                    return View(p);
                }
                var fileName = Guid.NewGuid() + ext;
                var path = Path.Combine(_env.WebRootPath, "images", "products", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var stream = new FileStream(path, FileMode.Create);
                await imageFile.CopyToAsync(stream);
                p.ImagePath = "/images/products/" + fileName;
            }
            else
            {
                p.ImagePath = "/images/products/default.jpg";
            }

            if (ModelState.IsValid)
            {
                _db.Products.Add(p);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Produs adaugat! Asteapta aprobare.";
                return RedirectToAction("MyProducts");
            }
            ViewBag.Categories = await _db.Categories.ToListAsync();
            return View(p);
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var uid = _users.GetUserId(User);
            var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id && x.CreatedByUserId == uid);
            if (p == null) return Forbid();
            ViewBag.Categories = await _db.Categories.ToListAsync();
            return View(p);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(Product p, IFormFile? imageFile)
        {
            var uid = _users.GetUserId(User);
            var existing = await _db.Products.FirstOrDefaultAsync(x => x.Id == p.Id && x.CreatedByUserId == uid);
            if (existing == null) return Forbid();

            existing.Title = p.Title;
            existing.Description = p.Description;
            existing.Price = p.Price;
            existing.Stock = p.Stock;
            existing.CategoryId = p.CategoryId;
            existing.Status = ProductStatus.Pending;

            if (imageFile != null && imageFile.Length > 0)
            {
                var ext = Path.GetExtension(imageFile.FileName).ToLower();
                if (!new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(ext))
                {
                    ModelState.AddModelError("ImageFile", "Format invalid.");
                    ViewBag.Categories = await _db.Categories.ToListAsync();
                    return View(p);
                }
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ImageFile", "Max 5MB.");
                    ViewBag.Categories = await _db.Categories.ToListAsync();
                    return View(p);
                }
                var fileName = Guid.NewGuid() + ext;
                var path = Path.Combine(_env.WebRootPath, "images", "products", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var stream = new FileStream(path, FileMode.Create);
                await imageFile.CopyToAsync(stream);
                existing.ImagePath = "/images/products/" + fileName;
            }

            existing.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Produs actualizat!";
            return RedirectToAction("MyProducts");
        }

        public async Task<IActionResult> MySales()
        {
            var uid = _users.GetUserId(User);
            var prodIds = await _db.Products.Where(p => p.CreatedByUserId == uid).Select(p => p.Id).ToListAsync();
            var orders = await _db.OrderItems.Include(oi => oi.Order).ThenInclude(o => o!.User).Where(oi => prodIds.Contains(oi.ProductId)).Select(oi => oi.Order).Distinct().OrderByDescending(o => o!.CreatedAt).ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> FAQs(int productId)
        {
            var uid = _users.GetUserId(User);
            var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == productId && x.CreatedByUserId == uid);
            if (p == null) return NotFound();
            ViewBag.ProductId = productId;
            ViewBag.ProductTitle = p.Title;
            return View(await _db.FAQs.Where(f => f.ProductId == productId).ToListAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFAQ(int productId, string question, string answer)
        {
            var uid = _users.GetUserId(User);
            var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == productId && x.CreatedByUserId == uid);
            if (p == null) return NotFound();
            _db.FAQs.Add(new FAQ { ProductId = productId, Question = question, Answer = answer, CreatedAt = DateTime.Now });
            await _db.SaveChangesAsync();
            TempData["Success"] = "FAQ adaugat!";
            return RedirectToAction("FAQs", new { productId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFAQ(int faqId, int productId)
        {
            var uid = _users.GetUserId(User);
            var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == productId && x.CreatedByUserId == uid);
            if (p == null) return NotFound();
            var faq = await _db.FAQs.FirstOrDefaultAsync(f => f.Id == faqId && f.ProductId == productId);
            if (faq != null)
            {
                _db.FAQs.Remove(faq);
                await _db.SaveChangesAsync();
                TempData["Success"] = "FAQ sters!";
            }
            return RedirectToAction("FAQs", new { productId });
        }
    }
}
