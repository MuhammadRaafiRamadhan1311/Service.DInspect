using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.DInspect.Helpers;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Helper;
using Service.DInspect.Models.Response;
using Service.DInspect.Repositories;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

public class Version103Service
{
    private MySetting _appSetting;
    private string _accessToken;
    private IConnectionFactory _connectionFactory;

    private IRepositoryBase _serviceSheetDefectHeaderRepository;
    private IRepositoryBase _serviceSheetDefectDetailRepository;
    private IRepositoryBase _serviceSheetHeaderRepository;
    private IRepositoryBase _serviceSheetDetailRepository;

    private IRepositoryBase _interimDefectHeaderRepository;
    private IRepositoryBase _interimDefectDetailRepository;
    private IRepositoryBase _interimHeaderRepository;
    private IRepositoryBase _interimDetailRepository;

    private IRepositoryBase _interventionDefectHeaderRepository;
    private IRepositoryBase _interventionDefectDetailRepository;
    private IRepositoryBase _interventionRepository;

    public Version103Service(MySetting appSetting, IConnectionFactory connectionFactory, string accessToken)
    {
        _appSetting = appSetting;
        _accessToken = accessToken;
        _connectionFactory = connectionFactory;

        _serviceSheetDefectHeaderRepository = new InterventionRepository(connectionFactory, EnumContainer.DefectHeader);
        _serviceSheetDefectDetailRepository = new InterventionRepository(connectionFactory, EnumContainer.DefectDetail);
        _serviceSheetHeaderRepository = new InterventionRepository(connectionFactory, EnumContainer.ServiceSheetHeader);
        _serviceSheetDetailRepository = new InterventionRepository(connectionFactory, EnumContainer.ServiceSheetDetail);

        _interimDefectHeaderRepository = new InterimEngineDefectHeaderRepository(connectionFactory, EnumContainer.InterimEngineDefectHeader);
        _interimDefectDetailRepository = new InterimEngineDefectHeaderRepository(connectionFactory, EnumContainer.InterimEngineDefectDetail);
        _interimHeaderRepository = new InterimEngineDefectHeaderRepository(connectionFactory, EnumContainer.InterimEngineHeader);
        _interimDetailRepository = new InterimEngineDefectHeaderRepository(connectionFactory, EnumContainer.InterimEngineDetail);

        _interventionDefectHeaderRepository = new InterventionRepository(connectionFactory, EnumContainer.InterventionDefectHeader);
        _interventionDefectDetailRepository = new InterventionRepository(connectionFactory, EnumContainer.InterventionDefectDetail);
        _interventionRepository = new InterventionRepository(connectionFactory, EnumContainer.Intervention);
    }

    public async Task<dynamic> GetServiceSheetDefectHeader(string workOrder)
    {
        DefectIdentifyResponse result = new DefectIdentifyResponse();

        Dictionary<string, object> param = new Dictionary<string, object>();
        param.Add(EnumQuery.Workorder, workOrder);
        param.Add(EnumQuery.IsActive, true.ToString().ToLower());
        param.Add(EnumQuery.IsDeleted, false.ToString().ToLower());

        var defectHeaders = await _serviceSheetDefectHeaderRepository.GetDataListByParamJArray(param);

        foreach (var header in defectHeaders)
        {
            if ((string.IsNullOrEmpty(header[EnumQuery.ApprovedBy]?.ToString()) ||
                string.IsNullOrEmpty(header[EnumQuery.ApprovedDate]?.ToString())) &&
                header[EnumQuery.Status].ToString() != EnumStatus.DefectDecline)
            {
                var statusHistory = header[EnumQuery.StatusHistory];
                var latestStatus = statusHistory.FirstOrDefault(x => x[EnumQuery.Status].ToString() == EnumStatus.DefectAcknowledge);
                header[EnumQuery.ApprovedBy] = latestStatus != null ? latestStatus[EnumQuery.UpdatedBy] : "";
                header[EnumQuery.ApprovedDate] = latestStatus != null ? latestStatus[EnumQuery.UpdatedDate].ToString() : "";
            }
        }

        param = new Dictionary<string, object>();
        param.Add(EnumQuery.Workorder, workOrder);
        param.Add(EnumQuery.IsActive, true.ToString().ToLower());
        param.Add(EnumQuery.IsDeleted, false.ToString().ToLower());

        var defectDetails = await _serviceSheetDefectDetailRepository.GetDataListByParamJArray(param);

        param = new Dictionary<string, object>();
        param.Add(EnumQuery.SSWorkorder, workOrder);
        param.Add(EnumQuery.IsActive, true.ToString().ToLower());
        param.Add(EnumQuery.IsDeleted, false.ToString().ToLower());

        var serviceSheetHeader = await _serviceSheetHeaderRepository.GetDataByParam(param);

        param = new Dictionary<string, object>();
        param.Add(EnumQuery.SSWorkorder, workOrder);
        param.Add(EnumQuery.IsActive, true.ToString().ToLower());
        param.Add(EnumQuery.IsDeleted, false.ToString().ToLower());

        var serviceSheetDetails = await _serviceSheetDetailRepository.GetDataListByParamJArray(param);

        Dictionary<string, object> response = await GetComment(defectHeaders, serviceSheetDetails, false);

        result.version = serviceSheetDetails.FirstOrDefault()[EnumQuery.Version].ToString();
        result.defectHeader = response.Where(x => x.Key == EnumCaption.DefectHeaders).FirstOrDefault().Value;
        result.defectDetail = defectDetails;
        result.comment = response.Where(x => x.Key == EnumCaption.Comments).FirstOrDefault().Value;
        result.comment = response.Where(x => x.Key == EnumCaption.Comments).FirstOrDefault().Value;
        result.comment = response.Where(x => x.Key == EnumCaption.Comments).FirstOrDefault().Value;

        result.approveBy = StaticHelper.GetPropValue(serviceSheetHeader, EnumQuery.Status, EnumStatus.EFormApprovedSPV, EnumQuery.UpdatedBy);
        result.approveDate = StaticHelper.GetPropValue(serviceSheetHeader, EnumQuery.Status, EnumStatus.EFormApprovedSPV, EnumQuery.UpdatedDate);

        return result;
    }

