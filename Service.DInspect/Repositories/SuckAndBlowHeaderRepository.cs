using Service.DInspect.Interfaces;

namespace Service.DInspect.Repositories
{
    public class SuckAndBlowHeaderRepository : RepositoryBase
    {
        public SuckAndBlowHeaderRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}
