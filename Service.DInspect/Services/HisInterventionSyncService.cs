using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Services
{
    public class HisInterventionSyncService : ServiceBase
    {
        public HisInterventionSyncService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            _repository = new HisInterventionSyncRepository(connectionFactory, container);
        }
    }
}
