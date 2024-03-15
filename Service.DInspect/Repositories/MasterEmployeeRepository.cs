using Service.DInspect.Interfaces;

namespace Service.DInspect.Repositories
{
    public class MasterEmployeeRepository : RepositoryBase
    {
        public MasterEmployeeRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}
