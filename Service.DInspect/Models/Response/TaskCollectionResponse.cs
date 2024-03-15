using System.Collections.Generic;

namespace Service.DInspect.Models.Response
{
    public class TaskCollectionResponse
    {
        public string ModelId { get; set; }
        public string PsTypeId { get; set; }
        public string Version { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string SubTask { get; set; }
        public string Rating { get; set; }
        public string Status { get; set; }
        public string ReleaseDate { get; set; }
    }
}
