using Service.DInspect.Models.Helper;

namespace Service.DInspect.Models.Response
{
    public class PreviousReplacementResponse : PreviousTandomResponse
    {
        public ReplacementHelperModel replacement { get; set; }
    }
}
