using Service.DInspect.Interfaces;
using Service.DInspect.Models.Enum;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Service.DInspect.Repositories
{
    public class TaskTandemRepository : RepositoryBase
    {
        public TaskTandemRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }
    }
}
