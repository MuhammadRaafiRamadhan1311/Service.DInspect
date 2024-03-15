using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Service.DInspect.Repositories;
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Linq;
using System.Collections.Generic;
using System.Linq;
using Service.DInspect.Helpers;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models;
using Service.DInspect.Models.Helper;
using Service.DInspect.Interfaces;
using Service.DInspect.Models.Request;

namespace Service.DInspect.Services
{
    public class InterventionDefectDetailService : ServiceBase
    {
        private string _container;
        private IConnectionFactory _connectionFactory;
        private IRepositoryBase _interventionRepository;
        private IRepositoryBase _defectHeaderRepository;

        public InterventionDefectDetailService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            _container = container;
            _connectionFactory = connectionFactory;
            _repository = new PsTypeSettingRepository(connectionFactory, container);
            _interventionRepository = new InterventionRepository(connectionFactory, EnumContainer.Intervention);
            _defectHeaderRepository = new InterventionDefectHeaderRepository(connectionFactory, EnumContainer.InterventionDefectHeader);
        }

        public override async Task<ServiceResult> Put(UpdateRequest updateRequest)
        {
            try
            {
                var rsc = await _repository.Get(updateRequest.id);

                string interventionId = rsc[EnumQuery.InterventionId];
                var rscIntervention = await _interventionRepository.Get(interventionId);
                string interventionExecution = rscIntervention[EnumQuery.InterventionExecution];
                string interventionDefectStatus = rscIntervention[EnumQuery.DefectStatus];

                string defectHeaderId = rsc[EnumQuery.DefectHeaderId];
                var rscDefectHeader = await _defectHeaderRepository.Get(defectHeaderId);
                string status = rscDefectHeader[EnumQuery.Status];
                string plannerStatus = rscDefectHeader[EnumQuery.PlannerStatus];
                var defectType = StaticHelper.GetPropValue(rscDefectHeader, EnumQuery.DefectType);

                // get user profile
                CallAPIHelper callAPIHelperEmp = new CallAPIHelper(_accessToken);
                var empRes = await callAPIHelperEmp.Get(EnumUrl.GetDataEmployeeProfileById + $"/{updateRequest.employee.id}?ver=v1");

                IList<EmployeeHelperModel> empProfiles = JsonConvert.DeserializeObject<List<EmployeeHelperModel>>(JsonConvert.SerializeObject(empRes.Result.Content));
                IList<string> empUserGroups = empProfiles.Select(x => x.GroupName.ToLower()).ToList();

                if (updateRequest.userGroup == EnumPosition.Planner && !string.IsNullOrEmpty(plannerStatus.ToString()))
                {
                    var _tempData = StaticHelper.GetPropValue(rscDefectHeader, EnumQuery.UpdatedBy);

                    var defectUpdatedBy = "";
                    if (!string.IsNullOrEmpty(_tempData.ToString()))
                        defectUpdatedBy = _tempData[EnumQuery.Name];
                    string errMsg = "";

                    if (defectType == EnumDefectType.MachineSMU)
                        errMsg = EnumErrorMessage.ErrMsgMachineSMUApproval.Replace(EnumCommonProperty.Status, plannerStatus.ToString().ToLower()).Replace(EnumCommonProperty.UserName, defectUpdatedBy);
                    else
                        errMsg = EnumErrorMessage.ErrMsgDefectApproval.Replace(EnumCommonProperty.Status, plannerStatus.ToString().ToLower()).Replace(EnumCommonProperty.UserName, defectUpdatedBy);

                    throw new Exception(errMsg);
                }

                if (updateRequest.userGroup != EnumPosition.Planner && status.ToString() == EnumStatus.DefectAcknowledge || status.ToString() == EnumStatus.DefectDecline)
                {
                    var _tempData = StaticHelper.GetPropValue(rscDefectHeader, EnumQuery.UpdatedBy);

                    var defectUpdatedBy = "";
                    if (!string.IsNullOrEmpty(_tempData.ToString()))
                        defectUpdatedBy = _tempData[EnumQuery.Name];

                    string errMsg = "";
                    if (defectType == EnumDefectType.MachineSMU)
                        errMsg = EnumErrorMessage.ErrMsgMachineSMUApproval.Replace(EnumCommonProperty.Status, status.ToString().ToLower()).Replace(EnumCommonProperty.UserName, defectUpdatedBy);
                    else
                        errMsg = EnumErrorMessage.ErrMsgDefectApproval.Replace(EnumCommonProperty.Status, status.ToString().ToLower()).Replace(EnumCommonProperty.UserName, defectUpdatedBy);

                    throw new Exception(errMsg);
                }
                var result = await _repository.Update(updateRequest, rsc);


                ServiceResult serviceResult = new ServiceResult
                {
                    Message = "Data updated successfully",
                    IsError = false,
                    Content = result
                };
                return serviceResult;
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

        public virtual async Task<ServiceResult> PutByFitter(UpdateRequest updateRequest)
        {
            try
            {
                var rsc = await _repository.Get(updateRequest.id);
                string defectHeaderId = StaticHelper.GetPropValue(rsc, EnumQuery.DefectHeaderId);

                var defectHeaderRsc = await _defectHeaderRepository.Get(defectHeaderId);

                var statusDefect = StaticHelper.GetPropValue(defectHeaderRsc, EnumQuery.Status);

                if (statusDefect != EnumStatus.DefectSubmited)
                {
                    return new ServiceResult
                    {
                        Message = EnumErrorMessage.ErrMsgDefectReview.Replace(EnumCommonProperty.Status, statusDefect.ToString().ToLower()),
                        IsError = true
                    };
                }

                var result = await _repository.Update(updateRequest, rsc);

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
    }
}