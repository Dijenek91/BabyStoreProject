using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using BabyStore.DAL;
using BabyStore.Models.BabyStoreModelClasses;
using BabyStore.ViewModel.Products;
using PagedList;
using BabyStore.Utilities;
using Serilog;

namespace BabyStore.Controllers.ModelControllers
{
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private StoreContext db = new StoreContext();

        // GET: Products
        [AllowAnonymous]
        public async Task<ActionResult> Index(string category, string search, string sortBy, int? page)
        {
            ProductIndexViewModel viewModel = new ProductIndexViewModel();
            
            var products = db.Products.Include(p => p.Category);

            if (!string.IsNullOrWhiteSpace(search))
            {
                products = SearchProductsFor(search, products);
                viewModel.Search = search;
            }
            
            //group search results into categories and count how many items in each category
            viewModel.CatsWithCount = viewModel.CatsWithCount = from matchingProducts in products
                                                                where
                                                                matchingProducts.CategoryID != null
                                                                group matchingProducts by
                                                                matchingProducts.Category.Name into
                                                                catGroup
                                                                select new CategoryWithCount()
                                                                {
                                                                    CategoryName = catGroup.Key,
                                                                    ProductCount = catGroup.Count()
                                                                };
            


            if (!string.IsNullOrWhiteSpace(category))
            {
                products = products.Where(p => p.Category.Name == category);
                viewModel.Category = category;
            }
           
            switch (sortBy)
            {
                case "price_lowest":
                    products = products.OrderBy(p => p.Price);
                    break;
                case "price_highest":
                    products = products.OrderByDescending(p => p.Price);
                    break;
                default:
                    products = products.OrderBy(p => p.Name);
                    break;
            }
      
            int currentPage = (page ?? 1);
            viewModel.CurrentPage = currentPage;
            viewModel.TotalNumberOfProducts = products.Count();
            viewModel.TotalPages = (int)Math.Ceiling((decimal)viewModel.TotalNumberOfProducts / Constants.PageItems);
            viewModel.currentPageOfProducts = await products.ReturnPages(currentPage, Constants.PageItems);

            viewModel.SortBy = sortBy;
            viewModel.Sorts = new Dictionary<string, string>
            {
                {"Price low to high", "price_lowest" },
                {"Price high to low", "price_highest" }
            };

            return View(viewModel);
        }

        // GET: Products/Details/5
        [AllowAnonymous]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // GET: Products/Create
        public ActionResult Create()
        {
            ProductViewModel viewModel = new ProductViewModel();
            viewModel.CategoryList = new SelectList(db.Categories, "ID", "Name");
            viewModel.ImageLists = new List<SelectList>();
            for (int i = 0; i < Constants.NumberOfProductImages; i++)
            {
                viewModel.ImageLists.Add(new SelectList(db.ProductImages,"ID","FileName"));
            }

            return View(viewModel);
        }

        // POST: Products/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProductViewModel viewModel)
        {
            var product = new Product();
            product.Name = viewModel.Name;
            product.Category = db.Categories.Find(viewModel.CategoryID);
            product.CategoryID = viewModel.CategoryID;
            product.Description = viewModel.Description;
            product.Price = viewModel.Price;
            product.ProductImageMappings = new List<ProductImageMapping>();
            string[] productImages = viewModel.ProductImages.Where(pi => !string.IsNullOrWhiteSpace(pi)).ToArray();

            for (int i = 0; i < productImages.Length; i++)
            {
                var prodImageMapping = new ProductImageMapping();
                prodImageMapping.ImageNumber = i;
                prodImageMapping.ProductImage = db.ProductImages.Find(int.Parse(productImages[i]));

                product.ProductImageMappings.Add(prodImageMapping);
            }

            if (ModelState.IsValid)
            {
                db.Products.Add(product);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            viewModel.CategoryList = new SelectList(db.Categories, "ID", "Name", product.CategoryID);
            viewModel.ImageLists = new List<SelectList>();
            for (int i = 0; i < Constants.NumberOfProductImages; i++)
            {
                viewModel.ImageLists.Add(new SelectList(db.ProductImages, "ID", "FileName", viewModel.ProductImages[i]));
            }
            ViewBag.CategoryID = new SelectList(db.Categories, "ID", "Name", product.CategoryID);
            return View(product);
        }

        // GET: Products/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }

            ProductViewModel viewModel = MapProductToViewModel(product);

            return View(viewModel);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ProductViewModel viewModel)
        {
            string[] fieldsToBind = new string[] { "Name", "Description", "Price", "CategoryID" };
            var productToUpdate = db.Products.Include(p => p.ProductImageMappings).Where(p => p.ID == viewModel.ID).Single();

            if (productToUpdate == null)
            {
                Product deletedProduct = new Product();
                TryUpdateModel(deletedProduct, fieldsToBind);
                ModelState.AddModelError(string.Empty, "Unable to save your changes because the product has been deleted by another user.");

                ProductViewModel outViewModel = MapProductToViewModel(deletedProduct);
                return View(outViewModel);
            }

            if (TryUpdateModel(productToUpdate, "", fieldsToBind))
            {
                try
                {
                    if (productToUpdate.ProductImageMappings == null)
                    {
                        productToUpdate.ProductImageMappings = new List<ProductImageMapping>();
                    }

                    string[] submitedImages = viewModel.ProductImages.Where(pi => !string.IsNullOrEmpty(pi)).ToArray();
                    for (int i = 0; i < submitedImages.Length; i++)
                    {
                        var imageMappingToEdit =
                            productToUpdate.ProductImageMappings.Where(pi => pi.ImageNumber == i).FirstOrDefault();
                        var image = db.ProductImages.Find(int.Parse(submitedImages[i]));

                        //if there is nothing stored then we need to add a new mapping
                        if (imageMappingToEdit == null)
                        {
                            //add image to the imagemappings
                            productToUpdate.ProductImageMappings.Add(new ProductImageMapping
                            {
                                ImageNumber = i,
                                ProductImage = image,
                                ProductImageID = image.ID
                            });
                        }
                        //else it's not a new file so edit the current mapping
                        else
                        {
                            //if they are not the same
                            if (imageMappingToEdit.ProductImageID != int.Parse(submitedImages[i]))
                            {
                                //assign image property of the image mapping
                                imageMappingToEdit.ProductImage = image;
                            }
                        }
                    }

                    //remove old images if they differ from new submited iamges on product
                    for (int i = submitedImages.Length; i < Constants.NumberOfProductImages; i++)
                    {
                        var imageMappingToEdit =
                            productToUpdate.ProductImageMappings.Where(pim => pim.ImageNumber == i).FirstOrDefault();
                        //if there is something stored in the mapping
                        if (imageMappingToEdit != null)
                        {
                            //delete the record from the mapping table directly.
                            //just calling productToUpdate.ProductImageMappings.Remove(imageMappingToEdit)
                            //results in a FK error
                            db.ProductImageMappings.Remove(imageMappingToEdit);
                        }
                    }
                    db.Entry(productToUpdate).OriginalValues["RowVersion"] = viewModel.RowVersion;
                    db.SaveChanges();
                    return RedirectToAction("Index");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var exEntry = ex.Entries.Single();
                var currentUIValues = (Product) exEntry.Entity;
                var databaseProduct = exEntry.GetDatabaseValues();

                if (databaseProduct == null)
                {
                    ModelState.AddModelError(string.Empty, "Unable to save your changes because the category has been deleted by another user.");
                }
                else
                {
                    var databaseProductValues = (Product)databaseProduct.ToObject();
                    VerifyProductFields(databaseProductValues, currentUIValues);

                    ModelState.AddModelError(string.Empty, "The record has been modified by another user after you loaded the screen.Your changes have not yet been saved. "
                    + "The new values in the database are shown below. If you want to overwrite these values with your changes then click save otherwise go back to the categories page.");

                    viewModel = MapProductToViewModel(currentUIValues);
                    viewModel.RowVersion = databaseProductValues.RowVersion;
                } 
            }
        }
            return View(viewModel);
        }

