using Service.DInspect.Interfaces;

namespace Service.DInspect.Repositories
{
    public class TaskCbmDefaultValueRepository : RepositoryBase
    {
        public TaskCbmDefaultValueRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}