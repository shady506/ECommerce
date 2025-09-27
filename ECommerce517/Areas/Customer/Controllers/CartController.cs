using ECommerce517.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Threading.Tasks;

namespace ECommerce517.Areas.Customer.Controllers
{
    [Area(SD.CustomerArea)]
    [Authorize]
    public class CartController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRepository<Cart> _cartRepository;
        private readonly IRepository<Promotion> _promotionRepository;

        public CartController(UserManager<ApplicationUser> userManager, IRepository<Cart> cartRepository, IRepository<Promotion> promotionRepository)
        {
            _userManager = userManager;
            _cartRepository = cartRepository;
            _promotionRepository = promotionRepository;
        }

        public async Task<IActionResult> AddToCart(CartRequestVM cartRequestVM)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            var cart = await _cartRepository.GetOneAsync(e => e.ApplicationUserId == user.Id && e.ProductId == cartRequestVM.ProductId);

            if (cart is not null)
            {
                cart.Count += cartRequestVM.Count;
            }
            else
            {
                await _cartRepository.CreateAsync(new()
                {
                    ApplicationUserId = user.Id,
                    ProductId = cartRequestVM.ProductId,
                    Count = cartRequestVM.Count
                });
            }
            
            await _cartRepository.CommitAsync();

            TempData["success-notification"] = "Add Product To Cart Successfully";
            return RedirectToAction("Index", "Home", new { area = "Customer" });
        }

        public async Task<IActionResult> Index(string? code = null)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            var carts = await _cartRepository.GetAsync(e => e.ApplicationUserId == user.Id, includes: [e=>e.Product]);

            var totalPrice = carts.Sum(e => e.Product.Price * e.Count);

            if(code is not null)
            {
                var promotion = await _promotionRepository.GetOneAsync(e => e.Code == code);
                if (promotion is null || !promotion.Status || DateTime.UtcNow > promotion.ValidTo)
                {
                    TempData["error-notification"] = "Invalid Code OR Expired";
                }
                else
                {
                    promotion.TotalUsed += 1;
                    await _promotionRepository.CommitAsync();

                    totalPrice = totalPrice - (totalPrice * 0.05);
                    TempData["success-notification"] = "Apply Promotion";
                }
            }
            
            ViewBag.TotalPrice = totalPrice;

            return View(carts);
        }

        public async Task<IActionResult> IncrementCart(int productId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            var cart = await _cartRepository.GetOneAsync(e => e.ApplicationUserId == user.Id && e.ProductId == productId);

            if (cart is null)
                return NotFound();

            cart.Count += 1;
            await _cartRepository.CommitAsync();

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> DecrementCart(int productId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            var cart = await _cartRepository.GetOneAsync(e => e.ApplicationUserId == user.Id && e.ProductId == productId);

            if (cart is null)
                return NotFound();

            if(cart.Count > 1)
            {
                cart.Count -= 1;
                await _cartRepository.CommitAsync();
            }
            
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> DeleteCart(int productId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            var cart = await _cartRepository.GetOneAsync(e => e.ApplicationUserId == user.Id && e.ProductId == productId);

            if (cart is null)
                return NotFound();

            _cartRepository.Delete(cart);
            await _cartRepository.CommitAsync();

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Pay()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            var carts = await _cartRepository.GetAsync(e => e.ApplicationUserId == user.Id, includes: [e=>e.Product]);

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = $"{Request.Scheme}://{Request.Host}/Customer/Checkout/Success",
                CancelUrl = $"{Request.Scheme}://{Request.Host}/Customer/checkout/cancel",
            };

            foreach (var item in carts)
            {
                options.LineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "egp",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Name,
                            Description = item.Product.Description,
                        },
                        UnitAmount = (long)item.Product.Price * 100,
                    },
                    Quantity = item.Count,
                });
            }

            var service = new SessionService();
            var session = service.Create(options);

            return Redirect(session.Url);
        }
    }
}
