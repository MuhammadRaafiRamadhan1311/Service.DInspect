using Service.DInspect.Interfaces;

namespace Service.DInspect.Repositories
{
    public class InterimEngineDefectHeaderRepository : RepositoryBase
    {
        public InterimEngineDefectHeaderRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}
