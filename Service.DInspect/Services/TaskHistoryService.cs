using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Repositories;

namespace Service.DInspect.Services
{
    public class TaskHistoryService : ServiceBase
    {
        public TaskHistoryService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            _repository = new TaskHistoryRepository(connectionFactory, container);
        }
    }
}