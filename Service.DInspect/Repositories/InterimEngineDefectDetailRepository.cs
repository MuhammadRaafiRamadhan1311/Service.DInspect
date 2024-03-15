using Service.DInspect.Interfaces;

namespace Service.DInspect.Repositories
{
    public class InterimEngineDefectDetailRepository : RepositoryBase
    {
        public InterimEngineDefectDetailRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}
