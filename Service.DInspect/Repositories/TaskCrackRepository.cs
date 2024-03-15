using Service.DInspect.Interfaces;

namespace Service.DInspect.Repositories
{
    public class TaskCrackRepository : RepositoryBase
    {
        public TaskCrackRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}
