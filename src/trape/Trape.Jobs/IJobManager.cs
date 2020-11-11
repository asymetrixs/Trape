namespace Trape.Jobs
{
    public interface IJobManager
    {
        void Start(IJob job);

        void StartAll();

        void StopAll();

        void TerminateAll();
    }
}
