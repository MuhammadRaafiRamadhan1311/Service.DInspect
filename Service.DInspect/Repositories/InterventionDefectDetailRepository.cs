using Service.DInspect.Interfaces;

namespace Service.DInspect.Repositories
{
    public class InterventionDefectDetailRepository : RepositoryBase
    {
        public InterventionDefectDetailRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}