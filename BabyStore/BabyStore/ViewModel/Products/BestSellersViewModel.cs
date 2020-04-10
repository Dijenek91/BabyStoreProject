using BabyStore.Models.BabyStoreModelClasses;

namespace BabyStore.ViewModel.Products
{
    public class BestSellersViewModel
    {
        public Product Product { get; set; }
        public int SalesCount { get; set; }
        public string ProductImage { get; set; }
    }
}