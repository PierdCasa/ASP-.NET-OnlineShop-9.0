using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;
using OnlineShop.Models;

namespace OnlineShop.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IWebHostEnvironment _env;

        public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> users, IWebHostEnvironment env)
        {
            _db = db;
            _users = users;
            _env = env;
        }

        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalSales = (await _db.Orders.Where(o => o.Status == "Livrata").ToListAsync()).Sum(o => o.TotalAmount);
            ViewBag.TotalOrders = await _db.Orders.CountAsync();
            ViewBag.TotalProducts = await _db.Products.CountAsync();
            ViewBag.TotalUsers = await _users.Users.CountAsync();
            ViewBag.RecentOrders = await _db.Orders.Include(o => o.User).OrderByDescending(o => o.CreatedAt).Take(5).ToListAsync();
            ViewBag.LowStockProducts = await _db.Products.Where(p => p.Stock <= 10).OrderBy(p => p.Stock).Take(5).ToListAsync();
            return View();
        }

        public async Task<IActionResult> Products() => View(await _db.Products.Include(p => p.Category).OrderByDescending(p => p.Id).ToListAsync());
        
        public async Task<IActionResult> Orders() => View(await _db.Orders.Include(o => o.User).OrderByDescending(o => o.CreatedAt).ToListAsync());
        
        public async Task<IActionResult> Categories() => View(await _db.Categories.Include(c => c.Products).OrderBy(c => c.Name).ToListAsync());

        [HttpGet]
        public IActionResult CreateCategory() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(Category cat)
        {
            if (await _db.Categories.AnyAsync(c => c.Name == cat.Name))
            {
                ModelState.AddModelError("Name", "Categoria exista deja.");
                return View(cat);
            }
            if (ModelState.IsValid)
            {
                _db.Categories.Add(cat);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Categorie creata!";
                return RedirectToAction("Categories");
            }
            return View(cat);
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            var cat = await _db.Categories.FindAsync(id);
            return cat == null ? NotFound() : View(cat);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(Category cat)
        {
            if (await _db.Categories.AnyAsync(c => c.Name == cat.Name && c.Id != cat.Id))
            {
                ModelState.AddModelError("Name", "Categoria exista deja.");
                return View(cat);
            }
            var existing = await _db.Categories.FindAsync(cat.Id);
            if (existing == null) return NotFound();
            existing.Name = cat.Name;
            existing.Description = cat.Description;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Categorie actualizata!";
            return RedirectToAction("Categories");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var cat = await _db.Categories
                .Include(c => c.Products!)
                    .ThenInclude(p => p.OrderItems)
                .Include(c => c.Products!)
                    .ThenInclude(p => p.CartItems)
                .Include(c => c.Products!)
                    .ThenInclude(p => p.WishlistItems)
                .Include(c => c.Products!)
                    .ThenInclude(p => p.Reviews)
                .Include(c => c.Products!)
                    .ThenInclude(p => p.FAQs)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            if (cat == null) return NotFound();
            if (cat.Products != null && cat.Products.Any())
            {
                foreach (var p in cat.Products)
                {
                    if (p.OrderItems != null && p.OrderItems.Any())
                        _db.OrderItems.RemoveRange(p.OrderItems);
                    if (p.CartItems != null && p.CartItems.Any())
                        _db.CartItems.RemoveRange(p.CartItems);
                    if (p.WishlistItems != null && p.WishlistItems.Any())
                        _db.WishlistItems.RemoveRange(p.WishlistItems);
                    if (p.Reviews != null && p.Reviews.Any())
                        _db.Reviews.RemoveRange(p.Reviews);
                    if (p.FAQs != null && p.FAQs.Any())
                        _db.FAQs.RemoveRange(p.FAQs);
                }
                _db.Products.RemoveRange(cat.Products);
            }
            
            _db.Categories.Remove(cat);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Categorie ștearsă!";
            return RedirectToAction("Categories");
        }

        public async Task<IActionResult> Users()
        {
            var lista = await _users.Users.ToListAsync();
            var roluri = new Dictionary<string, string>();
            foreach (var u in lista)
                roluri[u.Id!] = string.Join(", ", await _users.GetRolesAsync(u));
            ViewBag.UserRoles = roluri;
            ViewBag.AvailableRoles = new[] { "Admin", "Collaborator", "User" };
            return View(lista);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserRole(string userId, string newRole)
        {
            var user = await _users.FindByIdAsync(userId);
            if (user == null) return NotFound();
            await _users.RemoveFromRolesAsync(user, await _users.GetRolesAsync(user));
            await _users.AddToRoleAsync(user, newRole);
            TempData["Success"] = "Rol schimbat!";
            return RedirectToAction("Users");
        }

        public async Task<IActionResult> OrderDetails(int? id)
        {
            if (id == null) return NotFound();
            var order = await _db.Orders.Include(o => o.User).Include(o => o.OrderItems!).ThenInclude(oi => oi.Product).FirstOrDefaultAsync(o => o.Id == id);
            return order == null ? NotFound() : View(order);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();
            order.Status = status;
            order.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Status actualizat!";
            return RedirectToAction("OrderDetails", new { id });
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
            p.Status = ProductStatus.Aprobat;
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
                TempData["Success"] = "Produs adaugat!";
                return RedirectToAction("Products");
            }
            ViewBag.Categories = await _db.Categories.ToListAsync();
            return View(p);
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var p = await _db.Products.FindAsync(id);
            if (p == null) return NotFound();
            ViewBag.Categories = await _db.Categories.ToListAsync();
            return View(p);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(Product p, IFormFile? imageFile)
        {
            ModelState.Remove("ImagePath");
            ModelState.Remove("Category");
            ModelState.Remove("CreatedByUser");
            ModelState.Remove("Reviews");
            ModelState.Remove("CartItems");
            ModelState.Remove("WishlistItems");
            ModelState.Remove("OrderItems");
            ModelState.Remove("FAQs");
            
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _db.Categories.ToListAsync();
                return View(p);
            }
            var existing = await _db.Products.FindAsync(p.Id);
            if (existing == null) return NotFound();
            
            existing.Title = p.Title;
            existing.Description = p.Description;
            existing.Price = p.Price;
            existing.Stock = p.Stock;
            existing.CategoryId = p.CategoryId;

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
            return RedirectToAction("Products");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var p = await _db.Products
                .Include(x => x.OrderItems)
                .Include(x => x.CartItems)
                .Include(x => x.WishlistItems)
                .Include(x => x.Reviews)
                .Include(x => x.FAQs)
                .FirstOrDefaultAsync(x => x.Id == id);
            
            if (p != null)
            {
                if (!string.IsNullOrEmpty(p.SeedId))
                {
                    var alreadyDeleted = await _db.DeletedSeeds.AnyAsync(d => d.SeedId == p.SeedId);
                    if (!alreadyDeleted)
                    {
                        _db.DeletedSeeds.Add(new DeletedSeed { SeedId = p.SeedId, DeletedAt = DateTime.Now });
                    }
                }

                if (p.OrderItems != null && p.OrderItems.Any())
                    _db.OrderItems.RemoveRange(p.OrderItems);
                if (p.CartItems != null && p.CartItems.Any())
                    _db.CartItems.RemoveRange(p.CartItems);
                if (p.WishlistItems != null && p.WishlistItems.Any())
                    _db.WishlistItems.RemoveRange(p.WishlistItems);
                if (p.Reviews != null && p.Reviews.Any())
                    _db.Reviews.RemoveRange(p.Reviews);
                if (p.FAQs != null && p.FAQs.Any())
                    _db.FAQs.RemoveRange(p.FAQs);
                
                _db.Products.Remove(p);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Produs șters!";
            }
            return RedirectToAction("Products");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProduct(int id)
        {
            var p = await _db.Products.FindAsync(id);
            if (p == null) return NotFound();
            p.Status = ProductStatus.Aprobat;
            p.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Produs aprobat!";
            return RedirectToAction("Products");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectProduct(int id, string? reason)
        {
            var p = await _db.Products.FindAsync(id);
            if (p == null) return NotFound();
            p.Status = ProductStatus.Respins;
            p.RejectionReason = reason;
            p.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            TempData["Warning"] = "Produs respins!";
            return RedirectToAction("Products");
        }

        public async Task<IActionResult> Reviews() => View(await _db.Reviews.Include(r => r.User).Include(r => r.Product).OrderByDescending(r => r.CreatedAt).ToListAsync());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var r = await _db.Reviews.FindAsync(id);
            if (r == null) return NotFound();
            var prodId = r.ProductId;
            _db.Reviews.Remove(r);
            await _db.SaveChangesAsync();

            var prod = await _db.Products.Include(p => p.Reviews).FirstOrDefaultAsync(p => p.Id == prodId);
            if (prod != null)
            {
                var ratings = prod.Reviews?.Where(x => x.Rating.HasValue).Select(x => x.Rating!.Value).ToList();
                prod.Rating = ratings != null && ratings.Any() ? (decimal)ratings.Average() : null;
                await _db.SaveChangesAsync();
            }
            TempData["Success"] = "Review sters!";
            return RedirectToAction("Reviews");
        }
    }
}
