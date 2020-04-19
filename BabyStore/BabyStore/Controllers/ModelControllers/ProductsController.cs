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
using BabyStore.RepositoryLayer;
using BabyStore.RepositoryLayer.UnitOfWork;
using BabyStore.ViewModel.Products;
using BabyStore.Utilities;
using Serilog;

namespace BabyStore.Controllers.ModelControllers
{
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly IUnitOfWork<StoreContext> _unitOfWork = new GenericUnitOfWork<StoreContext>();
        private readonly IGenericRepository<Category> _categoryRepo;
        private readonly IGenericRepository<Product> _productRepo;
        private readonly IGenericRepository<ProductImage> _productImageRepo;

        #region Constructors
        public ProductsController()
        {
            _categoryRepo = _unitOfWork.GenericRepository<Category>();
            _productRepo = _unitOfWork.GenericRepository<Product>();
            _productImageRepo = _unitOfWork.GenericRepository<ProductImage>();
        }

        public ProductsController(IUnitOfWork<StoreContext> unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _categoryRepo = _unitOfWork.GenericRepository<Category>();
            _productRepo = _unitOfWork.GenericRepository<Product>();
            _productImageRepo = _unitOfWork.GenericRepository<ProductImage>();
        }
        #endregion

        // GET: Products
        [AllowAnonymous]
        public async Task<ActionResult> Index(string category, string search, string sortBy, int? page)
        {
            ProductIndexViewModel viewModel = new ProductIndexViewModel();

            var products = _productRepo.GetTable().Include(p => p.Category);

            if (!string.IsNullOrWhiteSpace(search))
            {
                products = SearchProductsFor(search, products);
                viewModel.Search = search;
            }
            
            GroupSearchIntoCategoriesAndCountPerCategory(viewModel, products);

            if (!string.IsNullOrWhiteSpace(category))
            {
                products = products.Where(p => p.Category.Name == category);
                viewModel.Category = category;
            }
           
            products = SortProducts(sortBy, products);
      
            await SetViewModelPages(viewModel, products, page);
            SetViewModelSortProperties(sortBy, viewModel);

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
            Product product = _productRepo.Find(id);
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
            viewModel.CategoryList = new SelectList(_categoryRepo.GetTable(), "ID", "Name");
            viewModel.ImageLists = new List<SelectList>();
            for (int i = 0; i < Constants.NumberOfProductImages; i++)
            {
                viewModel.ImageLists.Add(new SelectList(_productImageRepo.GetTable(), "ID","FileName"));
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
            product.Category = _categoryRepo.Find(viewModel.CategoryID);
            product.CategoryID = viewModel.CategoryID;
            product.Description = viewModel.Description;
            product.Price = viewModel.Price;
            product.ProductImageMappings = new List<ProductImageMapping>();
            string[] productImages = viewModel.ProductImages.Where(pi => !string.IsNullOrWhiteSpace(pi)).ToArray();

            for (int i = 0; i < productImages.Length; i++)
            {
                var prodImageMapping = new ProductImageMapping();
                prodImageMapping.ImageNumber = i;
                prodImageMapping.ProductImage = _productImageRepo.Find(int.Parse(productImages[i]));

                product.ProductImageMappings.Add(prodImageMapping);
            }

            if (ModelState.IsValid)
            {
                _productRepo.Add(product);
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }
            var allCategories = _categoryRepo.GetTable();

            viewModel.CategoryList = new SelectList(allCategories, "ID", "Name", product.CategoryID);
            viewModel.ImageLists = new List<SelectList>();
            for (int i = 0; i < Constants.NumberOfProductImages; i++)
            {
                viewModel.ImageLists.Add(new SelectList(_productImageRepo.GetTable(), "ID", "FileName", viewModel.ProductImages[i]));
            }
            ViewBag.CategoryID = new SelectList(allCategories, "ID", "Name", product.CategoryID);

            return View(product);
        }

        // GET: Products/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = _productRepo.Find(id);
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
            var productToUpdate = _productRepo.GetTable().Include(p => p.ProductImageMappings).Single(p => p.ID == viewModel.ID);

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
               
                if (productToUpdate.ProductImageMappings == null)
                {
                    productToUpdate.ProductImageMappings = new List<ProductImageMapping>();
                }

                string[] submitedImages = viewModel.ProductImages.Where(pi => !string.IsNullOrEmpty(pi)).ToArray();
                for (int i = 0; i < submitedImages.Length; i++)
                {
                    var imageMappingToEdit =
                        productToUpdate.ProductImageMappings.FirstOrDefault(pi => pi.ImageNumber == i);
                    var image = _productImageRepo.Find(int.Parse(submitedImages[i]));

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
                        productToUpdate.ProductImageMappings.FirstOrDefault(pim => pim.ImageNumber == i);
                    //if there is something stored in the mapping
                    if (imageMappingToEdit != null)
                    {
                        //delete the record from the mapping table directly.
                        //just calling productToUpdate.ProductImageMappings.Remove(imageMappingToEdit)
                        //results in a FK error
                        _unitOfWork.GenericRepository<ProductImageMapping>().Delete(imageMappingToEdit);
                    }
                }

                _productRepo.SetOriginalValueRowVersion(productToUpdate, viewModel.RowVersion);
                if(_unitOfWork.Save(ModelState, productToUpdate, VerifyProductFields))
                    return RedirectToAction("Index");

                var retViewModel = SetViewModel(viewModel, productToUpdate);
                return View(retViewModel);
            }  
             
