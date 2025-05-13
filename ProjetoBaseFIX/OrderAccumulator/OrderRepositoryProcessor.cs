using EletronicTradingModel;
using EletronicTradingModel.Enum;
using QuickFix.Fields;
using QuickFix.FIX44;

namespace OrderAccumulator
{
    internal class OrderRepositoryProcessor
    {

        #region Singleton
        private static SemaphoreSlim _instanceSemaphore = new SemaphoreSlim(1);
        private static OrderRepositoryProcessor _instance;

        public static OrderRepositoryProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instanceSemaphore.Wait();
                    try
                    {
                        if (_instance == null)
                        {
                            _instance = new OrderRepositoryProcessor();
                        }
                    }
                    finally
                    {
                        _instanceSemaphore.Release();
                    }
                }
                return _instance;
            }
        }
        #endregion

        private const decimal TOTAL_LIMIT = 100_000_000m;

        private SemaphoreSlim _semaphoreCollection;
        private Dictionary<string, SymbolInfo> _symbolCollection;
        // como eu foi pedido somente o valor acumulado das operacoes por simbolo, eu nao estou armazenando as ordens
        private Dictionary<string, decimal> _totalVolumeBySymbol;

        private OrderRepositoryProcessor()
        {
            _semaphoreCollection = new(1);
            _symbolCollection = new();
            _totalVolumeBySymbol = new();

            _symbolCollection.Add("PETR4", new SymbolInfo() { Symbol = "PETR4", PriceIncrement = 0.01m, PriceMin = 0.01m, PriceMax = 1_000m, LotIncrement = 1m, LotMin = 1, LotMax = 100_000m });
            _symbolCollection.Add("VALE3", new SymbolInfo() { Symbol = "VALE3", PriceIncrement = 0.01m, PriceMin = 0.01m, PriceMax = 1_000m, LotIncrement = 1m, LotMin = 1, LotMax = 100_000m });
            _symbolCollection.Add("VIIA4", new SymbolInfo() { Symbol = "VIIA4", PriceIncrement = 0.01m, PriceMin = 0.01m, PriceMax = 1_000m, LotIncrement = 1m, LotMin = 1, LotMax = 100_000m });
        }

        internal Message NewOrder(NewOrderSingle newOrderSingle)
        {
            string clOrdID = newOrderSingle.GetString(ClOrdID.TAG);
            string symbol = newOrderSingle.GetString(Symbol.TAG);
            string ordType = newOrderSingle.GetString(OrdType.TAG);
            int qty = newOrderSingle.GetInt(OrderQty.TAG);
            decimal price = newOrderSingle.GetDecimal(Price.TAG);

            try
            {
                _semaphoreCollection.Wait();
                // ativo nao existe
                if (!_symbolCollection.TryGetValue(symbol, out var symbolData))
                {
                    OrdRejReason ordRejReason = new OrdRejReason(OrdRejReason.UNKNOWN_SYMBOL);
                    return new ExecutionReport()
                    {
                        ClOrdID = newOrderSingle.ClOrdID,
                        OrdStatus = new OrdStatus(OrdStatus.REJECTED),
                        OrdRejReason = ordRejReason,
                        Symbol = newOrderSingle.Symbol,
                        Side = newOrderSingle.Side,
                        OrdType = newOrderSingle.OrdType,
                        TransactTime = newOrderSingle.TransactTime,
                        Text = new Text(ConvertReasonToBeautyString(ordRejReason.Value))
                    };
                }
                // preco fora do incremento
                else if (price % symbolData.PriceIncrement != 0)
                {
                    OrdRejReason ordRejReason = new OrdRejReason(OrdRejReason.INVALID_PRICE_INCREMENT);
                    return new ExecutionReport()
                    {
                        ClOrdID = newOrderSingle.ClOrdID,
                        OrdStatus = new OrdStatus(OrdStatus.REJECTED),
                        OrdRejReason = ordRejReason,
                        Symbol = newOrderSingle.Symbol,
                        Side = newOrderSingle.Side,
                        OrdType = newOrderSingle.OrdType,
                        TransactTime = newOrderSingle.TransactTime,
                        Text = new Text(ConvertReasonToBeautyString(ordRejReason.Value))
                    };
                }
                // preco fora do band
                else if (price < symbolData.PriceMin || price > symbolData.PriceMax)
                {
                    OrdRejReason ordRejReason = new OrdRejReason(OrdRejReason.PRICE_EXCEEDS_CURRENT_PRICE_BAND);
                    return new ExecutionReport()
                    {
                        ClOrdID = newOrderSingle.ClOrdID,
                        OrdStatus = new OrdStatus(OrdStatus.REJECTED),
                        OrdRejReason = ordRejReason,
                        Symbol = newOrderSingle.Symbol,
                        Side = newOrderSingle.Side,
                        OrdType = newOrderSingle.OrdType,
                        TransactTime = newOrderSingle.TransactTime,
                        Text = new Text(ConvertReasonToBeautyString(ordRejReason.Value))
                    };
                }
                // lote fora do band
                else if (qty < symbolData.LotMin || price > symbolData.LotMax)
                {
                    OrdRejReason ordRejReason = new OrdRejReason(OrdRejReason.PRICE_EXCEEDS_CURRENT_PRICE_BAND);
                    return new ExecutionReport()
                    {
                        ClOrdID = newOrderSingle.ClOrdID,
                        OrdStatus = new OrdStatus(OrdStatus.REJECTED),
                        OrdRejReason = ordRejReason,
                        Symbol = newOrderSingle.Symbol,
                        Side = newOrderSingle.Side,
                        OrdType = newOrderSingle.OrdType,
                        TransactTime = newOrderSingle.TransactTime,
                        Text = new Text(ConvertReasonToBeautyString(ordRejReason.Value))
                    };
                }

                else
                {
                    OrderSide side = newOrderSingle.GetChar(Side.TAG) == Side.BUY ? OrderSide.Buy : OrderSide.Sell;
                    decimal volumeOrder = qty * price;

                    // valido se tem ordens do ativo
                    if (!_totalVolumeBySymbol.TryGetValue(symbol, out var volumeTotal))
                        volumeTotal = _totalVolumeBySymbol[symbol] = 0;


                    var novoPossivelVolumeTotal = volumeTotal + (side == OrderSide.Buy ? volumeOrder : -volumeOrder);

                    if (Math.Abs(novoPossivelVolumeTotal) > TOTAL_LIMIT)
                    {
                        OrdRejReason ordRejReason = new OrdRejReason(OrdRejReason.ORDER_EXCEEDS_LIMIT);
                        return new ExecutionReport()
                        {
                            ClOrdID = newOrderSingle.ClOrdID,
                            OrdStatus = new OrdStatus(OrdStatus.REJECTED),
                            OrdRejReason = ordRejReason,
                            Symbol = newOrderSingle.Symbol,
                            Side = newOrderSingle.Side,
                            OrdType = newOrderSingle.OrdType,
                            TransactTime = newOrderSingle.TransactTime,
                            Text = new Text($"Valor limite de operação atingido para o ativo. Limite máximo:{TOTAL_LIMIT}; Atual consumido:{Math.Abs(volumeTotal)}; disponível:{TOTAL_LIMIT - Math.Abs(volumeTotal)}")
                        };
                    }
                    else
                    {

                        _totalVolumeBySymbol[symbol] += side == OrderSide.Buy ? volumeOrder : -volumeOrder;
                        return new ExecutionReport()
                        {
                            ClOrdID = newOrderSingle.ClOrdID,
                            OrdStatus = new OrdStatus(OrdStatus.NEW),
                            Symbol = newOrderSingle.Symbol,
                            Side = newOrderSingle.Side,
                            OrdType = newOrderSingle.OrdType,
                            TransactTime = newOrderSingle.TransactTime
                        };
                    }
                }
            }
            finally
            {
                _semaphoreCollection.Release();
            }
        }

        private string ConvertReasonToBeautyString(int ordRejReason)
        {
            return ordRejReason switch
            {
                OrdRejReason.UNKNOWN_SYMBOL => "Simbolo não existe",
                OrdRejReason.INVALID_PRICE_INCREMENT => "Preço inválido",
                OrdRejReason.PRICE_EXCEEDS_CURRENT_PRICE_BAND => "Ordem fora de banda de preço",
                OrdRejReason.INCORRECT_QUANTITY => "Quantidade inválida",
                OrdRejReason.ORDER_EXCEEDS_LIMIT => "Order exceeds limit",
                _ => "Unknown reason"
            };
        }
    }
}
