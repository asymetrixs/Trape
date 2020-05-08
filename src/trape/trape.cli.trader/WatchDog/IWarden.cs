namespace trape.cli.trader.WatchDog
{
    /// <summary>
    /// Interface for <c>Warden</c>
    /// </summary>
    public interface IWarden
    {
        void Add(IActive watchMe);

        void Remove(IActive watchMe);

        void Start();

        void Terminate();
    }
}
