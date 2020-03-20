namespace trape.cli.trader.Analyze
{
	public interface IRecommender
	{
		void ConfirmBuy(string symbol);

		int Recommendation(string symbol);

		void Start();

		void Stop();
	}
}
