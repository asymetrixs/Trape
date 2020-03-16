namespace trape.cli.trader.Decision
{
	public interface IDecisionMaker
	{
		void ConfirmBuy(string symbol);

		int Recommendation(string symbol);

		void Start();

		void Stop();
	}
}
