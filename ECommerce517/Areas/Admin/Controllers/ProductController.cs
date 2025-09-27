using ECommerce517.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Threading.Tasks;

namespace ECommerce517.Areas.Admin.Controllers
{
    [Area(SD.AdminArea)]
    [Authorize(Roles = $"{SD.SuperAdminRole},{SD.AdminArea}")]
    public class ProductController : Controller
    {
        //ApplicationDbContext _context = new();
        private IProductRepository _productRepository;// = new ProductRepository();
        private IRepository<Brand> _brandRepository;// = new Repository<Brand>();
        private IRepository<Category> _categoryRepository;// = new Repository<Category>();

        public ProductController(IProductRepository productRepository, 
            IRepository<Brand> brandRepository, 
            IRepository<Category> categoryRepository)
        {
            _productRepository = productRepository;
            _brandRepository = brandRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAsync(includes: [e => e.Category]);

            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var categories = await _categoryRepository.GetAsync();
            var brands = await _brandRepository.GetAsync();

            CategoryWithBrandVM categoryWithBrandVM = new()
            {
                Categories = categories.ToList(),
                Brands = brands.ToList()
            };

            return View(categoryWithBrandVM);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product, IFormFile MainImg)
        {
            if (MainImg is not null && MainImg.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(MainImg.FileName); 
                // 0924fdsfs-d429-fskdf-jsd230-423.png

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images", fileName);

                // Save Img in wwwroot
                using (var stream = System.IO.File.Create(filePath))
                {
                    MainImg.CopyTo(stream);
                }

                // Sava img name in DB
                product.MainImg = fileName;

                // Save in DB
                await _productRepository.CreateAsync(product);
                await _productRepository.CommitAsync();

                return RedirectToAction(nameof(Index));
            }

            return BadRequest();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productRepository.GetOneAsync(e => e.Id == id);

            if (product is null)
                return RedirectToAction(SD.NotFoundPage, SD.HomeController);

            var categories = await _categoryRepository.GetAsync();
            var brands = await _brandRepository.GetAsync();

            CategoryWithBrandVM categoryWithBrandVM = new()
            {
                Categories = categories.ToList(),
                Brands = brands.ToList(),
                Product = product
            };

            return View(categoryWithBrandVM);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Product product, IFormFile? MainImg)
        {
            var productInDB = await _productRepository.GetOneAsync(e => e.Id == product.Id, tracked: false);

            if (productInDB is null)
                return BadRequest();

            if(MainImg is not null && MainImg.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(MainImg.FileName);
                // 0924fdsfs-d429-fskdf-jsd230-423.png

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images", fileName);

                // Save Img in wwwroot
                using (var stream = System.IO.File.Create(filePath))
                {
                    MainImg.CopyTo(stream);
                }

                // Delete old img from wwwroot
                var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images", productInDB.MainImg);
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }

                // Update img name in DB
                product.MainImg = fileName;
            }
            else
            {
                product.MainImg = productInDB.MainImg;
            }

            // Update in DB
            _productRepository.Update(product);
            await _productRepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetOneAsync(e => e.Id == id);

            if (product is null)
                return RedirectToAction(SD.NotFoundPage, SD.HomeController);

            // Delete old img from wwwroot
            var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images", product.MainImg);
            if (System.IO.File.Exists(oldFilePath))
            {
                System.IO.File.Delete(oldFilePath);
            }

            // Remove in DB
            _productRepository.Delete(product);
            await _productRepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
