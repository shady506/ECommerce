using ECommerce517.Models;
using ECommerce517.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ECommerce517.Areas.Admin.Controllers
{
    [Area(SD.AdminArea)]
    [Authorize(Roles = $"{SD.SuperAdminRole},{SD.AdminArea}")]
    public class BrandController : Controller
    {
        //private ApplicationDbContext _context = new();
        private IRepository<Brand> _brandRepository;// = new Repository<Brand>();

        public BrandController(IRepository<Brand> brandRepository)
        {
            _brandRepository = brandRepository;
        }

        public async Task<IActionResult> Index()
        {
            var brands = await _brandRepository.GetAsync();

            return View(brands);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Brand());
        }

        [HttpPost]
        public async Task<IActionResult> Create(Brand brand)
        {
            if(!ModelState.IsValid)
            {
                return View(brand);
            }

            await _brandRepository.CreateAsync(brand);
            await _brandRepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _brandRepository.GetOneAsync(e => e.Id == id);

            if (brand is null)
                return RedirectToAction(SD.NotFoundPage, SD.HomeController);

            return View(brand);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Brand brand)
        {
            if (!ModelState.IsValid)
            {
                return View(brand);
            }

            _brandRepository.Update(brand);
            await _brandRepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _brandRepository.GetOneAsync(e => e.Id == id);

            if (brand is null)
                return RedirectToAction(SD.NotFoundPage, SD.HomeController);

            _brandRepository.Delete(brand);
            await _brandRepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
