using Service.DInspect.Interfaces;

namespace Service.DInspect.Repositories
{
    public class TaskTemplateRepository : RepositoryBase
    {
        public TaskTemplateRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}