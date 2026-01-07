using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;
using OnlineShop.Models;

namespace OnlineShop.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public WishlistController(ApplicationDbContext db, UserManager<ApplicationUser> users)
        {
            _db = db;
            _users = users;
        }

        public async Task<IActionResult> Index()
        {
            var uid = _users.GetUserId(User);
            var items = await _db.WishlistItems.Include(w => w.Product).ThenInclude(p => p!.Category).Where(w => w.UserId == uid).OrderByDescending(w => w.AddedAt).ToListAsync();
            return View(items);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId)
        {
            var uid = _users.GetUserId(User);
            var prod = await _db.Products.FindAsync(productId);

            if (prod == null || prod.Status != ProductStatus.Aprobat)
            {
                TempData["Error"] = "Produs negasit.";
                return RedirectToAction("Index", "Products");
            }
            if (await _db.WishlistItems.AnyAsync(w => w.UserId == uid && w.ProductId == productId))
            {
                TempData["Warning"] = "Deja in wishlist.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            _db.WishlistItems.Add(new WishlistItem { UserId = uid!, ProductId = productId });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Adaugat in wishlist!";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int wishlistItemId)
        {
            var uid = _users.GetUserId(User);
            var item = await _db.WishlistItems.FirstOrDefaultAsync(w => w.Id == wishlistItemId && w.UserId == uid);
            if (item != null)
            {
                _db.WishlistItems.Remove(item);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Sters din wishlist.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveToCart(int wishlistItemId)
        {
            var uid = _users.GetUserId(User);
            var item = await _db.WishlistItems.Include(w => w.Product).FirstOrDefaultAsync(w => w.Id == wishlistItemId && w.UserId == uid);
            if (item == null) return NotFound();

            if (item.Product!.Stock <= 0)
            {
                TempData["Error"] = "Nu e in stoc.";
                return RedirectToAction("Index");
            }

            var cartItem = await _db.CartItems.FirstOrDefaultAsync(c => c.UserId == uid && c.ProductId == item.ProductId);
            if (cartItem != null)
                cartItem.Quantity += 1;
            else
                _db.CartItems.Add(new CartItem { UserId = uid!, ProductId = item.ProductId, Quantity = 1 });

            _db.WishlistItems.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Mutat in cos!";
            return RedirectToAction("Index", "Cart");
        }
    }
}
