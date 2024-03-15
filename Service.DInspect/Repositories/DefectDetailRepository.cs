using Service.DInspect.Interfaces;

namespace Service.DInspect.Repositories
{
    public class DefectDetailRepository : RepositoryBase
    {
        public DefectDetailRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}
