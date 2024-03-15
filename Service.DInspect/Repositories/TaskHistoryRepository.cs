using Service.DInspect.Interfaces;

namespace Service.DInspect.Repositories
{
    public class TaskHistoryRepository : RepositoryBase
    {
        public TaskHistoryRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}