using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Repositories;

namespace Service.DInspect.Services
{
    public class MasterReferenceService : ServiceBase
    {
        public MasterReferenceService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            _repository = new TaskTemplateRepository(connectionFactory, container);
        }
    }
}