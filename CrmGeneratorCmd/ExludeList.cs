using System.Collections.Generic;

namespace PureCrmGeneratorCmd
{
    public static class ExludeList
    {
        public static List<string> Entities = new List<string>()
        {
            "duplicaterule",
            "processtrigger",
            "thk_contractnamemaster",
            "importmap",
            "quote",
            "sla",
            "activitymimeattachment"
        };
        public static List<string> Attrs = new List<string>()
        {
            "attachment",
            "event",
            "namespace",
            "abstract"
        };
    }
}
