using SRTPluginBase;
using System;

namespace SRTPluginUIEDDirectXOverlay
{
    internal class PluginInfo : IPluginInfo
    {
        public string Name => "DirectX Overlay UI (Elden Ring)";

        public string Description => "A DirectX-based Overlay User Interface for displaying Elden Ring game memory values.";

        public string Author => "Mysterion352";

        public Uri MoreInfoURL => new Uri("https://github.com/SpeedrunTooling/SRTPluginUIEDDirectXOverlay");

        public int VersionMajor => assemblyFileVersion.ProductMajorPart;

        public int VersionMinor => assemblyFileVersion.ProductMinorPart;

        public int VersionBuild => assemblyFileVersion.ProductBuildPart;

        public int VersionRevision => assemblyFileVersion.ProductPrivatePart;

        private System.Diagnostics.FileVersionInfo assemblyFileVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
    }
}