    public async Task<dynamic> GetInterimDefectHeader(string workOrder)
    {
        DefectIdentifyResponse result = new DefectIdentifyResponse();

        Dictionary<string, object> param = new Dictionary<string, object>();
        param.Add(EnumQuery.Workorder, workOrder);
        param.Add(EnumQuery.IsActive, true.ToString().ToLower());
        param.Add(EnumQuery.IsDeleted, false.ToString().ToLower());

        var defectHeaders = await _interimDefectHeaderRepository.GetDataListByParamJArray(param);

        foreach (var header in defectHeaders)
        {
            if ((string.IsNullOrEmpty(header[EnumQuery.ApprovedBy]?.ToString()) ||
                string.IsNullOrEmpty(header[EnumQuery.ApprovedDate]?.ToString())) &&
                header[EnumQuery.Status].ToString() != EnumStatus.DefectDecline)
            {
                var statusHistory = header[EnumQuery.StatusHistory];
                var latestStatus = statusHistory.FirstOrDefault(x => x[EnumQuery.Status].ToString() == EnumStatus.DefectAcknowledge);
                header[EnumQuery.ApprovedBy] = latestStatus != null ? latestStatus[EnumQuery.UpdatedBy] : "";
                header[EnumQuery.ApprovedDate] = latestStatus != null ? latestStatus[EnumQuery.UpdatedDate].ToString() : "";
            }
        }

        param = new Dictionary<string, object>();
        param.Add(EnumQuery.Workorder, workOrder);
        param.Add(EnumQuery.IsActive, true.ToString().ToLower());
        param.Add(EnumQuery.IsDeleted, false.ToString().ToLower());

        var defectDetails = await _interimDefectDetailRepository.GetDataListByParamJArray(param);

        param = new Dictionary<string, object>();
        param.Add(EnumQuery.SSWorkorder, workOrder);
        param.Add(EnumQuery.IsActive, true.ToString().ToLower());
        param.Add(EnumQuery.IsDeleted, false.ToString().ToLower());

        var interimHeader = await _interimHeaderRepository.GetDataByParam(param);

        param = new Dictionary<string, object>();
        param.Add(EnumQuery.SSWorkorder, workOrder);
        param.Add(EnumQuery.IsActive, true.ToString().ToLower());
        param.Add(EnumQuery.IsDeleted, false.ToString().ToLower());

        var interimDetails = await _interimDetailRepository.GetDataListByParamJArray(param);

        Dictionary<string, object> response = await GetComment(defectHeaders, interimDetails, false);

        result.version = interimDetails.FirstOrDefault()[EnumQuery.Version].ToString();
        result.defectHeader = response.Where(x => x.Key == EnumCaption.DefectHeaders).FirstOrDefault().Value;
        result.defectDetail = defectDetails;
        result.comment = response.Where(x => x.Key == EnumCaption.Comments).FirstOrDefault().Value;

        var dataApprover = StaticHelper.GetPropValue(interimHeader, EnumQuery.Status, EnumStatus.EFormApprovedSPV, EnumQuery.UpdatedBy);
        var dataApprovedDate = StaticHelper.GetPropValue(interimHeader, EnumQuery.Status, EnumStatus.EFormApprovedSPV, EnumQuery.UpdatedDate);

        result.approveBy = dataApprover == null ? StaticHelper.GetPropValue(interimHeader, EnumQuery.Status, EnumStatus.IEngineClosed, EnumQuery.UpdatedBy) : dataApprover;
        result.approveDate = dataApprovedDate == null ? StaticHelper.GetPropValue(interimHeader, EnumQuery.Status, EnumStatus.IEngineClosed, EnumQuery.UpdatedDate) : dataApprovedDate;

        return result;
    }

