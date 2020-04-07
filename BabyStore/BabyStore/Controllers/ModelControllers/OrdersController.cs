using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using BabyStore.DAL;
using BabyStore.Models.BabyStoreModelClasses;
using BabyStore.Models.Orders;
using Microsoft.AspNet.Identity.Owin;

namespace BabyStore.Controllers.ModelControllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private StoreContext db = new StoreContext();
        private ApplicationUserManager _userManager;

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ??
                HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        // GET: Orders
        public ActionResult Index(string orderSearch, string startDate, string endDate, string orderSortOrder)
        {
            var orders = db.Orders.OrderBy(o => o.DateCreated).Include(o => o.OrderLines);
            if (!User.IsInRole("Admin"))
            {
                orders = orders.Where(o => o.UserID == User.Identity.Name);
            }
            if (!string.IsNullOrWhiteSpace(orderSearch))
            {
                orders = orders.Where(o => o.OrderID.ToString().Equals(orderSearch) ||
                            o.UserID.Contains(orderSearch) || 
                            o.DeliveryName.Contains(orderSearch) ||
                            o.DeliveryAddress.AddressLine1.Contains(orderSearch) ||
                            o.DeliveryAddress.AddressLine2.Contains(orderSearch) ||
                            o.DeliveryAddress.Town.Contains(orderSearch) ||
                            o.DeliveryAddress.County.Contains(orderSearch) ||
                            o.DeliveryAddress.Postcode.Contains(orderSearch) ||
                            o.TotalPrice.ToString().Equals(orderSearch) ||
                            o.OrderLines.Any(ol => ol.ProductName.Contains(orderSearch)));
            }
            DateTime parsedStartDate;
            if (DateTime.TryParse(startDate, out parsedStartDate))
            {
                orders = orders.Where(o => o.DateCreated >= parsedStartDate);
            }
            DateTime parsedEndDate;
            if (DateTime.TryParse(endDate, out parsedEndDate))
            {
                orders = orders.Where(o => o.DateCreated <= parsedEndDate);
            }

            ViewBag.DateSort = String.IsNullOrEmpty(orderSortOrder) ? "date" : "";
            ViewBag.UserSort = orderSortOrder == "user" ? "user_desc" : "user";
            ViewBag.PriceSort = orderSortOrder == "price" ? "price_desc" : "price";
            ViewBag.CurrentOrderSearch = orderSearch;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            switch (orderSortOrder)
            {
                case "user":
                    orders = orders.OrderBy(o => o.UserID);
                    break;
                case "user_desc":
                    orders = orders.OrderByDescending(o => o.UserID);
                    break;
                case "price":
                    orders = orders.OrderBy(o => o.TotalPrice);
                    break;
                case "price_desc":
                    orders = orders.OrderByDescending(o => o.TotalPrice);
                    break;
                case "date":
                    orders = orders.OrderBy(o => o.DateCreated);
                    break;
                default:
                    orders = orders.OrderByDescending(o => o.DateCreated);
                    break;
            }
            return View(orders);
        }

        // GET: Orders/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Order order = db.Orders.Include(o => o.OrderLines).Where(o => o.OrderID == id).SingleOrDefault();
            if (order == null)
            {
                return HttpNotFound();
            }
            if (order.UserID == User.Identity.Name || User.IsInRole("Admin"))
            {
                return View(order);
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            }
        }

        // GET: Orders/Create
        public async Task<ActionResult> Review()
        {
            Basket basket = Basket.GetBasket();
            Order order = new Order();

            order.UserID = User.Identity.Name;//TODO: This can be an initialize order method, and get the logic from here to order class
            var appUser = await UserManager.FindByNameAsync(order.UserID);
            order.DeliveryName = appUser.FirstName + "" + appUser.LastName;
            order.DeliveryAddress = appUser.Address;
            order.OrderLines = new List<OrderLine>();

            foreach (var line in basket.GetBasketLines())
            {
                OrderLine orderLine = new OrderLine()
                {
                    ProductID = line.ProductID,
                    Product = line.Product,
                    ProductName = line.Product.Name,
                    Quantity = line.Quantity,
                    UnitPrice = line.Product.Price
                };
                order.OrderLines.Add(orderLine);
            }

            order.TotalPrice = basket.GetTotalCost();
            return View(order);
        }

        // POST: Orders/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "UserID,DeliveryName,DeliveryAddress")] Order order)
        {
            if (ModelState.IsValid)
            {
                order.DateCreated = DateTime.Now;
                db.Orders.Add(order);
                db.SaveChanges();
                //add the orderlines to the database after creating the order
                Basket basket = Basket.GetBasket();
                order.TotalPrice = basket.CreateOrderLines(order.OrderID);
                db.SaveChanges();
                return RedirectToAction("Details", new { id = order.OrderID });
            }

            return RedirectToAction("Review");
        }

        // GET: Orders/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "OrderID,UserID,DeliveryName,DeliveryAddress,TotalPrice,DateCreated")] Order order)
        {
            if (ModelState.IsValid)
            {
                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(order);
        }

        // GET: Orders/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }
            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Order order = db.Orders.Find(id);
            db.Orders.Remove(order);
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
    }
}
