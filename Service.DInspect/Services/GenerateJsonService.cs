using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Repositories;

namespace Service.DInspect.Services
{
    public class GenerateJsonService : ServiceBase
    {
        protected string _container;
        protected IConnectionFactory _connectionFactory;

        public GenerateJsonService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            _container = container;
            _connectionFactory = connectionFactory;
            _repository = new GenerateJsonRepository(connectionFactory, container);
        }
    }
}
