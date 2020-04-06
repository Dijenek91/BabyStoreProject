using System.ComponentModel.DataAnnotations;

namespace BabyStore.ViewModel.Basket
{
    public class BasketSummaryViewModel
    {
        public int NumberOfItems { get; set; }

        [DataType(DataType.Currency)]
        [DisplayFormat(DataFormatString = "{0:c}")]
        public decimal TotalCost { get; set; }
    }
}