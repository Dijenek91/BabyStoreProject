using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using BabyStore.DAL;
using BabyStore.Models.Orders;
using BabyStore.RepositoryLayer.UnitOfWork;
using BabyStore.ViewModel.Products;

namespace BabyStore.Controllers
{
    //[RequireHttps]
    public class HomeController : Controller
    {
        private readonly IUnitOfWork<StoreContext> _unitOfWork;

        public HomeController()
        {
            _unitOfWork = new GenericUnitOfWork<StoreContext>(); 
        }

        public HomeController(IUnitOfWork<StoreContext> unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ActionResult> Index()
        {
            return View(await GetTopSellers());
        }

        public ActionResult About(string id)
        {
            ViewBag.Message = "Your application description page.Your entered the id: " + id;

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        private async Task<List<BestSellersViewModel>> GetTopSellers()
        {
            var _orderlinesTable = _unitOfWork.GenericRepository<OrderLine>().GetTable();
            var topSellers = (from topProducts in _orderlinesTable
                              where (topProducts.ProductID != null)
                              group topProducts by topProducts.Product
                into topGroup
                              select new BestSellersViewModel
                              {
                                  Product = topGroup.Key,
                                  SalesCount = topGroup.Sum(o => o.Quantity),
                                  ProductImage =
                                      topGroup.Key.ProductImageMappings.OrderBy(pim => pim.ImageNumber).FirstOrDefault().ProductImage.FileName
                              }).OrderByDescending(tg => tg.SalesCount).Take(4);

            return await topSellers.ToListAsync();
        }

        protected override void Dispose(bool disposing)
        {
            _unitOfWork.Dispose();
            base.Dispose(disposing);
        }
    }
}