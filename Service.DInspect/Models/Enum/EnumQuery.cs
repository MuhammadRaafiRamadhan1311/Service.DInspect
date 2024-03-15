﻿using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json.Linq;

namespace Service.DInspect.Models.Enum
{
    public static class EnumQuery
    {
        public static string ID { get { return "id"; } }
        public static string Rating { get { return "rating"; } }
        public static string IsActive { get { return "isActive"; } }
        public static string IsDeleted { get { return "isDeleted"; } }
        public static string CreatedBy { get { return "createdBy"; } }
        public static string CreatedDate { get { return "createdDate"; } }
        public static string UpdatedBy { get { return "updatedBy"; } }
        public static string UpdatedDate { get { return "updatedDate"; } }
        public static string Key { get { return "key"; } }
        public static string Value { get { return "value"; } }
        public static string Description { get { return "description"; } }
        public static string Workorder { get { return "workorder"; } }
        public static string SSWorkorder { get { return "workOrder"; } }
        public static string TaskValue { get { return "taskValue"; } }
        public static string TaskValueLeak { get { return "taskValueLeak"; } }
        public static string TaskValueMounting { get { return "taskValueMounting"; } }
        public static string TaskDescLeak { get { return "Leak"; } }
        public static string TaskDescMounting { get { return "Mounting"; } }
        public static string TaskDesc { get { return "taskDesc"; } }
        public static string GroupName { get { return "groupName"; } }
        public static string ModelId { get { return "modelId"; } }
        public static string PsTypeId { get { return "psTypeId"; } }
        public static string HeaderId { get { return "headerId"; } }
        public static string Supervisor { get { return "supervisor"; } }
        public static string AdditionalInformation { get { return "additionalInformation"; } }
        public static string Status { get { return "status"; } }
        public static string DefectStatus { get { return "defectStatus"; } }
        public static string StatusHistory { get { return "statusHistory"; } }
        public static string DefectHeaderId { get { return "defectHeaderId"; } }
        public static string ApprovedBy { get { return "approvedBy"; } }
        public static string ApprovedDate { get { return "approvedDate"; } }
        public static string ServicesheetDetailId { get { return "servicesheetDetailId"; } }
        public static string CbmNAStatus { get { return "cbmNAStatus"; } }
        public static string CbmMeasurement { get { return "cbmMeasurement"; } }
        public static string CbmUom { get { return "cbmUom"; } }
        public static string CbmImageKey { get { return "cbmImageKey"; } }
        public static string CbmImageProp { get { return "cbmImageProp"; } }
        public static string CbmRatingType { get { return "cbmRatingType"; } }
        public static string isCbmAdjustment { get { return "isCbmAdjustment"; } }
        public static string PlannerCbmNAStatus { get { return "plannerCbmNAStatus"; } }

        public static string Ts { get { return "_ts"; } }

        public static string TaskId { get { return "taskId"; } }
        public static string Category { get { return "category"; } }
        public static string DefectType { get { return "defectType"; } }
        public static string DefectHeader { get { return "defectHeader"; } }
        public static string DefectDetail { get { return "defectDetail"; } }
        public static string Detail { get { return "detail"; } }
        public static string Details { get { return "details"; } }
        public static string PreviousCracks { get { return "previousCracks"; } }
        public static string Parts { get { return "parts"; } }
        public static string Labours { get { return "labours"; } }
        public static string ValueType { get { return "valueType"; } }
        public static string CustomValidation { get { return "customValidation"; } }
        public static string GroupTaskId { get { return "groupTaskId"; } }
        public static string ParentGroupTaskId { get { return "parentGroupTaskId"; } }
        public static string ChildGroupTaskId { get { return "childGroupTaskId"; } }
        public static string LabourActivity { get { return "labourActivity"; } }
        public static string Qty { get { return "qty"; } }
        public static string HireEach { get { return "hireEach"; } }
        public static string TotalHours { get { return "totalHours"; } }
        public static string TsServiceStart { get { return "tsServiceStart"; } }
        public static string TsServiceEnd { get { return "tsServiceEnd"; } }
        public static string TaskNo { get { return "taskNo"; } }
        public static string IsDownload { get { return "isDownload"; } }
        public static string ComponentSystem { get { return "componentSystem"; } }
        public static string Reason { get { return "reason"; } }

