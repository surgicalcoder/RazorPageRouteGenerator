using System.Collections.Generic;

namespace GoLive.Generator.RazorPageRoute.Generator
{
    public class Settings
    {
        public string Namespace { get; set; }
        public string ClassName { get; set; }

        public string OutputToFile { get; set; }
        public List<string> OutputToFiles { get; set; } = new();

        public string DebugOutputFile { get; set; }

        public bool OutputLastCreatedTime { get; set; }
        
        public bool OutputExtensionMethod { get; set; }
    }
}