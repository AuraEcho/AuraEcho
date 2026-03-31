using System;
using System.Collections.Generic;

namespace AuraEcho.Setup.UI.WixToolset
{
    public class RelatedBundleInfo
    {
        public Version Version { get; set; } = new Version();
        public Dictionary<string, bool> FeatureStatus { get; set; } = new Dictionary<string, bool>();
        public string InstallationFolder { get; set; }
    }
}
