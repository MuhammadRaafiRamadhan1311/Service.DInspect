using Newtonsoft.Json;
using Service.DInspect.Helpers;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.Entity;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Helper;
using Service.DInspect.Models.ADM;
using Service.DInspect.Models.Response;
using Service.DInspect.Repositories;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Services
{
    public class GeneralService : ServiceBase
    {
        private IRepositoryBase _serviceSheetHeaderRepository;
        private IRepositoryBase _interventionRepository;
        private IRepositoryBase _settingRepository;
        private IConnectionFactory _connectionFactory;

        public GeneralService(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            _serviceSheetHeaderRepository = new ServiceSheetHeaderRepository(connectionFactory, EnumContainer.ServiceSheetHeader);
            _interventionRepository = new InterventionRepository(connectionFactory, EnumContainer.Intervention);
            _settingRepository = new InterventionRepository(connectionFactory, EnumContainer.MasterSetting);
            _connectionFactory = connectionFactory;
        }

        public async Task<ServiceResult> GetOutstanding(string supervisor, string startdate = null, string enddate = null)
        {
            //ServiceSheetHeaderService ServiceSheetHeaderHelper = new ServiceSheetHeaderService(_appSetting, _connectionFactory, EnumContainer.ServiceSheetHeader, _accessToken);
            //InterventionServiceHelper interventionServiceHelper = new InterventionServiceHelper(_appSetting, _connectionFactory, _accessToken);

            try
            {
                // get user profile
                CallAPIHelper callAPIHelperEmp = new CallAPIHelper(_accessToken);
                CallAPIService callAPIHelper = new CallAPIService(_appSetting, _accessToken);
                var empRes = await callAPIHelperEmp.Get(EnumUrl.GetDataEmployeeProfileById + $"/{supervisor}?ver=v1");

                IList<EmployeeHelperModel> empProfiles = JsonConvert.DeserializeObject<List<EmployeeHelperModel>>(JsonConvert.SerializeObject(empRes.Result.Content));
                IList<string> empUserGroups = empProfiles.Select(x => x.GroupName.ToLower()).ToList();

                if (string.IsNullOrEmpty(startdate))
                    startdate = DateTime.MinValue.ToString();

                if (string.IsNullOrEmpty(enddate))
                    enddate = DateTime.MaxValue.Date.AddDays(-1).AddTicks(1).ToString();

                DateTime start = DateTime.Now;

                HistoryHelper historyHelper = new HistoryHelper(_appSetting, _connectionFactory, _accessToken);
                var histories = await historyHelper.GetHistoryV2(empProfiles.FirstOrDefault().SiteId, startdate, enddate);

                DateTime end = DateTime.Now;
                double diff = (end - start).TotalSeconds;

                int openDataSS = histories.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault().data.Servicesheet.Count;
                int onProgressDataSS = histories.Where(x => x.status == EnumStatus.EFormOnProgress).FirstOrDefault().data.Servicesheet.Count;
                int submitDataSS = histories.Where(x => x.status == EnumStatus.EFormSubmited).FirstOrDefault().data.Servicesheet.Count;

                int openDataInt = histories.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault().data.Intervention.Count;
                int onProgressDataInt = histories.Where(x => x.status == EnumStatus.EFormOnProgress).FirstOrDefault().data.Intervention.Count;
                int submitDataInt = histories.Where(x => x.status == EnumStatus.EFormSubmited).FirstOrDefault().data.Intervention.Count;

                //int openDataInterim = histories.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault().data.InterimEngine.Count;
                //int onProgressDataInterim = 0;

                int defectReviewAndServiceApprovalSupervisorCount = 0;
                int defectReviewAndServiceApprovalPlannerCount = 0;

                var menu = await callAPIHelper.GetUserMenu(supervisor);
                List<UserMenuModel> userMenu = JsonConvert.DeserializeObject<List<UserMenuModel>>(JsonConvert.SerializeObject(menu));


                if (empUserGroups.Contains(EnumPosition.Supervisor.ToLower()))
                {
                    defectReviewAndServiceApprovalSupervisorCount += histories.Where(x => x.status == EnumStatus.EFormSubmited).FirstOrDefault().data.Servicesheet.Count;
                    defectReviewAndServiceApprovalSupervisorCount += histories.Where(x => x.status == EnumStatus.EFormOnProgress).FirstOrDefault().data.Servicesheet.Count;

                    defectReviewAndServiceApprovalSupervisorCount += histories.Where(x => x.status == EnumStatus.EFormSubmited).FirstOrDefault().data.InterimEngine.Count;
                    defectReviewAndServiceApprovalSupervisorCount += histories.Where(x => x.status == EnumStatus.EFormOnProgress).FirstOrDefault().data.InterimEngine.Count;

                    defectReviewAndServiceApprovalSupervisorCount += histories.Where(x => x.status == EnumStatus.EFormOnProgress).FirstOrDefault().data.Intervention.Count;
                    defectReviewAndServiceApprovalSupervisorCount += histories.Where(x => x.status == EnumStatus.EFormSubmited).FirstOrDefault().data.Intervention.Count;
                }
                if (empUserGroups.Contains(EnumPosition.Planner.ToLower()))
                {
                    defectReviewAndServiceApprovalPlannerCount += histories.Where(x => x.status == EnumStatus.EFormFinalReview).FirstOrDefault().data.Servicesheet.Count;
                    defectReviewAndServiceApprovalPlannerCount += histories.Where(x => x.status == EnumStatus.EFormFinalReview).FirstOrDefault().data.InterimEngine.Count;
                    defectReviewAndServiceApprovalPlannerCount += histories.Where(x => x.status == EnumStatus.EFormFinalReview).FirstOrDefault().data.Intervention.Count;
                }

                int closedData = histories.Where(x => x.status == EnumStatus.EFormClosed).FirstOrDefault().dataCount;

                List<OutstandingModel> result = new List<OutstandingModel>() {
                    new OutstandingModel(){
                        menu = EnumCaption.EForm,
                        menuId = userMenu.Where(x => x.PageName.ToLower() == EnumCaption.EForm.ToLower()).Select(x => x.MenuId).FirstOrDefault(),
                        dataCount = openDataSS + onProgressDataSS + submitDataSS
                    },
                    new OutstandingModel(){
                        menu = EnumCaption.ComponentInterventionForms,
                        menuId = userMenu.Where(x => x.PageName.ToLower() == EnumCaption.ComponentInterventionForms.ToLower()).Select(x => x.MenuId).FirstOrDefault(),
                        dataCount = openDataInt + onProgressDataInt + submitDataInt,
                    },
                    new OutstandingModel(){
                        menu = EnumCaption.ComponentInterimForms,
                        menuId = userMenu.Where(x => x.PageName.ToLower() == EnumCaption.ComponentInterimForms.ToLower()).Select(x => x.MenuId).FirstOrDefault(),
                        //dataCount = openDataInterim + onProgressDataInterim
                    },
                    new OutstandingModel(){
                        menu = EnumCaption.DefectReviewAndServiceApproval,
                        menuId = userMenu.Where(x => x.PageName.ToLower() == EnumCaption.DefectReviewAndServiceApproval.ToLower() && !x.MenuName.Contains(EnumPosition.Planner.ToLower())).Select(x => x.MenuId).FirstOrDefault(),
                        dataCount = defectReviewAndServiceApprovalSupervisorCount
                    },
                    new OutstandingModel(){
                        menu = EnumCaption.DefectReviewAndServiceApproval,
                        menuId = userMenu.Where(x => x.PageName.ToLower() == EnumCaption.DefectReviewAndServiceApproval.ToLower() && x.MenuName.Contains(EnumPosition.Planner.ToLower())).Select(x => x.MenuId).FirstOrDefault(),
                        dataCount = defectReviewAndServiceApprovalPlannerCount
                    },
                    //new OutstandingModel(){
                    //    menu = EnumCaption.Approval,
                    //    dataCount = closedData
                    //}
                };

                //#region e-Form

                ////CallAPIHelper callAPIHelper = new CallAPIHelper(_accessToken);
                ////var admServicesheet = await callAPIHelper.Get(EnumUrl.GetAdmServicesheet);

                ////if (admServicesheet != null && admServicesheet.Result.Content.Count > 0)
                ////    result.Where(x => x.menu == EnumCaption.EForm).FirstOrDefault().dataCount = admServicesheet.Result.Content.Count;

                //var admServicesheet = await ServiceSheetHeaderHelper.GetServiceSheetHistory();
                //if (admServicesheet.Content.Count != null)
                //{
                //    List<ServiceSheetHistoryResponse> response = admServicesheet.Content;

                //    var openServiceSheet = response.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault();
                //    var openCount = openServiceSheet.data.Servicesheet.Count;

                //    var InprogressServiceSheet = response.Where(x => x.status == EnumStatus.EFormOnProgress).FirstOrDefault();
                //    var InprogressCount = InprogressServiceSheet.data.Servicesheet.Count;

                //    int defectCount = openCount + InprogressCount;

                //    result.Where(x => x.menu == EnumCaption.EForm).FirstOrDefault().dataCount = defectCount;
                //}


                //#endregion

                //#region Component Intervention Forms

                //var interventions = await interventionServiceHelper.GetInterventionList();

                //if(interventions.Count > 0)
                //    result.Where(x => x.menu == EnumCaption.ComponentInterventionForms).FirstOrDefault().dataCount = interventions.Count;

                //#endregion

                //#region Digital Service Approval

                //ServiceResult ServiceApproval = await ServiceSheetHeaderHelper.GetApprovalServiceSheet(supervisor);
                //if (ServiceApproval.Content != null)
                //{
                //    result.Where(x => x.menu == EnumCaption.Approval).FirstOrDefault().dataCount = ServiceApproval.Content.Count;

                //}

                //#endregion

                //#region defect review

                //    int CountdefectServiceSheet = 0;
                //    int CountDefectIntervention = 0;

                //    InterventionDefectHeaderService InterventionDefectHeaderHelper = new InterventionDefectHeaderService(_appSetting, _connectionFactory, EnumContainer.InterventionDefectHeader, _accessToken);
                //    if (supervisor != null)
                //    {
                //        #region defect service sheet
                //            ServiceResult defectServiceSheet = await ServiceSheetHeaderHelper.GetDefectServiceSheet(supervisor);
                //            if (defectServiceSheet.Content != null)
                //                CountdefectServiceSheet = defectServiceSheet.Content.Count;
                //        #endregion

                //        #region Defect intervention
                //            ServiceResult defectIntervention = await InterventionDefectHeaderHelper.GetInterventionDefect(supervisor);
                //            if(defectIntervention.Content != null)
                //            {
                //                CountDefectIntervention =  defectIntervention.Content.Count;
                //            }
                //        #endregion

                //        result.Where(x => x.menu == EnumCaption.DefectReview).FirstOrDefault().dataCount = CountdefectServiceSheet + CountDefectIntervention;
                //    }


                //#endregion

                //#region defect review Planner

                //#region defect service sheet
                //int CountdefectServiceSheetPlanner = 0;

                //var defectServiceSheetPlanner = await ServiceSheetHeaderHelper.GetDefectServiceSheet(null);
                //if (defectServiceSheetPlanner.Content != null)
                //    CountdefectServiceSheetPlanner = defectServiceSheetPlanner.Content.Count;
                //#endregion

                //#region Defect intervention
                //int CountDefectInterventionPlanner = 0;

                //var defectInterventionPlanner = await InterventionDefectHeaderHelper.GetInterventionDefect(null);
                //if (defectInterventionPlanner.Content != null)
                //    CountDefectInterventionPlanner = defectInterventionPlanner.Content.Count;

                // result.Where(x => x.menu == EnumCaption.DefectReviewPlanner).FirstOrDefault().dataCount = CountdefectServiceSheetPlanner + CountDefectInterventionPlanner;

                //#endregion
                //#endregion

                //#region Digital Service Approval


                //#endregion

                return new ServiceResult
                {
                    Message = "Get outstanding data successfully",
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

        public async Task<ServiceResult> GetOutstandingV2(string supervisor)
        {
            //ServiceSheetHeaderService ServiceSheetHeaderHelper = new ServiceSheetHeaderService(_appSetting, _connectionFactory, EnumContainer.ServiceSheetHeader, _accessToken);
            //InterventionServiceHelper interventionServiceHelper = new InterventionServiceHelper(_appSetting, _connectionFactory, _accessToken);

            try
            {
                // get user profile
                //CallAPIHelper callAPIHelperEmp = new CallAPIHelper(_accessToken);
                //CallAPIService callAPIHelper = new CallAPIService(_appSetting, _accessToken);
                //var empRes = await callAPIHelperEmp.Get(EnumUrl.GetDataEmployeeProfileById + $"/{supervisor}?ver=v1");

                //IList<EmployeeHelperModel> empProfiles = JsonConvert.DeserializeObject<List<EmployeeHelperModel>>(JsonConvert.SerializeObject(empRes.Result.Content));
                //IList<string> empUserGroups = empProfiles.Select(x => x.GroupName.ToLower()).ToList();

                //HistoryHelper historyHelper = new HistoryHelper(_appSetting, _connectionFactory, _accessToken);
                //var histories = await historyHelper.GetHistory(empProfiles.FirstOrDefault().SiteId);

                //int openDataSS = histories.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault().data.Servicesheet.Count;
                //int onProgressDataSS = histories.Where(x => x.status == EnumStatus.EFormOnProgress).FirstOrDefault().data.Servicesheet.Count;
                //int submitDataSS = histories.Where(x => x.status == EnumStatus.EFormSubmited).FirstOrDefault().data.Servicesheet.Count;

                //int openDataInt = histories.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault().data.Intervention.Count;
                //int onProgressDataInt = histories.Where(x => x.status == EnumStatus.EFormOnProgress).FirstOrDefault().data.Intervention.Count;
                //int submitDataInt = histories.Where(x => x.status == EnumStatus.EFormSubmited).FirstOrDefault().data.Intervention.Count;

                ////int openDataInterim = histories.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault().data.InterimEngine.Count;
                ////int onProgressDataInterim = 0;

                //int defectReviewAndServiceApprovalSupervisorCount = 0;
                //int defectReviewAndServiceApprovalPlannerCount = 0;

                //var menu = await callAPIHelper.GetUserMenu(supervisor);
                //List<UserMenuModel> userMenu = JsonConvert.DeserializeObject<List<UserMenuModel>>(JsonConvert.SerializeObject(menu));


                //if (empUserGroups.Contains(EnumPosition.Supervisor.ToLower()))
                //{
                //    defectReviewAndServiceApprovalSupervisorCount += histories.Where(x => x.status == EnumStatus.EFormSubmited).FirstOrDefault().data.Servicesheet.Count;
                //    defectReviewAndServiceApprovalSupervisorCount += histories.Where(x => x.status == EnumStatus.EFormOnProgress).FirstOrDefault().data.Servicesheet.Count;

                //    defectReviewAndServiceApprovalSupervisorCount += histories.Where(x => x.status == EnumStatus.EFormSubmited).FirstOrDefault().data.InterimEngine.Count;
                //    defectReviewAndServiceApprovalSupervisorCount += histories.Where(x => x.status == EnumStatus.EFormOnProgress).FirstOrDefault().data.InterimEngine.Count;

                //    defectReviewAndServiceApprovalSupervisorCount += histories.Where(x => x.status == EnumStatus.EFormOnProgress).FirstOrDefault().data.Intervention.Count;
                //    defectReviewAndServiceApprovalSupervisorCount += histories.Where(x => x.status == EnumStatus.EFormSubmited).FirstOrDefault().data.Intervention.Count;
                //}
                //if (empUserGroups.Contains(EnumPosition.Planner.ToLower()))
                //{
                //    defectReviewAndServiceApprovalPlannerCount += histories.Where(x => x.status == EnumStatus.EFormFinalReview).FirstOrDefault().data.Servicesheet.Count;
                //    defectReviewAndServiceApprovalPlannerCount += histories.Where(x => x.status == EnumStatus.EFormFinalReview).FirstOrDefault().data.InterimEngine.Count;
                //    defectReviewAndServiceApprovalPlannerCount += histories.Where(x => x.status == EnumStatus.EFormFinalReview).FirstOrDefault().data.Intervention.Count;
                //}

                //int closedData = histories.Where(x => x.status == EnumStatus.EFormClosed).FirstOrDefault().dataCount;

                List<OutstandingModel> result = new List<OutstandingModel>() {
                    new OutstandingModel(){
                        menu = EnumCaption.EForm,
                        menuId = 18,
                        dataCount = 0
                    },
                    new OutstandingModel(){
                        menu = EnumCaption.ComponentInterventionForms,
                        menuId = 135,
                        dataCount = 0,
                    },
                    new OutstandingModel(){
                        menu = EnumCaption.ComponentInterimForms,
                        menuId = 153,
                        //dataCount = openDataInterim + onProgressDataInterim
                    },
                    new OutstandingModel(){
                        menu = EnumCaption.DefectReviewAndServiceApproval,
                        menuId = 19,
                        dataCount = 0
                    },
                    new OutstandingModel(){
                        menu = EnumCaption.DefectReviewAndServiceApproval,
                        menuId = 0,
                        dataCount = 0
                    },
                    //new OutstandingModel(){
                    //    menu = EnumCaption.Approval,
                    //    dataCount = closedData
                    //}
                };

                //#region e-Form

                ////CallAPIHelper callAPIHelper = new CallAPIHelper(_accessToken);
                ////var admServicesheet = await callAPIHelper.Get(EnumUrl.GetAdmServicesheet);

                ////if (admServicesheet != null && admServicesheet.Result.Content.Count > 0)
                ////    result.Where(x => x.menu == EnumCaption.EForm).FirstOrDefault().dataCount = admServicesheet.Result.Content.Count;

                //var admServicesheet = await ServiceSheetHeaderHelper.GetServiceSheetHistory();
                //if (admServicesheet.Content.Count != null)
                //{
                //    List<ServiceSheetHistoryResponse> response = admServicesheet.Content;

                //    var openServiceSheet = response.Where(x => x.status == EnumStatus.EFormOpen).FirstOrDefault();
                //    var openCount = openServiceSheet.data.Servicesheet.Count;

                //    var InprogressServiceSheet = response.Where(x => x.status == EnumStatus.EFormOnProgress).FirstOrDefault();
                //    var InprogressCount = InprogressServiceSheet.data.Servicesheet.Count;

                //    int defectCount = openCount + InprogressCount;

                //    result.Where(x => x.menu == EnumCaption.EForm).FirstOrDefault().dataCount = defectCount;
                //}


                //#endregion

                //#region Component Intervention Forms

                //var interventions = await interventionServiceHelper.GetInterventionList();

                //if(interventions.Count > 0)
                //    result.Where(x => x.menu == EnumCaption.ComponentInterventionForms).FirstOrDefault().dataCount = interventions.Count;

                //#endregion

                //#region Digital Service Approval

                //ServiceResult ServiceApproval = await ServiceSheetHeaderHelper.GetApprovalServiceSheet(supervisor);
                //if (ServiceApproval.Content != null)
                //{
                //    result.Where(x => x.menu == EnumCaption.Approval).FirstOrDefault().dataCount = ServiceApproval.Content.Count;

                //}

                //#endregion

                //#region defect review

                //    int CountdefectServiceSheet = 0;
                //    int CountDefectIntervention = 0;

                //    InterventionDefectHeaderService InterventionDefectHeaderHelper = new InterventionDefectHeaderService(_appSetting, _connectionFactory, EnumContainer.InterventionDefectHeader, _accessToken);
                //    if (supervisor != null)
                //    {
                //        #region defect service sheet
                //            ServiceResult defectServiceSheet = await ServiceSheetHeaderHelper.GetDefectServiceSheet(supervisor);
                //            if (defectServiceSheet.Content != null)
                //                CountdefectServiceSheet = defectServiceSheet.Content.Count;
                //        #endregion

                //        #region Defect intervention
                //            ServiceResult defectIntervention = await InterventionDefectHeaderHelper.GetInterventionDefect(supervisor);
                //            if(defectIntervention.Content != null)
                //            {
                //                CountDefectIntervention =  defectIntervention.Content.Count;
                //            }
                //        #endregion

                //        result.Where(x => x.menu == EnumCaption.DefectReview).FirstOrDefault().dataCount = CountdefectServiceSheet + CountDefectIntervention;
                //    }


                //#endregion

                //#region defect review Planner

                //#region defect service sheet
                //int CountdefectServiceSheetPlanner = 0;

                //var defectServiceSheetPlanner = await ServiceSheetHeaderHelper.GetDefectServiceSheet(null);
                //if (defectServiceSheetPlanner.Content != null)
                //    CountdefectServiceSheetPlanner = defectServiceSheetPlanner.Content.Count;
                //#endregion

                //#region Defect intervention
                //int CountDefectInterventionPlanner = 0;

                //var defectInterventionPlanner = await InterventionDefectHeaderHelper.GetInterventionDefect(null);
                //if (defectInterventionPlanner.Content != null)
                //    CountDefectInterventionPlanner = defectInterventionPlanner.Content.Count;

                // result.Where(x => x.menu == EnumCaption.DefectReviewPlanner).FirstOrDefault().dataCount = CountdefectServiceSheetPlanner + CountDefectInterventionPlanner;

                //#endregion
                //#endregion

                //#region Digital Service Approval


                //#endregion

                return new ServiceResult
                {
                    Message = "Get outstanding data successfully",
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

        public virtual async Task<ServiceResult> GetLastVersion()
        {
            try
            {
                var result = string.Empty;

                Dictionary<string, object> paramVersion = new Dictionary<string, object>();
                paramVersion.Add(EnumQuery.Group, EnumCaption.FormVersion);
                paramVersion.Add(EnumQuery.IsActive, true.ToString().ToLower());
                paramVersion.Add(EnumQuery.IsDeleted, false.ToString().ToLower());

                var versions = await _settingRepository.GetDataListByParamJArray(paramVersion);
                var lastVersion = versions.Select(x => x[EnumQuery.Value].ToString().Split('.'))
                            .OrderBy(o => Convert.ToInt32(o[0]))
                            .ThenBy(o => Convert.ToInt32(o[1]))
                            .ThenBy(o => Convert.ToInt32(o[2]))
                            .Last();
                result = string.Join(".", lastVersion);

                return new ServiceResult
                {
                    Message = "Get last version successfully",
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