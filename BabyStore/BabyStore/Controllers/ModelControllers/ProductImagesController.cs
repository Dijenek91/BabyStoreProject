using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BabyStore.DAL;
using BabyStore.FileOperations;
using BabyStore.Models.BabyStoreModelClasses;
using BabyStore.RepositoryLayer;
using BabyStore.RepositoryLayer.UnitOfWork;

namespace BabyStore.Controllers.ModelControllers
{
    public class ProductImagesController : Controller
    {
        private readonly IUnitOfWork<StoreContext> _unitOfWork = new GenericUnitOfWork<StoreContext>();
        private readonly IGenericRepository<ProductImage> _productImageRepo;

        public ProductImagesController()
        {
            _productImageRepo = _unitOfWork.GenericRepository<ProductImage>();
        }

        public ProductImagesController(IUnitOfWork<StoreContext> unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _productImageRepo = _unitOfWork.GenericRepository<ProductImage>();
        }

        // GET: ProductImages
        public ActionResult Index()
        {
            return View(_productImageRepo.GetAllRecords());
        }

        // GET: ProductImages/Create
        public ActionResult Upload()
        {
            return View();
        }

        // POST: ProductImages/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Upload(HttpPostedFileBase[] files)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            FileHandler.SaveFiles(files, ModelState);

            bool duplicates = false;
            bool otherDbError = false;
            string duplicateFiles = "";

            foreach (var file in files)
            {
                //try and save each file
                var imageToAdd = new ProductImage {FileName = file.FileName};
                try
                {
                    _productImageRepo.Add(imageToAdd);
                    _unitOfWork.Save();
                }
                    //if there is an exception check if it is caused by a duplicate file
                catch (DbUpdateException ex)
                {
                    SqlException innerException = ex.InnerException.InnerException as SqlException;
                    if (innerException != null && innerException.Number == 2601)
                    {
                        duplicateFiles += ", " + file.FileName;
                        duplicates = true;
                        _productImageRepo.DetachEntry(imageToAdd);
                    }
                    else
                    {
                        otherDbError = true;
                    }
                }
            }
            //add a list of duplicate files to the error message
            if (duplicates)
            {
                ModelState.AddModelError("FileName", "All files uploaded except the files" +
                                                        duplicateFiles + ", which already exist in the system." +
                                                        " Please delete them and try again if you wish to re - add them");
                return View();
            }

            if (otherDbError)
            {
                ModelState.AddModelError("FileName",
                    "Sorry an error has occurred saving to the database, please try again");
                return View();
            }
            return RedirectToAction("Index");

        }

       

        // GET: ProductImages/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ProductImage productImage = _productImageRepo.Find(id);
            if (productImage == null)
            {
                return HttpNotFound();
            }
            return View(productImage);
        }

        // POST: ProductImages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            ProductImage productImage = _productImageRepo.Find(id);
            var imageMappingTable = _unitOfWork.GenericRepository<ProductImageMapping>().GetTable();

            var mappings = imageMappingTable.Where(pim => pim.ProductImageID == id).ToList();

            SortImageNumbers(mappings, imageMappingTable);

            FileHandler.DeleteFile(productImage, Request);

            _productImageRepo.Delete(productImage);
            _unitOfWork.Save();
            return RedirectToAction("Index");
        }

        private static void SortImageNumbers(List<ProductImageMapping> mappings, IQueryable<ProductImageMapping> imageMappingTable)
        {
            foreach (var mapping in mappings)
            {
                var mappingsToUpdate = imageMappingTable.Where(pim => pim.ProductID == mapping.ProductID);

                foreach (var productMapping in mappingsToUpdate)
                {
                    if (productMapping.ImageNumber > mapping.ImageNumber)
                    {
                        productMapping.ImageNumber--;
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            _unitOfWork.Dispose();
            base.Dispose(disposing);
        }
    }
}
