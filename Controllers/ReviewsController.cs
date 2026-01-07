using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;
using OnlineShop.Models;

namespace OnlineShop.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public ReviewsController(ApplicationDbContext db, UserManager<ApplicationUser> users)
        {
            _db = db;
            _users = users;
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int productId, string? text, int? rating)
        {
            var uid = _users.GetUserId(User);
            var prod = await _db.Products.Include(p => p.Reviews).FirstOrDefaultAsync(p => p.Id == productId && p.Status == ProductStatus.Aprobat);
            if (prod == null) return NotFound();

            if (string.IsNullOrWhiteSpace(text) && !rating.HasValue)
            {
                TempData["Error"] = "Completeaza text sau rating.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }
            if (rating.HasValue && (rating < 1 || rating > 5))
            {
                TempData["Error"] = "Rating 1-5.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }
            if (await _db.Reviews.AnyAsync(r => r.ProductId == productId && r.UserId == uid))
            {
                TempData["Error"] = "Ai deja un review.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            _db.Reviews.Add(new Review { ProductId = productId, UserId = uid!, Text = text, Rating = rating });
            await _db.SaveChangesAsync();
            await RecalcRating(productId);

            TempData["Success"] = "Review adaugat!";
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var uid = _users.GetUserId(User);
            var r = await _db.Reviews.Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
            return r == null ? NotFound() : View(r);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string? text, int? rating)
        {
            var uid = _users.GetUserId(User);
            var r = await _db.Reviews.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
            if (r == null) return NotFound();

            if (string.IsNullOrWhiteSpace(text) && !rating.HasValue)
            {
                TempData["Error"] = "Completeaza text sau rating.";
                return RedirectToAction("Edit", new { id });
            }
            if (rating.HasValue && (rating < 1 || rating > 5))
            {
                TempData["Error"] = "Rating 1-5.";
                return RedirectToAction("Edit", new { id });
            }

            r.Text = text;
            r.Rating = rating;
            r.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            await RecalcRating(r.ProductId);

            TempData["Success"] = "Review actualizat!";
            return RedirectToAction("Details", "Products", new { id = r.ProductId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var uid = _users.GetUserId(User);
            var r = await _db.Reviews.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
            if (r == null) return NotFound();

            var prodId = r.ProductId;
            _db.Reviews.Remove(r);
            await _db.SaveChangesAsync();
            await RecalcRating(prodId);

            TempData["Success"] = "Review sters.";
            return RedirectToAction("Details", "Products", new { id = prodId });
        }

        private async Task RecalcRating(int productId)
        {
            var prod = await _db.Products.Include(p => p.Reviews).FirstOrDefaultAsync(p => p.Id == productId);
            if (prod != null)
            {
                var ratings = prod.Reviews?.Where(r => r.Rating.HasValue).Select(r => r.Rating!.Value).ToList();
                prod.Rating = ratings != null && ratings.Any() ? (decimal)ratings.Average() : null;
                await _db.SaveChangesAsync();
            }
        }
    }
}
