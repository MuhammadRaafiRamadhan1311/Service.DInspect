using Service.DInspect.Interfaces;

namespace Service.DInspect.Repositories
{
    public class CalibrationHeaderRepository : RepositoryBase
    {
        public CalibrationHeaderRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}
