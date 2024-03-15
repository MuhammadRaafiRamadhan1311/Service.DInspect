using Service.DInspect.Interfaces;

namespace Service.DInspect.Repositories
{
    public class InterventionDefectHeaderRepository : RepositoryBase
    {
        public InterventionDefectHeaderRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}