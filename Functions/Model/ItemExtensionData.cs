using System.Collections.Generic;

namespace Functions.Model
{
    public class ItemExtensionData
    {
        public string Item { get; set; }
        public string ItemId { get; set; }
        public IList<EvaluatedRule> Rules { get; set; }
        public string CiIdentifiers { get; set; }
    }
}