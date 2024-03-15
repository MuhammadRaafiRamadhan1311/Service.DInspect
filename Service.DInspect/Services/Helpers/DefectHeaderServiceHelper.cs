using Microsoft.Azure.Cosmos.Serialization.HybridRow.Schemas;
using Newtonsoft.Json;
using Service.DInspect.Helpers;
using Service.DInspect.Interfaces;
using Service.DInspect.Models.Entity;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Request;
using Service.DInspect.Helpers;
using Service.DInspect.Models;
using Service.DInspect.Models.Helper;
using Service.DInspect.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Services.Helpers
{
    public class DefectHeaderServiceHelper
    {
        private string _container;
        private IRepositoryBase _repository;
        private string _accessToken;
        //private readonly ILogger<TEntity> _logger;

        public DefectHeaderServiceHelper(IConnectionFactory connectionFactory, string container, string accessToken)
        {
            _container = container;

            if (container == EnumContainer.DefectHeader)
            {
                _repository = new DefectHeaderRepository(connectionFactory, container);
            }
            else if (container == EnumContainer.InterventionDefectHeader)
            {
                _repository = new InterventionDefectHeaderRepository(connectionFactory, container);
            }
            else if (container == EnumContainer.InterimEngineDefectHeader)
            {
                _repository = new InterimEngineDefectHeaderRepository(connectionFactory, container);
            }

            _accessToken = accessToken;
        }

        public async Task<dynamic> Put(UpdateRequest updateRequest)
        {
            string updatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerDateTime);
            string tsUpdatedDate = ((RepositoryBase)_repository).GetSettingValue(EnumCommonProperty.ServerTimeStamp);
            UpdateParam updateParam = updateRequest.updateParams.Where(s => s.propertyParams.Any(x => x.propertyName == EnumQuery.Status)).FirstOrDefault();
            bool updateDownloadHistory = updateRequest.updateParams.Any(s => s.propertyParams.Any(x => x.propertyName == EnumQuery.DownloadHistory));
            bool updateRepairedStatus = updateRequest.updateParams.Any(s => s.propertyParams.Any(x => x.propertyName == EnumQuery.RepairedStatus));
            bool updateDefectWorkorder = updateRequest.updateParams.Any(s => s.propertyParams.Any(x => x.propertyName == EnumQuery.DefectWorkorder));

            var rsc = await _repository.Get(updateRequest.id);
            var dbStatus = StaticHelper.GetPropValue(rsc, EnumQuery.Status);
            var dbPlannerStatus = StaticHelper.GetPropValue(rsc, EnumQuery.PlannerStatus);
            var dbCbmNAStatus = StaticHelper.GetPropValue(rsc, EnumQuery.CbmNAStatus);
            var dbPlannerCbmNAStatus = StaticHelper.GetPropValue(rsc, EnumQuery.PlannerCbmNAStatus);
            var dbTaskId = StaticHelper.GetPropValue(rsc, EnumQuery.TaskId);
            var dbWorkOrder = StaticHelper.GetPropValue(rsc, EnumQuery.Workorder);

            //Dictionary<string, object> paramDefect = new Dictionary<string, object>();
            //paramDefect.Add(EnumQuery.TaskId, dbTaskId.ToString());
            //paramDefect.Add(EnumQuery.Workorder, dbWorkOrder.ToString());
            ////paramDefect.Add(EnumQuery.IsActive, "true");
            ////paramDefect.Add(EnumQuery.IsDeleted, "false");

            //var defects = await _repository.GetDataListByParam(paramDefect);

            //if (defects != null)
            //{
            //    foreach (var defect in defects)
            //    {
            //        var status = StaticHelper.GetPropValue(defect, EnumQuery.Status);
            //        if (status != EnumStatus.DefectSubmit && status != EnumStatus.DefectSubmited)
            //        {
            //            throw new Exception($"You cannot modify the defect once already approved or declined by Supervisor");
            //        }
            //    }
            //}

            //check + assign default value if planner cbm status exists or not in defect header
            string _plannerCbmNAStatus = dbPlannerCbmNAStatus ?? "";

            var category = StaticHelper.GetPropValue(rsc, EnumQuery.Category);

            // get user profile
            //CallAPIHelper callAPIHelperEmp = new CallAPIHelper(_accessToken);
            //var empRes = await callAPIHelperEmp.Get(EnumUrl.GetDataEmployeeProfileById + $"/{updateRequest.employee.id}?ver=v1");

            //IList<EmployeeHelperModel> empProfiles = JsonConvert.DeserializeObject<List<EmployeeHelperModel>>(JsonConvert.SerializeObject(empRes.Result.Content));
            //IList<string> empUserGroups = empProfiles.Select(x => x.GroupName.ToLower()).ToList();

            var _tempData = StaticHelper.GetPropValue(rsc, EnumQuery.UpdatedBy);

            var defectUpdatedBy = "";
            if (!string.IsNullOrEmpty(_tempData.ToString()))
                defectUpdatedBy = _tempData[EnumQuery.Name];
            if (!updateDownloadHistory && !updateRepairedStatus && !updateDefectWorkorder)
            {
                if (updateRequest.userGroup == EnumPosition.Planner && (!string.IsNullOrEmpty(dbPlannerStatus.ToString()) || !string.IsNullOrEmpty(_plannerCbmNAStatus)))
                {
                    if (category == EnumTaskType.Crack)
                        throw new Exception(EnumErrorMessage.ErrMsgTaskCrackApproval.Replace(EnumCommonProperty.Status, dbPlannerStatus.ToString().ToLower()).Replace(EnumCommonProperty.UserName, defectUpdatedBy));
                    else
                        if (dbStatus.ToString() == EnumStatus.DefectAcknowledge || dbStatus.ToString() == EnumStatus.DefectDecline)
                        throw new Exception(EnumErrorMessage.ErrMsgDefectApproval.Replace(EnumCommonProperty.Status, dbPlannerStatus.ToString().ToLower()).Replace(EnumCommonProperty.UserName, defectUpdatedBy));
                    else if (_plannerCbmNAStatus == EnumStatus.DefectDecline || _plannerCbmNAStatus == EnumStatus.DefectConfirm)
                        throw new Exception(EnumErrorMessage.ErrMsgTaskNAApproval.Replace(EnumCommonProperty.Status, _plannerCbmNAStatus == EnumStatus.DefectDecline ? _plannerCbmNAStatus.ToLower() : EnumStatus.DefectAcknowledge.ToLower()).Replace(EnumCommonProperty.UserName, defectUpdatedBy));
                }

                if (updateRequest.userGroup != EnumPosition.Planner)
                {
                    if (category == EnumTaskType.Crack && (dbStatus.ToString() == EnumStatus.DefectAcknowledge || dbStatus.ToString() == EnumStatus.DefectDecline))
                        throw new Exception(EnumErrorMessage.ErrMsgTaskCrackApproval.Replace(EnumCommonProperty.Status, dbStatus.ToString().ToLower()).Replace(EnumCommonProperty.UserName, defectUpdatedBy));
                    else
                        if (dbStatus.ToString() == EnumStatus.DefectAcknowledge || dbStatus.ToString() == EnumStatus.DefectDecline)
                        throw new Exception(EnumErrorMessage.ErrMsgDefectApproval.Replace(EnumCommonProperty.Status, dbStatus.ToString().ToLower()).Replace(EnumCommonProperty.UserName, defectUpdatedBy));
                    else if (dbCbmNAStatus.ToString() == EnumStatus.DefectDecline || dbCbmNAStatus.ToString() == EnumStatus.DefectConfirm)
                        throw new Exception(EnumErrorMessage.ErrMsgTaskNAApproval.Replace(EnumCommonProperty.Status, dbCbmNAStatus.ToString() == EnumStatus.DefectDecline ? dbCbmNAStatus.ToString().ToLower() : EnumStatus.DefectAcknowledge.ToLower()).Replace(EnumCommonProperty.UserName, defectUpdatedBy));
                }
            }


            string updateDefectWorkorderValue = StaticHelper.GetPropValue(rsc, EnumQuery.DefectWorkorder);
            if (updateDefectWorkorder && !string.IsNullOrEmpty(updateDefectWorkorderValue))
            {
                throw new Exception(EnumErrorMessage.ErrMsgDefectWorOrder.Replace(EnumCommonProperty.UserName, defectUpdatedBy));
            }

            if (updateParam != null)
            {
                var status = StaticHelper.GetPropValue(rsc, updateParam.keyValue, EnumQuery.StatusHistory);

                List<StatusHistoryModel> statusHistories = JsonConvert.DeserializeObject<List<StatusHistoryModel>>(JsonConvert.SerializeObject(status));
                if (statusHistories == null)
                    statusHistories = new List<StatusHistoryModel>();

                statusHistories.Add(new StatusHistoryModel()
                {
                    status = updateParam.propertyParams.Where(x => x.propertyName == EnumQuery.Status).FirstOrDefault()?.propertyValue,
                    updatedBy = updateRequest.employee,
                    updatedDate = updatedDate,
                    tsUpdatedDate = tsUpdatedDate
                });

                List<PropertyParam> propertyParams = new List<PropertyParam>() {
                        new PropertyParam()
                        {
                            propertyName = EnumQuery.StatusHistory,
                            propertyValue = JsonConvert.SerializeObject(statusHistories)
                        }
                    };

                updateRequest.updateParams.Add(new UpdateParam()
                {
                    keyValue = updateParam.keyValue,
                    propertyParams = propertyParams
                });
            }

            if (updateDownloadHistory)
            {
                UpdateParam updateParams = updateRequest.updateParams.Where(s => s.propertyParams.Any(x => x.propertyName == EnumQuery.DownloadHistory)).FirstOrDefault();

                var downloadHistory = StaticHelper.GetPropValue(rsc, updateParams.keyValue, EnumQuery.DownloadHistory);

                List<DownloadHistoryModel> downloadHistoryModels = JsonConvert.DeserializeObject<List<DownloadHistoryModel>>(JsonConvert.SerializeObject(downloadHistory));
                if (downloadHistoryModels == null)
                    downloadHistoryModels = new List<DownloadHistoryModel>();

                downloadHistoryModels.Add(new DownloadHistoryModel()
                {
                    downloadBy = updateRequest.employee,
                    downloadDate = updatedDate
                });

                List<PropertyParam> propertyParams = new List<PropertyParam>() {
                        new PropertyParam()
                        {
                            propertyName = EnumQuery.DownloadHistory,
                            propertyValue = JsonConvert.SerializeObject(downloadHistoryModels)
                        }
                    };

                updateRequest.updateParams.Add(new UpdateParam()
                {
                    keyValue = updateParams.keyValue,
                    propertyParams = propertyParams
                });
            }

            var result = await _repository.Update(updateRequest, rsc);
            return result;
        }

        public async Task<bool> GetDataDetail(UpdateRequest updateRequest)
        {
            PropertyParam validateParam = new PropertyParam();

            UpdateParam updateParam = updateRequest.updateParams.Where(s => s.propertyParams.Any(x => x.propertyName == EnumQuery.IsActive)).FirstOrDefault();
            if (updateParam != null)
            {
                validateParam = updateParam.propertyParams.Where(x => x.propertyName == EnumQuery.IsActive).FirstOrDefault();
            }
            else
            {
                validateParam = null;
            }

            if (validateParam != null)
            {
                var rsc = await _repository.Get(updateRequest.id);
                var dbTaskId = StaticHelper.GetPropValue(rsc, EnumQuery.TaskId);
                var dbWorkOrder = StaticHelper.GetPropValue(rsc, EnumQuery.Workorder);

                Dictionary<string, object> paramDefect = new Dictionary<string, object>();
                paramDefect.Add(EnumQuery.TaskId, dbTaskId.ToString());
                paramDefect.Add(EnumQuery.Workorder, dbWorkOrder.ToString());

                var defects = await _repository.GetDataListByParam(paramDefect);

                if (defects != null)
                {
                    foreach (var defect in defects)
                    {
                        var status = StaticHelper.GetPropValue(defect, EnumQuery.Status);
                        if (status != EnumStatus.DefectSubmit && status != EnumStatus.DefectSubmited)
                        {
                            throw new Exception($"You cannot modify the defect once already approved or declined by Supervisor");
                        }
                    }
                }
            }

            return true;
        }
    }
}