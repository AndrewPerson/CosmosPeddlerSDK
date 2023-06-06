using CosmosPeddler.SDK.Internal;

namespace CosmosPeddler.SDK;

public record struct Market
(
    TradeGood[] Goods
)
{
    public static Market FromInternal(CosmosPeddler.SDK.Internal.Market data)
    {
        var prices = data.TradeGoods.ToDictionary(x => x.Symbol, x => (x.PurchasePrice, x.SellPrice));

        return new
        (
            Goods: data.Exports.Select(export => new TradeGood(export.Symbol, prices[export.Symbol.ToString()].PurchasePrice, null)).Concat(
                data.Imports.Select(import => new TradeGood(import.Symbol, null, prices[import.Symbol.ToString()].SellPrice)).Concat(
                    data.Exchange.Select(exchange => new TradeGood(exchange.Symbol, prices[exchange.Symbol.ToString()].PurchasePrice, prices[exchange.Symbol.ToString()].SellPrice))
                )
            ).ToArray()
        );
    }
}

public record struct TradeGood
(
    TradeSymbol Symbol,
    int? BuyPrice,
    int? SellPrice
);