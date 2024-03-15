using Service.DInspect.Interfaces;

namespace Service.DInspect.Repositories
{
    public class PsTypeSettingRepository : RepositoryBase
    {
        public PsTypeSettingRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}