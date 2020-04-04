using System.Data.Entity;
using BabyStore.Models.BabyStoreModelClasses;

namespace BabyStore.DAL
{
    public class StoreContext : DbContext
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductImageMapping> ProductImageMappings { get; set; }

        public System.Data.Entity.DbSet<BabyStore.ViewModel.Security.EditUserViewModel> EditUserViewModels { get; set; }
    }
}