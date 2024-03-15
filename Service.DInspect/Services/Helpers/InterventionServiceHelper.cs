using Microsoft.AspNetCore.JsonPatch.Internal;
using Newtonsoft.Json;
using Service.DInspect.Helpers;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.EHMS;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Helper;
using Service.DInspect.Services;
using Service.DInspect.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class InterventionServiceHelper
{
    private MySetting _appSetting;
    private string _accessToken;
    private IRepositoryBase _settingRepository;

    public InterventionServiceHelper(MySetting appSetting, IConnectionFactory connectionFactory, string accessToken)
    {
        _appSetting = appSetting;
        _accessToken = accessToken;
        _settingRepository = new InterventionRepository(connectionFactory, EnumContainer.MasterSetting);
    }

    public async Task<List<InterventionHeaderListModel>> GetInterventionList(string siteId = null)
    {
        // if siteId == HO Site then skip filter by site
        CallAPIHelper callAPIHelperFilter = new CallAPIHelper(_accessToken);
        var filterRes = await callAPIHelperFilter.Get(EnumUrl.GetGeneralFilter + $"?group=site&ver=v1");
        IList<GeneralFilterHelperModel> filters = JsonConvert.DeserializeObject<List<GeneralFilterHelperModel>>(JsonConvert.SerializeObject(filterRes.Result.Content));

        if (filters.Any(x => x.Value == siteId))
            siteId = null;

        CallAPIService callAPI = new CallAPIService(_appSetting, _accessToken);
        dynamic InterventionHeaderResult = await callAPI.GetInterventionHeader(siteId);

        Dictionary<string, object> settingParam = new Dictionary<string, object>();
        settingParam.Add(EnumQuery.Key, EnumQuery.InterventionMaxEstDate);

        var setting = await _settingRepository.GetDataByParam(settingParam);
        var maxDay = setting[EnumQuery.Value];
        DateTime curentDate = EnumCommonProperty.CurrentDateTime.AddDays(Convert.ToInt32(maxDay));

        List<InterventionHeaderListModel> interventions = JsonConvert.DeserializeObject<List<InterventionHeaderListModel>>(JsonConvert.SerializeObject(InterventionHeaderResult));

        foreach (var item in interventions)
        {
            item.equipmentDesc = $"{item.equipmentBrand} {item.equipmentModel}";
        }

        var result = interventions.Where(x => x.estimationCompletionDate <= curentDate).OrderBy(x => x.estimationCompletionDate).ToList();
        return result;
    }
}