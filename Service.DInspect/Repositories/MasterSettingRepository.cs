using Service.DInspect.Interfaces;

namespace Service.DInspect.Repositories
{
    public class MasterSettingRepository : RepositoryBase
    {
        public MasterSettingRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}
