using Service.DInspect.Models;
using System;

namespace Service.DInspect.Models.Enum
{
    public static class EnumCommonProperty
    {
        public static MySetting appSetting { get; set; }
        public static string appTimeZone { get; set; }

        public static string ID { get { return "id"; } }
        public static string IsActive { get { return "isActive"; } }
        public static string IsDeleted { get { return "isDeleted"; } }
        public static string CreatedBy { get { return "createdBy"; } }
        public static string CreatedDate { get { return "createdDate"; } }
        public static string UpdatedBy { get { return "updatedBy"; } }
        public static string UpdatedDate { get { return "updatedDate"; } }
        public static string ErrorMsg { get { return "errorMsg"; } }

        public static string ServerDateTime { get { return "<<ServerDateTime>>"; } }
        public static string ServerTimeStamp { get { return "<<ServerTimeStamp>>"; } }
        public static string UserAccount { get { return "<<userAccount>>"; } }

        public static string TaskId { get { return "<<taskId>>"; } }
        public static string ModelUnitId { get { return "<<modelUnitId>>"; } }
        public static string PsType { get { return "<<psType>>"; } }
        public static string TaskGroupKey { get { return "<<taskGroupKey>>"; } }
        public static string TaskKey { get { return "<<taskKey>>"; } }
        public static string InterventionSequence { get { return "<<interventionSequence>>"; } }
        public static string SubTask { get { return "<<subTask>>"; } }
        public static string Guid { get { return "<<guid>>"; } }
        public static string ConditionGuid { get { return "<<conditionGuid>>"; } }
        public static string Sequence { get { return "<<sequence>>"; } }
        public static string Description { get { return "<<description>>"; } }
        public static string Uom { get { return "<<uom>>"; } }
        public static string UomCaption { get { return "<<UomCaption>>"; } }
        public static string UomValue { get { return "<<UomValue>>"; } }
        public static string RatingCaption { get { return "<<RatingCaption>>"; } }
        public static string RatingValue { get { return "<<RatingValue>>"; } }
        public static string ConditionCaption { get { return "<<ConditionCaption>>"; } }
        public static string ConditionValue { get { return "<<ConditionValue>>"; } }
        public static string Controller { get { return "<<controller>>"; } }
        public static string Id { get { return "<<id>>"; } }
        public static string KeyPbi { get { return "<<keyPbi>>"; } }
        public static string TypeTaskId { get { return "<<typeTaskId>>"; } }
        public static string TypeTask { get { return "<<typeTask>>"; } }
        public static string IsAdditionalTask { get { return "<<isAdditionalTask>>"; } }
        public static DateTime CurrentDateTime { get { return DateTime.UtcNow.AddHours(Convert.ToDouble(appTimeZone)); } }
        public static double CurrentTimeStamp { get { return (DateTime.UtcNow.AddHours(Convert.ToDouble(appTimeZone)) - new DateTime(1970, 1, 1)).TotalSeconds; } }
        public static string Status { get { return "<<status>>"; } }
        public static string Equipment { get { return "<<equipment>>"; } }
        public static string MappingParamKey { get { return "mappingParamKey"; } }
        public static string MappingParamKeyTag { get { return "<<mappingParamKey>>"; } }
        public static string ResultParamRating { get { return "resultParamRating"; } }
        public static string CalculateAvg { get { return "calculateAvg"; } }
        public static string CbmCalculateAvg { get { return "cbmCalculateAvg"; } }
        public static string ImageData { get { return "<<imageData>>"; } }
        public static string Lubricant { get { return "<<LUBRICANT>>"; } }
        public static string StyleBorder { get { return "1px solid #919eab3d"; } }
        public static string UserName { get { return "<<userName>>"; } }
        public static string ApprovedBy { get { return "<<approvedBy>>"; } }
    }
}
