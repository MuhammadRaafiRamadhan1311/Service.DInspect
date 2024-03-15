using Service.DInspect.Interfaces;

namespace Service.DInspect.Repositories
{
    public class DefectHeaderRepository : RepositoryBase
    {
        public DefectHeaderRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}