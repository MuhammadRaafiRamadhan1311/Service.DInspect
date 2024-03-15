using Service.DInspect.Interfaces;

namespace Service.DInspect.Repositories
{
    public class GenerateJsonRepository : RepositoryBase
    {
        public GenerateJsonRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}
