using System.Collections.Generic;

namespace BabyStore.Models.BabyStoreModelClasses
{
    public class Category
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public virtual ICollection<Product> Products { get; set; }
    }
}