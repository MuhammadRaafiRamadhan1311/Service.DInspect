using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Service.DInspect.Models;
using Service.DInspect.Models.Enum;
using Service.DInspect.Interfaces;

namespace Service.DInspect.Repositories
{
    public class ConnectionFactory : IConnectionFactory
    {
        private readonly MySetting _appSettings;
        public CosmosClientOptions _option;
        public CosmosClient _client;

        public ConnectionFactory(IOptions<MySetting> appSettings)
        {
            _appSettings = appSettings.Value;
            EnumUrl.appSetting = appSettings.Value;
            EnumCommonProperty.appSetting = appSettings.Value;
            EnumFormatting.appSetting = appSettings.Value;
        }

        // public void Dispose()
        // {
        //     if (_client != null)
        //     {
        //         _client.Dispose();
        //     }

        //     GC.SuppressFinalize(this);
        // }

        public Database GetDatabase()
        {
            _option = new CosmosClientOptions() { ConnectionMode = ConnectionMode.Direct };

            if (_client == null)
            {
                _client = new CosmosClient(_appSettings.ConnectionStrings.CosmosConnection, _option);
            }
            return _client.GetDatabase(_appSettings.ConnectionStrings.DatabaseName);
        }

        //public Database GetDatabase()
        //{
        //    var options = new CosmosClientOptions() { ConnectionMode = ConnectionMode.Gateway };
        //    var client = new CosmosClient(_appSettings.ConnectionStrings.CosmosConnection, options);
        //    var databaseResponse = Task.Run(() => client.CreateDatabaseIfNotExistsAsync(_appSettings.ConnectionStrings.DatabaseName)).Result;
        //    return client.GetDatabase(_appSettings.ConnectionStrings.DatabaseName);
        //}
    }
}
