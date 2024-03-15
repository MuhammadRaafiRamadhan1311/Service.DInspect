using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Newtonsoft.Json;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.Enum;
using Service.DInspect.Repositories;
using Service.DInspect.Helpers;
using System;
using System.Threading.Tasks;

namespace Service.DInspect.Services
{
    public class MasterSettingService : ServiceBase
    {
        public MasterSettingService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            _repository = new MasterSettingRepository(connectionFactory, container);
        }

        public virtual async Task<ServiceResult> GetServerTime()
        {
            try
            {
                var result = string.Empty;
                await Task.Run(() => result = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime));

                return new ServiceResult
                {
                    Message = "Get server time successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Message = ex.Message,
                    IsError = true
                };
            }
        }

        //public virtual async Task<ServiceResult> GetAppSetting(string key)
        //{
        //    string result = string.Empty;

        //    var settingData = JsonConvert.DeserializeObject<AppSettingResponse>(JsonConvert.SerializeObject(_appSetting));
        //    await Task.Run(() => result = StaticHelper.GetPropValue(JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(settingData)), key));

        //    return new ServiceResult
        //    {
        //        Message = "Get application settings successfully",
        //        IsError = false,
        //        Content = result
        //    };
        //}
    }
}
