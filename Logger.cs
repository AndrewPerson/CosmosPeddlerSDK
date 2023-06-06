namespace CosmosPeddler.SDK;

public interface ICosmosLogger
{
    public abstract void Info(string message);
    public abstract void Warning(string message);
    public abstract void Error(string message);
}