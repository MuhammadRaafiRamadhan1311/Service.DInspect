using System;

namespace Service.DInspect.Models.ADM
{
    public class ShiftModel
    {
        public long shiftId { get; set; }
        public string shift { get; set; }
        public TimeSpan startHour { get; set; }
        public string startHourType { get; set; }
        public TimeSpan startHour24
        {
            get
            {
                DateTime date = DateTime.Parse($"{startHour} {startHourType}");
                return date.TimeOfDay;
            }
        }
        public TimeSpan endHour { get; set; }
        public string endHourType { get; set; }
        public TimeSpan endHour24
        {
            get
            {
                DateTime date = DateTime.Parse($"{endHour} {endHourType}");
                return date.TimeOfDay;
            }
        }
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        public bool isActive { get; set; }
        public string createdBy { get; set; }
        public DateTime createdOn { get; set; }
        public string changedBy { get; set; }
        public DateTime changedOn { get; set; }
    }
}