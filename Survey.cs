using CosmosPeddler.SDK.Internal;
using SurveyData = CosmosPeddler.SDK.Internal.Survey;

namespace CosmosPeddler.SDK;

public record struct Survey
(
    string Signature,
    string Symbol,
    Deposit[] Deposits,
    DateTimeOffset Expiration,
    SurveySize Size
)
{
    public static Survey FromInternal(SurveyData data) => new
    (
        Signature: data.Signature,
        Symbol: data.Symbol,
        Deposits: data.Deposits.Select(Deposit.FromInternal).ToArray(),
        Expiration: data.Expiration,
        Size: data.Size
    );
}

public record struct Deposit
(
    string Symbol
)
{
    public static Deposit FromInternal(SurveyDeposit data) => new
    (
        Symbol: data.Symbol
    );
}