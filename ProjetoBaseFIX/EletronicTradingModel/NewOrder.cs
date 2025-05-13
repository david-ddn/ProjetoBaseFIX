using EletronicTradingModel.Enum;

namespace EletronicTradingModel
{
    public class NewOrder
    {
        public string Symbol { get; set; }
        public long OrderQty { get; set; }
        public decimal Price { get; set; }
        public OrderSide Side { get; set; }
    }
}