    public async Task<dynamic> GetInterventionDefectHeader(string interventionId)
    {
        DefectIdentifyResponse result = new DefectIdentifyResponse();

        Dictionary<string, object> param = new Dictionary<string, object>();
        param.Add(EnumQuery.InterventionId, interventionId);
        param.Add(EnumQuery.IsActive, true.ToString().ToLower());
        param.Add(EnumQuery.IsDeleted, false.ToString().ToLower());

        var defectHeaders = await _interventionDefectHeaderRepository.GetDataListByParamJArray(param);

        foreach (var header in defectHeaders)
        {
            if ((string.IsNullOrEmpty(header[EnumQuery.ApprovedBy]?.ToString()) ||
                string.IsNullOrEmpty(header[EnumQuery.ApprovedDate]?.ToString())) &&
                header[EnumQuery.Status].ToString() != EnumStatus.DefectDecline)
            {
                var statusHistory = header[EnumQuery.StatusHistory];
                var latestStatus = statusHistory.FirstOrDefault(x => x[EnumQuery.Status].ToString() == EnumStatus.DefectAcknowledge);
                header[EnumQuery.ApprovedBy] = latestStatus != null ? latestStatus[EnumQuery.UpdatedBy] : "";
                header[EnumQuery.ApprovedDate] = latestStatus != null ? latestStatus[EnumQuery.UpdatedDate].ToString() : "";
            }
        }

        param = new Dictionary<string, object>();
        param.Add(EnumQuery.InterventionId, interventionId);
        param.Add(EnumQuery.IsActive, true.ToString().ToLower());
        param.Add(EnumQuery.IsDeleted, false.ToString().ToLower());

        var defectDetails = await _interventionDefectDetailRepository.GetDataListByParamJArray(param);

        param = new Dictionary<string, object>();
        param.Add(EnumQuery.ID, interventionId);
        param.Add(EnumQuery.IsActive, true.ToString().ToLower());
        param.Add(EnumQuery.IsDeleted, false.ToString().ToLower());

        var interventionDetails = await _interventionRepository.GetDataListByParamJArray(param);

        Dictionary<string, object> response = await GetComment(defectHeaders, interventionDetails, true);

        result.version = interventionDetails.FirstOrDefault()[EnumQuery.Version].ToString();
        result.defectHeader = response.Where(x => x.Key == EnumCaption.DefectHeaders).FirstOrDefault().Value;
        result.defectDetail = defectDetails;
        result.comment = response.Where(x => x.Key == EnumCaption.Comments).FirstOrDefault().Value;

        dynamic intervention = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(interventionDetails.FirstOrDefault()));

        //result.approveBy = StaticHelper.GetPropValue(intervention, EnumQuery.Status, EnumStatus.EFormApprovedSPV, EnumQuery.UpdatedBy);
        //result.approveDate = StaticHelper.GetPropValue(intervention, EnumQuery.Status, EnumStatus.EFormApprovedSPV, EnumQuery.UpdatedDate);

        result.approveBy = StaticHelper.GetPropValue(intervention, EnumQuery.Status, EnumStatus.EFormClosed, EnumQuery.UpdatedBy);
        result.approveDate = StaticHelper.GetPropValue(intervention, EnumQuery.Status, EnumStatus.EFormClosed, EnumQuery.UpdatedDate);

