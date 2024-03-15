using Service.DInspect.Interfaces;

namespace Service.DInspect.Repositories
{
    public class TempMasterServiceSheetRepository : RepositoryBase
    {
        public TempMasterServiceSheetRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}
