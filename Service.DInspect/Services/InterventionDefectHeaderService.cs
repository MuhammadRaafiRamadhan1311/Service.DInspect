using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.DInspect.Helpers;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.EHMS;
using Service.DInspect.Models.Entity;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Helper;
using Service.DInspect.Models.Request;
using Service.DInspect.Services.Helpers;
using Service.DInspect.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace Service.DInspect.Services
{
    public class InterventionDefectHeaderService : ServiceBase
    {
        private string _container;
        private IConnectionFactory _connectionFactory;
        private IRepositoryBase _interventionRepository;

        public InterventionDefectHeaderService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            _container = container;
            _connectionFactory = connectionFactory;
            _repository = new InterventionDefectHeaderRepository(connectionFactory, container);
            _interventionRepository = new InterventionRepository(connectionFactory, EnumContainer.Intervention);
        }

        public async Task<ServiceResult> GetInterventionDefect(string supervisor, string userGroup)
        {
            try
            {
                CallAPIService callAPI = new CallAPIService(_appSetting, _accessToken);

                List<InterventionDefectHelperModel> result = new List<InterventionDefectHelperModel>();
                Dictionary<string, object> interventionsParam = new Dictionary<string, object>();

                // get user profile
                CallAPIHelper callAPIHelperEmp = new CallAPIHelper(_accessToken);
                var empRes = await callAPIHelperEmp.Get(EnumUrl.GetDataEmployeeProfileById + $"/{supervisor}?ver=v1");

                IList<EmployeeHelperModel> empProfiles = JsonConvert.DeserializeObject<List<EmployeeHelperModel>>(JsonConvert.SerializeObject(empRes.Result.Content));
                IList<string> empUserGroups = empProfiles.Select(x => x.GroupName.ToLower()).ToList();
                var siteId = empProfiles.FirstOrDefault().SiteId;

                if (empUserGroups.Contains(userGroup.ToLower()) && userGroup.ToLower() == EnumPosition.Supervisor.ToLower())
                {
                    JArray listParam = new JArray();
                    listParam.Add(EnumStatus.EFormOnProgress);
                    listParam.Add(EnumStatus.EFormSubmited);
                    interventionsParam.Add(EnumQuery.InterventionExecution, listParam);
                }
                else if (empUserGroups.Contains(userGroup.ToLower()) && userGroup.ToLower() == EnumPosition.Planner.ToLower())
                {
                    interventionsParam.Add(EnumQuery.InterventionExecution, EnumStatus.EFormClosed);
                    interventionsParam.Add(EnumQuery.DefectStatus, EnumStatus.DefectApprovedSPV);
                }
                else
                {
                    return new ServiceResult
                    {
                        Message = $"User group is not {char.ToUpper(userGroup.First()) + userGroup.Substring(1).ToLower()}",
                        IsError = true
                    };
                }

                interventionsParam.Add(EnumQuery.InterventionStatus, EnumStatus.InterventionAccepted);
                interventionsParam.Add(EnumQuery.IsActive, true.ToString().ToLower());
                interventionsParam.Add(EnumQuery.IsDeleted, false.ToString().ToLower());


                // if siteId == HO Site then skip filter by site
                string groupFilter = EnumGeneralFilterGroup.Site;
                CallAPIHelper callAPIHelperFilter = new CallAPIHelper(_accessToken);
                var filterRes = await callAPIHelperFilter.Get(EnumUrl.GetGeneralFilter + $"?group={groupFilter}&ver=v1");
                IList<GeneralFilterHelperModel> filters = JsonConvert.DeserializeObject<List<GeneralFilterHelperModel>>(JsonConvert.SerializeObject(filterRes.Result.Content));

                if (filters.Any(x => x.Value == siteId))
                    siteId = null;

                if (siteId != null)
                    interventionsParam.Add(EnumQuery.siteId, siteId);

                var interventions = await _interventionRepository.GetDataListByParamJArray(interventionsParam);
                //var defects = await _repository.GetActiveDataJArray();

                var interventionComponentSystemResult = await callAPI.GetInterventionComponentSystem();
                List<InterventionListModel> interventionComponentSystem = JsonConvert.DeserializeObject<List<InterventionListModel>>(JsonConvert.SerializeObject(interventionComponentSystemResult));

                foreach (var jHeader in interventions)
                {
                    //var header = defects.FilterEqual(EnumQuery.InterventionId, jHeader[EnumQuery.ID].ToString()).FirstOrDefault();

                    //if (defects.Count > 0)
                    //{
                    //    string jsonDefects = JsonConvert.SerializeObject(defects);
                    //    List<DefectHelperModel> defectHelpers = JsonConvert.DeserializeObject<List<DefectHelperModel>>(jsonDefects);

                    //    if (defectHelpers.Any(x => x.status != EnumStatus.DefectApprovedSPV && x.status != EnumStatus.DefectCompleted))
                    //    {
                    InterventionDefectHelperModel defect = new InterventionDefectHelperModel();

                    defect.interventionId = jHeader[EnumQuery.ID]?.ToString();
                    defect.interventionKey = jHeader[EnumQuery.Key]?.ToString();
                    defect.equipmentDesc = jHeader[EnumQuery.EquipmentDesc]?.ToString();
                    defect.componentDescription = jHeader[EnumQuery.ComponentDescription]?.ToString();
                    defect.interventionDiagnosis = jHeader[EnumQuery.InterventionDiagnosis]?.ToString();
                    defect.sapWorkOrder = jHeader[EnumQuery.SapWorkOrder]?.ToString();
                    defect.equipment = jHeader[EnumQuery.Equipment]?.ToString();
                    defect.sampleStatus = jHeader[EnumQuery.SampleStatus]?.ToString();
                    defect.interventionReason = jHeader[EnumQuery.InterventionReason]?.ToString();
                    defect.componentSystem = interventionComponentSystem.Where(x => x.KeyPbi == jHeader[EnumQuery.KeyPbi]?.ToString()).FirstOrDefault()?.ComponentGroup;
                    defect.intFormStatus = jHeader[EnumQuery.InterventionExecution]?.ToString();
                    defect.defectStatus = jHeader[EnumQuery.DefectStatus]?.ToString();

                    //        int countNeedAct = defectHelpers.Where(x => (x.category == EnumTaskType.Normal && x.taskValue == EnumTaskValue.NormalNotOK && x.defectType == EnumDefectType.Yes)
                    //        || (x.category == EnumTaskType.Crack && x.taskValue == EnumTaskValue.CrackNotOKYes)).Count();

                    //        int countOnpAct = defectHelpers.Where(x => (x.category == EnumTaskType.Normal && x.taskValue == EnumTaskValue.NormalNotOK && x.defectType == EnumDefectType.Yes && x.status == EnumStatus.DefectSubmit)
                    //        || (x.category == EnumTaskType.Crack && x.taskValue == EnumTaskValue.CrackNotOKYes && x.status == EnumStatus.DefectSubmit)).Count();

                    //        if (countNeedAct == countOnpAct)
                    //            defect.status = EnumStatus.DefectNotAcknowledge;
                    //        else
                    //            defect.status = EnumStatus.DefectNotApproved;

                    result.Add(defect);
                    //    }
                    //}
                }

                return new ServiceResult
                {
                    Message = "Get intervention defect successfully",
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

        public override async Task<ServiceResult> Post(CreateRequest createRequest)
        {
            try
            {
                var result = await _repository.Create(createRequest);

                return new ServiceResult
                {
                    Message = "Data created successfully",
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

        public override async Task<ServiceResult> Put(UpdateRequest updateRequest)
        {
            try
            {
                DefectHeaderServiceHelper serviceHelper = new DefectHeaderServiceHelper(_connectionFactory, _container, _accessToken);
                dynamic result = await serviceHelper.Put(updateRequest);

                await UpdateInterventionDefectEHMS(updateRequest);

                return new ServiceResult
                {
                    Message = "Data updated successfully",
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

        private async Task UpdateInterventionDefectEHMS(UpdateRequest updateRequest)
        {
            CallAPIService callAPI = new CallAPIService(_appSetting, _accessToken);
            DefectHeaderModel defectHeader = new DefectHeaderModel();
            InterventionDetailModel interventionDetail = new InterventionDetailModel();
            InterventionDefectModel interventionDefect = new InterventionDefectModel();

            var defectHeaderResult = await _repository.Get(updateRequest.id);

            if (defectHeaderResult != null)
            {
                defectHeader = JsonConvert.DeserializeObject<DefectHeaderModel>(JsonConvert.SerializeObject(defectHeaderResult));

                Dictionary<string, object> intDetailParam = new Dictionary<string, object>();
                intDetailParam.Add(StaticHelper.GetPropertyName(() => interventionDetail.intervention_header_id), Convert.ToInt64(defectHeader.interventionHeaderId));
                intDetailParam.Add(StaticHelper.GetPropertyName(() => interventionDetail.task_key), defectHeader.taskId);
                intDetailParam.Add(StaticHelper.GetPropertyName(() => interventionDetail.is_active), true);
                intDetailParam.Add(StaticHelper.GetPropertyName(() => interventionDetail.is_deleted), false);

                dynamic intDetailResult = await callAPI.EHMSGetByParam(EnumController.InterventionDetail, intDetailParam);

                if (intDetailResult != null)
                {
                    interventionDetail = JsonConvert.DeserializeObject<InterventionDetailModel>(JsonConvert.SerializeObject(intDetailResult));

                    Dictionary<string, object> interventionDefectParam = new Dictionary<string, object>();
                    interventionDefectParam.Add(StaticHelper.GetPropertyName(() => interventionDefect.intervention_detail_id), interventionDetail.tr_intervention_detail_id);
                    interventionDefectParam.Add(StaticHelper.GetPropertyName(() => interventionDefect.is_active), true);
                    interventionDefectParam.Add(StaticHelper.GetPropertyName(() => interventionDefect.is_deleted), false);

                    dynamic defectResult = await callAPI.EHMSGetByParam(EnumController.InterventionDefect, interventionDefectParam);

                    if (defectResult != null)
                    {
                        interventionDefect = JsonConvert.DeserializeObject<InterventionDefectModel>(JsonConvert.SerializeObject(defectResult));

                        dynamic updateData = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(updateRequest));
                        string spvStatus = StaticHelper.GetPropValue(updateData, EnumQuery.PropertyName, EnumQuery.Status, EnumQuery.PropertyValue)?.Value;
                        string plannerStatus = StaticHelper.GetPropValue(updateData, EnumQuery.PropertyName, EnumQuery.PlannerStatus, EnumQuery.PropertyValue)?.Value;
                        string declineReason = StaticHelper.GetPropValue(updateData, EnumQuery.PropertyName, EnumQuery.DeclineReason, EnumQuery.PropertyValue)?.Value;

                        if (!string.IsNullOrEmpty(spvStatus))
                            interventionDefect.spv_status = spvStatus;

                        if (!string.IsNullOrEmpty(plannerStatus))
                            interventionDefect.planner_status = plannerStatus;

                        if (!string.IsNullOrEmpty(declineReason))
                            interventionDefect.decline_reason = declineReason;

                        interventionDefect.changed_on = EnumCommonProperty.CurrentDateTime;
                        interventionDefect.changed_by = updateRequest.employee.id;

                        await callAPI.EHMSPut(EnumController.InterventionDefect, interventionDefect);
                    }
                }
            }
        }

        public async Task<ServiceResult> GetInterventionDefectHeader(string interventionId)
        {
            try
            {
                Version103Service version103Service = new Version103Service(_appSetting, _connectionFactory, _accessToken);
                var result = await version103Service.GetInterventionDefectHeader(interventionId);

                return new ServiceResult()
                {
                    Message = "Get Intervention Defect Header successfully",
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
    }
}