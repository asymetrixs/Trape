namespace trape.jobs
{
    public interface IJobManager
    {
        void Start(IJob job);

        void StartAll();

        void StopAll();

        void TerminateAll();
    }
}
