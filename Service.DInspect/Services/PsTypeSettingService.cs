using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Repositories;

namespace Service.DInspect.Services
{
    public class PsTypeSettingService : ServiceBase
    {
        public PsTypeSettingService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            _repository = new PsTypeSettingRepository(connectionFactory, container);
        }
    }
}