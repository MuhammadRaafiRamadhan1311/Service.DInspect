using Service.DInspect.Interfaces;

namespace Service.DInspect.Repositories
{
    public class MasterReferenceRepository : RepositoryBase
    {
        public MasterReferenceRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}