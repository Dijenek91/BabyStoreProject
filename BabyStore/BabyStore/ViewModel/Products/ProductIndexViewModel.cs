using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using BabyStore.Models.BabyStoreModelClasses;
using PagedList;

namespace BabyStore.ViewModel.Products
{
    public class ProductIndexViewModel
    {
        public int TotalNumberOfProducts { get; set; }
        public string Search { get; set; }
        public IEnumerable<CategoryWithCount> CatsWithCount { get; set; }
        public string Category { get; set; }
        public string SortBy { get; set; }
        public Dictionary<string,string> Sorts { get; set; }
        public IEnumerable<SelectListItem> CatFilterItems
        {
            get
            {
                var allCats = CatsWithCount.Select(cc => new SelectListItem
                {
                    Value = cc.CategoryName,
                    Text = cc.CatNameWithCount
                });
                return allCats;
            }
        }
        #region Paging
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public List<Product> currentPageOfProducts { get; set; }
        #endregion
    }

    public class CategoryWithCount
    {
        public int ProductCount { get; set; }
        public string CategoryName { get; set; }

        public string CatNameWithCount
        {
            get
            {
                return CategoryName + " (" + ProductCount.ToString() + ")";
            }
        }
    }
}