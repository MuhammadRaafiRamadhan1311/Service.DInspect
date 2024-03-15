using System.Numerics;
using System;

namespace Service.DInspect.Models.Helper
{
    public class GeneralFilterHelperModel
    {
        public BigInteger Id { get; set; }
        public string Code { get; set; }
        public string Group { get; set; }
        public string Description { get; set; }
        public string Value { get; set; }
        //public string created_by { get; set; }
        //public DateTime created_on { get; set; }
        //public string modified_by { get; set; }
        //public DateTime? modified_on { get; set; }
        //public DateTime? valid_from { get; set; }
        //public DateTime? valid_to { get; set; }
    }
}
