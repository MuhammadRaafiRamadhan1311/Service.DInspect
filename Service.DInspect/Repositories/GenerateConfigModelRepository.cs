using Service.DInspect.Interfaces;

namespace Service.DInspect.Repositories
{
    public class GenerateConfigModelRepository : RepositoryBase
    {
        public GenerateConfigModelRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}
