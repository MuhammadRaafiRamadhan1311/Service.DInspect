using Service.DInspect.Interfaces;

namespace Service.DInspect.Repositories
{
    public class CalibrationDetailRepository : RepositoryBase
    {
        public CalibrationDetailRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}