        public static string SapWorkOrder { get { return "sapWorkOrder"; } }
        public static string Template { get { return "template"; } }
        public static string HeaderTask { get { return "headerTask"; } }
        public static string RiskAssesment { get { return "riskAssesment"; } }
        public static string SafetyPrecaution { get { return "safetyPrecaution"; } }
        public static string InterventionId { get { return "interventionId"; } }
        public static string TrInterventionHeaderId { get { return "trInterventionHeaderId"; } }
        public static string InterventionHeaderId { get { return "interventionHeaderId"; } }
        public static string InterventionExecution { get { return "interventionExecution"; } }
        public static string InterventionExecutionId { get { return "interventionExecutionId"; } }
        public static string KeyPbi { get { return "keyPbi"; } }
        public static string siteId { get { return "siteId"; } }
        public static string site { get { return "site"; } }
        public static string siteDesc { get { return "sitedesc"; } }
        public static string EquipmentDesc { get { return "equipmentDesc"; } }
        public static string EquipmentModel { get { return "equipmentModel"; } }
        public static string EquipmentBrand { get { return "equipmentBrand"; } }
        public static string EquipmentGroup { get { return "equipmentGroup"; } }
        public static string ComponentId { get { return "componentId"; } }
        public static string ComponentCode { get { return "componentCode"; } }
        public static string ComponentDescription { get { return "componentDescription"; } }
        public static string SampleType { get { return "sampleType"; } }
        public static string InterventionCode { get { return "interventionCode"; } }
        public static string InterventionReason { get { return "interventionReason"; } }
        public static string SampleDate { get { return "sampleDate"; } }
        public static string SampleStatusId { get { return "sampleStatusId"; } }
        public static string SampleStatus { get { return "sampleStatus"; } }
        public static string Smu { get { return "smu"; } }
        public static string SmuDue { get { return "smuDue"; } }
        public static string ComponentHm { get { return "componentHm"; } }
        public static string Equipment { get { return "equipment"; } }
        public static string EquipmentId { get { return "equipmentId"; } }
        public static string InterventionStatusDesc { get { return "interventionStatusDesc"; } }
        public static string interventionDiagnosis { get { return "interventionDiagnosis"; } }
        public static string StatusDatetime { get { return "statusDatetime"; } }
        public static string FollowUpPriorityUom { get { return "followUpPriorityUom"; } }
        public static string FollowUpPriorityUomId { get { return "followUpPriorityUomId"; } }
        public static string CautionRatingDate { get { return "cautionRatingDate"; } }
        public static string InterventionExecutionBy { get { return "interventionExecutionBy"; } }
        public static string IsSuccess { get { return "isSuccess"; } }
        public static string Group { get { return "group"; } }
        public static string Tasks { get { return "tasks"; } }
        public static string Task { get { return "task"; } }
        public static string RefDocId { get { return "refDocId"; } }
        public static string SerialNumber { get { return "serialNumber"; } }
        public static string EquipmentSerialNumber { get { return "equipmentSerialNumber"; } }
        public static string CommentValue { get { return "commentValue"; } }


        public static string Fields { get { return "fields"; } }
        public static string PropertyName { get { return "propertyName"; } }
        public static string PropertyValue { get { return "propertyValue"; } }
        public static string ValueItemType { get { return "valueItemType"; } }


        public static string TimeZone { get { return "TimeZone"; } }
        public static string TimeZoneDesc { get { return "TimeZoneDesc"; } }
        public static string InterventionMaxEstDate { get { return "InterventionMaxEstDate"; } }
        public static string ASC { get { return "ASC"; } }
        public static string DESC { get { return "DESC"; } }

        public static string MdInterventionStatusId { get { return "mdInterventionStatusId"; } }
        public static string InterventionStatus { get { return "interventionStatus"; } }
        public static string InterventionDiagnosis { get { return "interventionDiagnosis"; } }
        public static string FollowUpPriority { get { return "followUpPriority"; } }
        public static string EstimationCompletionDate { get { return "estimationCompletionDate"; } }
        public static string Version { get { return "version"; } }
        public static string RecomendedActionId { get { return "recomended_action_id"; } }
        public static string Intervention_Header_Id { get { return "intervention_header_id"; } }
        public static string TaskKey { get { return "task_key"; } }
        public static string SSTaskKey { get { return "taskKey"; } }
        public static string ServicePersonnels { get { return "servicePersonnels"; } }
        public static string DownloadHistory { get { return "downloadHistory"; } }
        public static string RepairedStatus { get { return "repairedStatus"; } }
        public static string DefectWorkorder { get { return "defectWorkorder"; } }
        public static string KeyValueRiskAssesment { get { return "11968563-1f23-4de2-b70e-e227e271d4b0"; } }
        public static string Log { get { return "log"; } }
        public static string EformType { get { return "eformType"; } }
        public static string Form { get { return "form"; } }
        public static string formDefect { get { return "formDefect"; } }
        public static string PlannerStatus { get { return "plannerStatus"; } }
        public static string DeclineReason { get { return "declineReason"; } }
        public static string DeclineBy { get { return "declineBy"; } }
        public static string DeclineDate { get { return "declineDate"; } }
        public static string InterventionSMU { get { return "interventionSMU"; } }
        public static string LocalInterventionStatus { get { return "localInterventionStatus"; } }
        public static string Employee { get { return "employee"; } }
        public static string Mechanic { get { return "mechanic"; } }
        public static string ServiceStart { get { return "serviceStart"; } }
        public static string ServiceEnd { get { return "serviceEnd"; } }
        public static string Shift { get { return "shift"; } }
        public static string AM { get { return "am"; } }
        public static string PM { get { return "pm"; } }
        public static string Active { get { return "Active"; } }
        public static string Inactive { get { return "Inactive"; } }

