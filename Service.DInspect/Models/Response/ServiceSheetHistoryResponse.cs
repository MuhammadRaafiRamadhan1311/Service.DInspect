using Service.DInspect.Models.Entity;
using System.Collections.Generic;

namespace Service.DInspect.Models.Response
{
    public class ServiceSheetHistoryResponse
    {
        public string status { get; set; }
        public int dataCount { get; set; }
        public TypeMonitoringStatus data { get; set; }
    }

    public class TypeMonitoringStatus
    {
        public List<DailyScheduleModel> Servicesheet { get; set; }
        public List<dynamic> Intervention { get; set; }
        public List<dynamic> InterimEngine { get; set; }
    }

    public class test
    {
        public dynamic content { get; set; }
    }
}
