using Service.DInspect.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Repositories
{
    public class LubricantMappingRepository : RepositoryBase
    {
        public LubricantMappingRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}
