using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Practice.Areas.AdminPanel.Data;
using Practice.DataAccessLayer;
using Practice.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Practice.Areas.AdminPanel.Controllers
{
    [Area("AdminPanel")]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _dbContext;

        public CategoryController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(int page=1)
        {
            if (page < 1)
                return BadRequest();

            if (((page - 1) * 10) >= await _dbContext.Categories.Where(x => x.IsDeleted == false).CountAsync())
                page--;

            var totalPageCount = Math.Ceiling((decimal)await _dbContext.Categories.Where(x => x.IsDeleted == false).CountAsync() / 10);
            if (page > totalPageCount)
                return NotFound();

            ViewBag.totalPageCount = totalPageCount;
            ViewBag.currentPage = page;
            var categories = await _dbContext.Categories.Where(x => x.IsDeleted == false)
                .ToListAsync();

            return View(categories);
        }

        public async Task<IActionResult> Create()
        {
            var parentCategories = await _dbContext.Categories
                .Where(x => x.IsDeleted == false && x.IsMain).ToListAsync();
            ViewBag.ParentCategories = parentCategories;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category, int parentCategoryId)
        {
            ViewBag.ParentCategories = await _dbContext.Categories.Where(x => x.IsDeleted == false && x.IsMain).ToListAsync();

            if (!ModelState.IsValid)
                return View();

            if (category.IsMain)
            {
                if (await _dbContext.Categories.AnyAsync(x => x.Name == category.Name && x.Id != category.Id && x.IsDeleted == false))
                {
                    ModelState.AddModelError("", "Category with this name already exist");
                    return View();
                }
                if (category.Photo == null)
                {
                    ModelState.AddModelError("", "Please Choose Photo");
                    return View();
                }

                if (!category.Photo.IsImage())
                {
                    ModelState.AddModelError("", "File must to be a Photo!");
                    return View();
                }

                if (!category.Photo.IsAllowedSize(1))
                {
                    ModelState.AddModelError("", "The size of the photo cannot be more than one MegaByte!");
                    return View();
                }

                var fileName = await category.Photo.GenerateFile(Constants.ImageFolderPath);

                category.Image = fileName;
            }
            else
            {
                if (parentCategoryId == 0)
                {
                    ModelState.AddModelError("", "Please choose Parent Category");
                    return View();
                }

                var existParentCategory = await _dbContext.Categories
                    .Include(x => x.Children.Where(y => y.IsDeleted == false))
                    .FirstOrDefaultAsync(x => x.IsDeleted == false && x.Id == parentCategoryId);
                if (existParentCategory == null)
                    return NotFound();

                var existChildCategory = existParentCategory.Children
                    .Any(x => x.Name.ToLower() == category.Name.ToLower());
                if (existChildCategory)
                {
                    ModelState.AddModelError("", "Category with this name already exist");
                    return View();
                }
                category.Parent = existParentCategory;
            }

            category.IsDeleted = false;
            await _dbContext.Categories.AddAsync(category);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();
            var category = await _dbContext.Categories.Where(x => x.Id == id && x.IsDeleted == false).Include(x => x.Parent).Include(x => x.Children.Where(y => y.IsDeleted == false)).FirstOrDefaultAsync();
            if (category == null)
                return NotFound();
            return View(category);
        }

        public async Task<IActionResult> Update(int? id)
        {
            if (id == null)
                return NotFound();
            var category = await _dbContext.Categories.Where(x => x.Id == id && x.IsDeleted == false).Include(x => x.Parent).Include(x => x.Children.Where(y => y.IsDeleted == false)).FirstOrDefaultAsync();
            if (category == null)
                return NotFound();
            ViewBag.ParentCategories = await _dbContext.Categories.Where(x => x.IsDeleted == false && x.IsMain).ToListAsync();
            return View(category);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int? id,Category category)
        {
            ViewBag.ParentCategories = await _dbContext.Categories.Where(x => x.IsDeleted == false && x.IsMain).ToListAsync();
            if (id == null)
                return NotFound();

            if (id != category.Id)
                return BadRequest();
            var existCategory = await _dbContext.Categories.FindAsync(id);
            if (existCategory == null)
                return NotFound();
         
            if (category.IsMain)
            {
                var existForName= await _dbContext.Categories.Where(x => x.Name == category.Name && x.Id != existCategory.Id && x.IsDeleted == false).FirstOrDefaultAsync();
                if (existForName!=null)
                {
                    ModelState.AddModelError("", $"{(existForName.IsMain ? "Parent" : "Child")} Category with this name already exist");
                    return View(existCategory);
                }
                if (category.Photo != null)
                {
                    if (!category.Photo.IsImage())
                    {
                        ModelState.AddModelError("Photo", "File must to be a Photo!");
                        return View(existCategory);
                    }
                    if (!category.Photo.IsAllowedSize(1))
                    {
                        ModelState.AddModelError("Photo", "The size of the photo cannot be more than one MegaByte!");
                        return View(existCategory);
                    }
                    var existPath = Path.Combine(Constants.ImageFolderPath, existCategory.Image);
                    if (System.IO.File.Exists(existPath))
                    {
                        System.IO.File.Delete(existPath);
                    }

                    category.Image = await category.Photo.GenerateFile(Constants.ImageFolderPath);
                    existCategory.Image = category.Image;
                }
            }
            else
            {
                if (category.Parent.Id == 0)
                {
                    ModelState.AddModelError("", "Please choose Parent Category");
                    return View();
                }

                var existParentCategory = await _dbContext.Categories
                    .Include(x => x.Children.Where(y => y.IsDeleted == false))
                    .FirstOrDefaultAsync(x => x.IsDeleted == false && x.Id == category.Parent.Id);
                if (existParentCategory == null)
                    return NotFound();

                var existChildCategory = existParentCategory.Children
                    .Any(x => x.Name.ToLower() == category.Name.ToLower());
                if (existChildCategory)
                {
                    ModelState.AddModelError("", $"{category.Parent.Name} Category already have child with this name");
                    return View();
                }
                existCategory.Parent = existParentCategory;
            }
            existCategory.Name = category.Name;
            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _dbContext.Categories.Where(x => x.Id == id && x.IsDeleted == false).Include(x => x.Parent).Include(x => x.Children.Where(y => y.IsDeleted == false)).FirstOrDefaultAsync();
            if (category == null)
                return NotFound();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteCategory(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _dbContext.Categories
                .Where(x => x.Id == id && x.IsDeleted == false)
                .Include(x => x.Children.Where(y => y.IsDeleted == false))
                .FirstOrDefaultAsync();
            if (category == null)
                return NotFound();

            category.IsDeleted = true;
            if (category.IsMain)
            {
                foreach (var item in category.Children)
                {
                    item.IsDeleted = true;
                }
                var path = Path.Combine(Constants.ImageFolderPath, "img", category.Image);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
            await _dbContext.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
