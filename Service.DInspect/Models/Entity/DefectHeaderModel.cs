using System;
using System.Collections.Generic;

namespace Service.DInspect.Models.Entity
{
    public class DefectHeaderModel
    {
        public string workorder { get; set; }
        public string form { get; set; } // e_Form Name
        public string serviceSheetDetailId { get; set; }
        public string interventionId { get; set; }
        public string interventionHeaderId { get; set; }
        public string category { get; set; } //Normal, CBM
        public string taskId { get; set; } //Task Key
        public string taskNo { get; set; } // 1, 2
        public string taskDesc { get; set; }
        public string priorityType { get; set; } // P1, P2
        public string defectWorkorder { get; set; }
        public string formDefect { get; set; } //DMPL - YES only
        public string defectType { get; set; } //Normal -- Yes/No
        public string taskValue { get; set; }
        public string repairedStatus { get; set; } // Repaired, Not-Repaired
        public string cbmNAStatus { get; set; } //Confirm, Not-Confirm
        public string cbmMeasurement { get; set; }
        public string cbmUom { get; set; }
        public string cbmImageKey { get; set; }
        public string cbmImageProp { get; set; }
        public string cbmRatingType { get; set; } // CBM Mounting & Leak
        public string isCbmAdjusment { get; set; }
        public EmployeeModel supervisor { get; set; }
        public string status { get; set; } // Submited Mechanic, OK Supervisor, Approved Defect
        public string plannerStatus { get; set; }
        public string declineReason { get; set; }
        public EmployeeModel declineBy { get; set; }
        public string declineDate { get; set; }
        //public EmployeeModel approveBy { get; set; }
        //public string approveDate { get; set; }
        public List<StatusHistoryModel> statusHistory { get; set; }
    }
    public class DefectHeaderWithIdModel : DefectHeaderModel
    {
        public Guid? id { get; set; }
        public Guid? defectHeaderId { get; set; }
        public Guid? defectDetailId { get; set; }
    }
}