            return View(viewModel);
        }

        private void VerifyProductFields(Product databaseProductValues, Product uiFilledProductValues, Product productToUpdate)
        {
            if (databaseProductValues.Name != uiFilledProductValues.Name)
            {
                ModelState.AddModelError("Name", "Current value in database: " + databaseProductValues.Name);
            }
            if (databaseProductValues.Description != uiFilledProductValues.Description)
            {
                ModelState.AddModelError("Description", "Current value in database: " + databaseProductValues.Description);
            }
            if (databaseProductValues.CategoryID != uiFilledProductValues.CategoryID)
            {
                ModelState.AddModelError("CategoryID", "Current value in database: " + databaseProductValues.CategoryID);
            }
            if (databaseProductValues.Price != uiFilledProductValues.Price)
            {
                ModelState.AddModelError("Price", "Current value in database: " + databaseProductValues.Price);
            }

            ModelState.AddModelError(string.Empty, "The record has been modified by another user after you loaded the screen.Your changes have not yet been saved. "
                   + "The new values in the database are shown below. If you want to overwrite these values with your changes then click save otherwise go back to the categories page.");

            productToUpdate.RowVersion = databaseProductValues.RowVersion;
            //var dbMapping = databaseProductValues.ProductImageMappings.ToList();//TODO: how do i retrtieve data for image mapping to verify the changes
            //var uiMapping = uiFilledProductValues.ProductImageMappings.ToList();

            //if (Enumerable.SequenceEqual(dbMapping, uiMapping))
            //{
            //    ModelState.AddModelError("Price", "Current value in database: " + databaseProductValues.Price);
            //}    
        }

        // GET: Products/Delete/5
        public ActionResult Delete(int? id, bool? deletionError)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = _productRepo.Find(id);
            if (product == null)
            {
                if (deletionError.GetValueOrDefault())
                {
                    return RedirectToAction("Index");
                }
                return HttpNotFound();
            }

            if (deletionError.GetValueOrDefault())
            {
                ModelState.AddModelError(string.Empty, "The product you attempted to delete has been modified by another user after you loaded it. " +
                    "The delete has not been performed.The current values in the database are shown above. " +
                    "If you still want to delete this record click the Delete button again, otherwise go back to the products page.");
            }
            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(Product product)
        {
            try
            {
                _productRepo.Delete(product);
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                return RedirectToAction("Delete", new { deletionError = true, id = product.ID });
            }
        }
        
        #region Private methods

        private static IQueryable<Product> SearchProductsFor(string search, IQueryable<Product> products)
        {
            return products.Where(
                p => p.Name.Contains(search) || p.Category.Name.Contains(search) || p.Description.Contains(search));
        }

        private static void GroupSearchIntoCategoriesAndCountPerCategory(ProductIndexViewModel viewModel, IQueryable<Product> products)
        {
            viewModel.CatsWithCount = from matchingProducts in products
                                      where
                                          matchingProducts.CategoryID != null
                                      group matchingProducts by
                                          matchingProducts.Category.Name
                into
                    catGroup
                                      select new CategoryWithCount()
                                      {
                                          CategoryName = catGroup.Key,
                                          ProductCount = catGroup.Count()
                                      };
        }

        private ProductViewModel SetViewModel(ProductViewModel viewModel, Product productToUpdate)
        {
            var retViewModel = new ProductViewModel();
            retViewModel.Name = viewModel.Name;
            retViewModel.Description = viewModel.Description;
            retViewModel.CategoryID = (int)viewModel.CategoryID;
            retViewModel.Price = viewModel.Price;
            retViewModel.RowVersion = productToUpdate.RowVersion;

            retViewModel.CategoryList = new SelectList(_categoryRepo.GetTable(), "ID", "Name", viewModel.CategoryID);
            retViewModel.ImageLists = new List<SelectList>();

            var allProductImages = _productImageRepo.GetTable();
            foreach (var imageMapping in productToUpdate.ProductImageMappings.OrderBy(pi => pi.ImageNumber))
            {
                retViewModel.ImageLists.Add(new SelectList(allProductImages, "ID", "FileName", imageMapping.ProductImageID));
            }
            for (int i = retViewModel.ImageLists.Count; i < Constants.NumberOfProductImages; i++)
            {
                retViewModel.ImageLists.Add(new SelectList(allProductImages, "ID", "FileName"));//BUG: db.productImages instead of returning a list of productImages, he returns a list with selected image, check front end, this line seems fine
            }

            return retViewModel;
        }

        private ProductViewModel MapProductToViewModel(Product product)
        {
            var viewModel = new ProductViewModel();
            viewModel.Name = product.Name;
            viewModel.Description = product.Description;
            viewModel.CategoryID = (int)product.CategoryID;
            viewModel.Price = product.Price;
            viewModel.RowVersion = product.RowVersion;

            viewModel.CategoryList = new SelectList(_categoryRepo.GetTable(), "ID", "Name", product.CategoryID);
            viewModel.ImageLists = new List<SelectList>();

            var allProductImages = _productImageRepo.GetTable();
            foreach (var imageMapping in product.ProductImageMappings.OrderBy(pi => pi.ImageNumber))
            {
                viewModel.ImageLists.Add(new SelectList(allProductImages, "ID", "FileName", imageMapping.ProductImageID));
            }
            for (int i = viewModel.ImageLists.Count; i < Constants.NumberOfProductImages; i++)
            {
                viewModel.ImageLists.Add(new SelectList(allProductImages, "ID", "FileName"));//BUG: db.productImages instead of returning a list of productImages, he returns a list with selected image, check front end, this line seems fine
            }

            return viewModel;
        }

        private static IQueryable<Product> SortProducts(string sortBy, IQueryable<Product> products)
        {
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
            return products;
        }

        private static void SetViewModelSortProperties(string sortBy, ProductIndexViewModel viewModel)
        {
            viewModel.SortBy = sortBy;
            viewModel.Sorts = new Dictionary<string, string>
            {
                {"Price low to high", "price_lowest"},
                {"Price high to low", "price_highest"}
            };
        }

        private static async Task SetViewModelPages(ProductIndexViewModel viewModel, IQueryable<Product> products, int? page)
        {
            int currentPage = (page ?? 1);
            viewModel.CurrentPage = currentPage;
            viewModel.TotalNumberOfProducts = products.Count();
            viewModel.TotalPages = (int)Math.Ceiling((decimal)viewModel.TotalNumberOfProducts / Constants.PageItems);
            viewModel.currentPageOfProducts = await products.ReturnPages(currentPage, Constants.PageItems);
        }

        protected override void Dispose(bool disposing)
        {
            _unitOfWork.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnException(ExceptionContext filterContext)
        {
            Log.Error("Exception occured with message: {Message}", filterContext.Exception.Message);
            Log.Error("Stacktrace: {StackTrace}", filterContext.Exception.StackTrace);
            base.OnException(filterContext);
        }


        #endregion
    }
}
