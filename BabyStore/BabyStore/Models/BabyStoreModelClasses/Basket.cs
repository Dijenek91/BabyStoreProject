using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BabyStore.DAL;
using BabyStore.Models.Orders;

namespace BabyStore.Models.BabyStoreModelClasses
{
    public class Basket
    {
        private string BasketID { get; set; }
        private const string BasketSessionKey = "BasketID";
        private StoreContext db = new StoreContext();

        public static Basket GetBasket()
        {
            Basket basket = new Basket();
            basket.BasketID = basket.GetBasketID();
            return basket;
        }

        public void AddToBasket(int productID, int quantity)
        {
            var basketLine = GetBasketLineFromDbFor(productID);
            if (basketLine == null)
            {
                basketLine = new BasketLine();
                basketLine.ProductID = productID;
                basketLine.Product = db.Products.Find(productID);
                basketLine.BasketID = BasketID;
                basketLine.DateCreated = DateTime.Now;
                basketLine.Quantity = quantity;

                db.BasketLines.Add(basketLine);
            }
            else
            {
                basketLine.Quantity += quantity;
            }
            db.SaveChanges();
        }

        public void RemoveLine(int productID)
        {
            var basketLine = GetBasketLineFromDbFor(productID);
            if (basketLine != null)
            {
                db.BasketLines.Remove(basketLine);
            }
            db.SaveChanges();
        }

        public void UpdateBasket(List<BasketLine> basketLines)
        {
            foreach (var line in basketLines)
            {
                var dbBasketLine = GetBasketLineFromDbFor(line.ProductID);
                if(dbBasketLine != null)
                { 
                    if (line.Quantity == 0)
                    {
                        RemoveLine(line.ProductID);
                    }
                    else
                    {
                        dbBasketLine.Quantity = line.Quantity;
                    }
                }
            }
            db.SaveChanges();
        }

        public void EmptyBasket()
        {
            var basketLines = GetBasketLines();
            foreach (var line in basketLines)
            {
                db.BasketLines.Remove(line);
            }
            db.SaveChanges();
        }

        public List<BasketLine> GetBasketLines()
        {
            return db.BasketLines.Where(b => b.BasketID == BasketID).ToList();
        }

        public decimal GetTotalCost()
        {
            decimal basketTotal = decimal.Zero;
            if (GetBasketLines().Count > 0)
            {
                basketTotal = db.BasketLines.Where(b => b.BasketID == BasketID).Sum(b => b.Product.Price * b.Quantity);
            }
            return basketTotal;
        }

        public int GetNumberOfItems()
        {
            int numberOfItems = 0;
            if (GetBasketLines().Count > 0)
            {
                numberOfItems = db.BasketLines.Where(b => b.BasketID == BasketID).Sum(b => b.Quantity);
            }
            return numberOfItems;
        }

        public void MigrateBasket(string userName)
        {
            var currentBasket = GetBasketLines();

            var userBasket = db.BasketLines.Where(b => b.BasketID == userName).ToList();

            if (userBasket != null)
            {
                string prevID = BasketID;
                BasketID = userName;

                foreach (var line in currentBasket)
                {
                    AddToBasket(line.ProductID, line.Quantity);
                }

                BasketID = prevID;
                EmptyBasket();

            }
            else
            {
                foreach (var line in currentBasket)
                {
                    line.BasketID = userName;
                }
            }
            db.SaveChanges();
            HttpContext.Current.Session[BasketSessionKey] = userName;
        }

        public decimal CreateOrderLines(int orderID)
        {
            decimal orderTotal = 0;
            var basketLines = GetBasketLines();

            foreach (var item in basketLines)
            {
                OrderLine orderLine = new OrderLine
                {
                    Product = item.Product,
                    ProductID = item.ProductID,
                    ProductName = item.Product.Name,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price,
                    OrderID = orderID
                };
                orderTotal += (item.Quantity * item.Product.Price);
                db.OrderLines.Add(orderLine);
            }
            db.SaveChanges();
            EmptyBasket();
            return orderTotal;
        }

        #region Private methods

        private string GetBasketID()
        {
            if (HttpContext.Current.Session[BasketSessionKey] == null)
            {
                if (!string.IsNullOrWhiteSpace(HttpContext.Current.User.Identity.Name))
                {
                    HttpContext.Current.Session[BasketSessionKey] = HttpContext.Current.User.Identity.Name;
                }
                else
                {
                    Guid tempBasketID = Guid.NewGuid();
                    HttpContext.Current.Session[BasketSessionKey] = tempBasketID.ToString();
                }
            }

            return HttpContext.Current.Session[BasketSessionKey].ToString();
        }

        private BasketLine GetBasketLineFromDbFor(int productID)
        {
            return db.BasketLines.FirstOrDefault(b => b.BasketID == BasketID && b.ProductID == productID);
        }

        #endregion
    }
}