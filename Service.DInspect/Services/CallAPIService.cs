using Newtonsoft.Json.Serialization;
using Service.DInspect.Helpers;
using Service.DInspect.Models;
using Service.DInspect.Models.Enum;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.DInspect.Services
{
    public class CallAPIService
    {
        private MySetting _appSetting { get; }
        private string _accessToken { get; }

        public CallAPIService(MySetting appSetting, string accessToken)
        {
            _appSetting = appSetting;
            _accessToken = accessToken;
        }

        #region ADM

        public async Task<dynamic> GetUoM()
        {
            CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
            ApiResponse response = await callAPI.Get(EnumUrl.GetUoM);

            return response.Result.Content;
        }

        public async Task<dynamic> GetEquipmentNumber(string unitNumber)
        {
            CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
            ApiResponse response = await callAPI.Get($"{EnumUrl.GetEquipmentNumber}?EquipmentNumber={unitNumber}&Page=1&PageSize=1&ver=v1");

            return response.Result.Content;
        }

        public async Task<dynamic> GetShift()
        {
            CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
            ApiResponse response = await callAPI.Get($"{EnumUrl.GetShift}");

            return response.Result.Content;
        }

        public async Task<dynamic> GetEquipmentAssignment(string equipment)
        {
            CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
            ApiResponse response = await callAPI.Get($"{EnumUrl.GetMasterEquipmentAssignment}&equipment={equipment}");

            return response.Result.Content;
        }

        #endregion

        #region EHMS

        public async Task<dynamic> EHMSPost(string controller, object content)
        {
            CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
            ApiResponse response = await callAPI.Post(EnumUrl.EHMSBaseUrl.Replace(EnumCommonProperty.Controller, controller), content);

            return response.Result.Content;
        }

        public async Task<dynamic> EHMSPut(string controller, object content)
        {
            CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
            ApiResponse response = await callAPI.Put(EnumUrl.EHMSBaseUrl.Replace(EnumCommonProperty.Controller, controller), content);

            return response.Result.Content;
        }

        public async Task<dynamic> EHMSGetAll(string controller)
        {
            CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
            ApiResponse response = await callAPI.Get(EnumUrl.EHMSBaseUrl.Replace(EnumCommonProperty.Controller, controller));

            return response.Result.Content;
        }

        public async Task<dynamic> EHMSGetById(string controller, string id)
        {
            CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
            ApiResponse response = await callAPI.Get(EnumUrl.EHMSGetByIdUrl.Replace(EnumCommonProperty.Controller, controller).Replace(EnumCommonProperty.Id, id));

            return response.Result.Content;
        }

        public async Task<dynamic> EHMSGetAllByParam(string controller, Dictionary<string, object> param)
        {
            CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
            ApiResponse response = await callAPI.Post(EnumUrl.EHMSGetAllByParamUrl.Replace(EnumCommonProperty.Controller, controller), param);

            return response.Result.Content;
        }

        public async Task<dynamic> EHMSGetByParam(string controller, Dictionary<string, object> param)
        {
            CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
            ApiResponse response = await callAPI.Post(EnumUrl.EHMSGetByParamUrl.Replace(EnumCommonProperty.Controller, controller), param);

            return response.Result.Content;
        }

        public async Task<dynamic> GetSyncIntervention(string keyPbi)
        {
            CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
            ApiResponse response = await callAPI.Get(EnumUrl.GetSyncInterventionUrl.Replace(EnumCommonProperty.KeyPbi, keyPbi));

            return response.Result.Content;
        }

        public async Task<dynamic> UpdateEHMSInterventionHeader(object content, string userAccount)
        {
            CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
            ApiResponse response = await callAPI.Post(EnumUrl.UpdateEHMSInterventionHeaderUrl.Replace(EnumCommonProperty.UserAccount, userAccount), content);

            return response.Result.Content;
        }

        public async Task<dynamic> GetInterventionHeader(string siteId = null)
        {
            CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
            ApiResponse response = await callAPI.Get(EnumUrl.GetInterventionList + $"&siteId={siteId}");

            return response.Result.Content;
        }

        public async Task<dynamic> GetTaskTypeCondition(string typeTaskId, string typeTask)
        {
            CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
            ApiResponse response = await callAPI.Get(EnumUrl.GetTaskTypeCondition.Replace(EnumCommonProperty.TypeTaskId, typeTaskId).Replace(EnumCommonProperty.TypeTask, typeTask));

            return response.Result.Content;
        }

        public async Task<dynamic> EhmsGetInterventionForm(string keyPbi)
        {
            CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
            ApiResponse response = await callAPI.Get(EnumUrl.EhmsGetInterventionForm.Replace(EnumCommonProperty.KeyPbi, keyPbi));

            return response.Result.Content;
        }

        public async Task<dynamic> GetInterventionComponentSystem()
        {
            CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
            ApiResponse response = await callAPI.Get(EnumUrl.GetInterventionComponentSystem);

            return response.Result.Content;
        }

        public async Task<dynamic> AdmGetMasterSos(string equipment)
        {
            CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
            ApiResponse response = await callAPI.Get(EnumUrl.GetMasterSos.Replace(EnumCommonProperty.Equipment, equipment));

            return response.Result.Content;
        }

        public async Task<dynamic> EHMSDeleteInterventionDefect(object content)
        {
            CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
            ApiResponse response = await callAPI.Post(EnumUrl.DeleteInterventionDefect, content);

            return response.Result.Content;
        }

        #endregion

        #region Utility
        public async Task<dynamic> GetUserMenu(string employeeId)
        {
            CallAPIHelper callAPI = new CallAPIHelper(_accessToken);
            ApiResponse response = await callAPI.Get($"{EnumUrl.GetUserMenu}&employeeid={employeeId}");

            return response.Result.Content;
        }
        #endregion
    }
}