        return result;
    }

    private async Task<Dictionary<string, object>> GetComment(JArray defectHeaders, JArray details, bool isIntervention)
    {
        Dictionary<string, object> result = new Dictionary<string, object>();
        JArray jArrdefectHeaders = new JArray();
        List<CommentHelperModel> comments = new List<CommentHelperModel>();

        string version = (details.FirstOrDefault())[EnumQuery.Version].ToString();

        //get correct object where called from intervention/service sheet/interim
        string work_order = "";
        if (!isIntervention)
        {
            work_order = (details.FirstOrDefault())[EnumQuery.SSWorkorder].ToString();
        } 
        else
        {
            work_order = (details.FirstOrDefault())[EnumQuery.SapWorkOrder].ToString();
        }
            
        List<JToken> doneTasks = new List<JToken>();
        List<ServiceDetailCommentGroupRespone> listCommentGroup = await GetCommentGroupTask(work_order);
        listCommentGroup = listCommentGroup.Where(x => x.commentGroupTask != null).ToList();

        foreach (var detail in details)
        {
            doneTasks.AddRange(StaticHelper.GetDataNotEqual(detail, EnumQuery.TaskValue, string.Empty));
            doneTasks.AddRange(StaticHelper.GetDataNotEqual(detail, EnumQuery.TaskValueLeak, string.Empty));
            //doneTasks.AddRange(StaticHelper.GetDataNotEqual(detail, EnumQuery.TaskValueMounting, string.Empty));
        }

        foreach (var doneTask in doneTasks)
        {
            var defectHeader = defectHeaders.FilterEqual(EnumQuery.TaskId, doneTask[EnumQuery.Key].Value<string>());
            var adjustment = StaticHelper.GetData(doneTask, EnumQuery.Description, EnumCaption.Adjustment).FirstOrDefault();
            var replacement = StaticHelper.GetData(doneTask, EnumQuery.Description, EnumCaption.Replacement).FirstOrDefault();
            var rating = StaticHelper.GetData(doneTask, EnumQuery.Rating).FirstOrDefault();
            var doneTaskCommentId = StaticHelper.GetData(doneTask, EnumQuery.CommentId).FirstOrDefault();
            string commentIdString = doneTaskCommentId?.ToString();
            string ratingAsString = rating?.ToString();

            CommentHelperModel comment = new CommentHelperModel();
            string commentValue = string.Empty;

            var taskComment = StaticHelper.GetData(doneTask, EnumQuery.ValueItemType, EnumValueItemType.Comment).FirstOrDefault();
            var taskCommentGroup = StaticHelper.GetData(doneTask,EnumQuery.ValueItemType, EnumValueItemType.CommentGroup).FirstOrDefault();


            if (string.Equals(ratingAsString, EnumRatingServiceSheet.AUTOMATIC_PREVIOUS_GROUP))
            {
                var commentId = StaticHelper.GetData(doneTask, EnumQuery.CommentId).FirstOrDefault()?.ToString();

                var tempComment = listCommentGroup.Where(x => x.commentId == commentId).FirstOrDefault();

                commentValue = tempComment == null ? string.Empty : tempComment.commentGroupTask;
            }

            if (taskComment != null) 
            {
                commentValue = taskComment == null ? string.Empty : taskComment[EnumQuery.Value].ToString();
            }
                
            if(adjustment != null)
            {
                commentValue = adjustment[EnumQuery.CommentValue] == null ? string.Empty : adjustment[EnumQuery.CommentValue].ToString();
            }

            if(replacement != null)
            {
                commentValue = replacement[EnumQuery.CommentValue] == null ? string.Empty : replacement[EnumQuery.CommentValue].ToString();
            }

            if (defectHeader.Count == 0)
            {
                if (!string.IsNullOrEmpty(commentValue))
                {
                    comment.taskKey = doneTask[EnumQuery.Key].ToString();
                    comment.taskDesc = doneTask[EnumQuery.Description].ToString();
                    comment.taskComment = commentValue;
                    comment.createdBy = doneTask[EnumQuery.CreatedBy];
                    comment.createdDate = doneTask[EnumQuery.CreatedDate];
                    comment.updatedBy = doneTask[EnumQuery.UpdatedBy];
                    comment.updatedDate = doneTask[EnumQuery.UpdatedDate];

                    comments.Add(comment);
                }
            }
            else
            {
                foreach (var defect in defectHeader)
                {
                    defect[EnumQuery.CommentValue] = commentValue;

                    jArrdefectHeaders.Add(defect);
                }
            }

        }

        var genericDefectHeader = defectHeaders.FilterEqual(EnumQuery.TaskId, string.Empty);

        foreach (var genericDefect in genericDefectHeader)
        {
            jArrdefectHeaders.Add(genericDefect);
        }

        jArrdefectHeaders.Add(StaticHelper.GetData(defectHeaders, EnumQuery.Category, EnumGroup.General)); // add general defect for smu not in range

        result.Add(EnumCaption.DefectHeaders, jArrdefectHeaders);
        result.Add(EnumCaption.Comments, comments);

        return result;
    }

    public async Task <List<ServiceDetailCommentGroupRespone>> GetCommentGroupTask(string workOrder)
    {
        
        string rating = "AUTOMATIC_PREVIOUS_GROUP";
        var _repoDetail = new ServiceSheetDetailRepository(_connectionFactory, EnumContainer.ServiceSheetDetail);
        var dataRespone = await _repoDetail.GetCommentId(workOrder, rating);
        List<ServiceDetailCommentGroupRespone> listDataCommentGroup = JsonConvert.DeserializeObject<List<ServiceDetailCommentGroupRespone>>(JsonConvert.SerializeObject(dataRespone));

        return listDataCommentGroup;
    }
}