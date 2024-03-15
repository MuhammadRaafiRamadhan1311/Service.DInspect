using Service.DInspect.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Repositories
{
    public class HisInterventionSyncRepository : RepositoryBase
    {
        public HisInterventionSyncRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {

        }
    }
}
