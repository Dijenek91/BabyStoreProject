using System.Collections;

namespace BabyStore.Models.BabyStoreModelClasses
{
    public class ProductImageMapping : IEqualityComparer
    {
        public int ID { get; set; }
        public int ImageNumber { get; set; }
        public int ProductID { get; set; }
        public int ProductImageID { get; set; }

        public virtual Product Product { get; set; }
        public virtual ProductImage ProductImage { get; set; }

        public bool Equals(object x, object y)
        {
            if (((ProductImageMapping)x).ProductImageID != ((ProductImageMapping)y).ProductImageID)
            {
                return false;
            }
            return true;
        }

        public int GetHashCode(object obj)
        {
            throw new System.NotImplementedException();
        }
    }
}