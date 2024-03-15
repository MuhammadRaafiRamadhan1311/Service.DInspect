using Service.DInspect.Interfaces;

namespace Service.DInspect.Repositories
{
    public class TaskCalibrationRepository : RepositoryBase
    {
        public TaskCalibrationRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}