        public static string ShowParameter { get { return "showParameter"; } }
        public static string ValueShowParameter { get { return "cylinderHeightNeedAdjustment"; } }

        public static string LubeServiceSample { get { return "LUBE_SERVICE_SAMPLE"; } }
        public static string LubeServiceChange { get { return "LUBE_SERVICE_CHANGE"; } }
        public static string LubeServiceLevelCheck { get { return "LUBE_SERVICE_OIL_LEVEL"; } }
        public static string LubeServiceLevelTopUp { get { return "LUBE_SERVICE_LEVEL_TOPUP"; } }
        public static string RecomendationLubricant { get { return "LUBE_SERVICE_OPERATIONAL_CHECK"; } }
        public static string OilStandart { get { return "oilStandart"; } }
        public static string OilStandartCapacity { get { return "oilStandartCapacity"; } }
        public static string TaskKeyOilSample { get { return "taskKeyOilSample"; } }
        public static string HrsOnOil { get { return "hrsOnOil"; } }
        public static string Compartment { get { return "compartment"; } }
        public static string CompartmentLubricant { get { return "compartmentLubricant"; } }
        public static string RecomendedLubricant { get { return "recommendedLubricant"; } }
        public static string TaskKeyOilChange { get { return "taskKeyOilChange"; } }
        public static string TaskKeyOilLevelCheck { get { return "taskKeyOilLevelCheck"; } }
        public static string TaskTopUpLevelCheck { get { return "taskTopUpLevelCheck"; } }
        public static string OilChange { get { return "oilChange"; } }
        public static string OilAdded { get { return "oilAdded"; } }
        public static string FuelType { get { return "fuelType"; } }
        public static string SosPrintLabelLessDay { get { return "SosPrintLabelLessDay"; } }
        public static string Ownership { get { return "ownership"; } }
        public static string SiteDescription { get { return "siteDescription"; } }
        public static string BrandDescription { get { return "brandDescription"; } }
        public static string Brand { get { return "brand"; } }
        public static string TaskGroupKey { get { return "taskGroupKey"; } }
        public static string EhmOffset { get { return "ehmOffset"; } }
        public static string JobSite { get { return "jobSite"; } }
        public static string CustomerName { get { return "customerName"; } }
        public static string Name { get { return "name"; } }
        public static string MeterHrs { get { return "meterHrs"; } }
        public static string LubricantType { get { return "lubricantType"; } }
        public static string Uom { get { return "uoM"; } }
        public static string SSUom { get { return "uom"; } }
        public static string RecommendedLubricants { get { return "recommendedLubricants"; } }
        public static string Volume { get { return "volume"; } }
        public static string IsSOS { get { return "isSOS"; } }
        public static string IsError { get { return "isError"; } }
        public static string DisabledByItemKey { get { return "disabledByItemKey"; } }
        public static string Filename { get { return "filename"; } }
        public static string CbmHistory { get { return "history"; } }
        public static string ServiceSheetDetailId { get { return "serviceSheetDetailId"; } }
        public static string Type { get { return "type"; } }
        public static string Table { get { return "TABLE"; } }
        public static string Guide { get { return "GUIDE"; } }
        public static string MeasurementLocation { get { return "measurementLocation"; } }
        public static string MeasurementValue { get { return "measurementValue"; } }
        public static string ItemType { get { return "itemType"; } }
        public static string SmallCamera { get { return "smallCamera"; } }
        public static string DropdownTool { get { return "dropdownTool"; } }
        public static string BrakeTypeDropdown { get { return "brakeTypeDropdown"; } }
        public static string DropdownToolDisc { get { return "dropdownToolDisc"; } }
        public static string ResultParamRating { get { return "resultParamRating"; } }
        public static string SubTaskUnderscore { get { return "sub_task"; } }
        public static string ModelIdUnderscore { get { return "model_id"; } }
        public static string PsTypeUnderscore { get { return "ps_type"; } }
        public static string IsAllowed { get { return "isAllowed"; } }
        public static string Items { get { return "items"; } }
        public static string DropDown { get { return "dropDown"; } }
        public static string MappingKeyId { get { return "mappingKeyId"; } }
        public static string SuspensionCylinder { get { return "suspensionCylinder"; } }
        public static string Priority { get { return "priority"; } }

        public static string priorityType { get { return "priorityType "; } }
        public static string NonCbmAdjustmentReplacementMeasurementValue { get { return "nonCbmAdjustmentReplacementMeasurementValue"; } }
        public static string NonCbmAdjustmentReplacementRating { get { return "nonCbmAdjustmentReplacementRating"; } }
        public static string Section { get { return "section"; } }
        public static string CommentId { get { return "commentId"; } }
        public static string CurrentValue { get { return "currentValue"; } }
        public static string CurrentRating { get { return "currentRating"; } }
        public static string ReplacementValue { get { return "replacementValue"; } }
        public static string ReplacementRating { get { return "replacementRating"; } }
        public static string CbmAdjustmentReplacement { get { return "cbmAdjustmentReplacement"; } }
    }
}