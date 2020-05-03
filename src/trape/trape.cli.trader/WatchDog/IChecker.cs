namespace trape.cli.trader.WatchDog
{
    /// <summary>
    /// Interface for <c>Checker</c>
    /// </summary>
    public interface IChecker
    {
        void Add(IActive watchMe);

        void Remove(IActive watchMe);

        void Start();

        void Terminate();
    }
}