        private void VerifyProductFields(Product databaseProductValues, Product currentUIValues)
        {
            if (databaseProductValues.Name != currentUIValues.Name)
            {
                ModelState.AddModelError("Name", "Current value in database: " + databaseProductValues.Name);
            }
            if (databaseProductValues.Description != currentUIValues.Description)
            {
                ModelState.AddModelError("Description", "Current value in database: " + databaseProductValues.Description);
            }
            if (databaseProductValues.CategoryID != currentUIValues.CategoryID)
            {
                ModelState.AddModelError("CategoryID", "Current value in database: " + databaseProductValues.CategoryID);
            }
            if (databaseProductValues.Price != currentUIValues.Price)
            {
                ModelState.AddModelError("Price", "Current value in database: " + databaseProductValues.Price);
            }
            //var dbMapping = databaseProductValues.ProductImageMappings.ToList();//TODO: how do i retrtieve data for image mapping to verify the changes
            //var uiMapping = currentUIValues.ProductImageMappings.ToList();
               
            //if (Enumerable.SequenceEqual(dbMapping, uiMapping))
            //{
            //    ModelState.AddModelError("Price", "Current value in database: " + databaseProductValues.Price);
            //}    
        }

        // GET: Products/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = db.Products.Find(id);
            db.Products.Remove(product);

            var orderLines = db.OrderLines.Where(ol => ol.ProductID == id);
            foreach (var ol in orderLines)
            {
                ol.ProductID = null;
            }

            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private static IQueryable<Product> SearchProductsFor(string search, IQueryable<Product> products)
        {
            return products.Where(
                p => p.Name.Contains(search) || p.Category.Name.Contains(search) || p.Description.Contains(search));
        }

        protected override void OnException(ExceptionContext filterContext)
        {
            Log.Error("Exception occured with message: {Message}", filterContext.Exception.Message);
            Log.Error("Stacktrace: {StackTrace}", filterContext.Exception.StackTrace);
            base.OnException(filterContext);
        }

        private ProductViewModel MapProductToViewModel(Product product)
        {
            var viewModel = new ProductViewModel();
            viewModel.Name = product.Name;
            viewModel.Description = product.Description;
            viewModel.CategoryID = (int)product.CategoryID;
            viewModel.Price = product.Price;
            viewModel.RowVersion = product.RowVersion;

            viewModel.CategoryList = new SelectList(db.Categories, "ID", "Name", product.CategoryID);
            viewModel.ImageLists = new List<SelectList>();

            foreach (var imageMapping in product.ProductImageMappings.OrderBy(pi => pi.ImageNumber))
            {
                viewModel.ImageLists.Add(new SelectList(db.ProductImages, "ID", "FileName", imageMapping.ProductImageID));
            }
            for (int i = viewModel.ImageLists.Count; i < Constants.NumberOfProductImages; i++)
            {
                viewModel.ImageLists.Add(new SelectList(db.ProductImages, "ID", "FileName"));//BUG: db.productImages umesto da vrati listu svih product image-a vrati samo jedan image?
            }

            return viewModel;
        }
    }
}
