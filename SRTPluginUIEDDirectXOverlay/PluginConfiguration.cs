using System;
using System.Collections.Generic;

namespace SRTPluginUIEDDirectXOverlay
{
    public class PluginConfiguration
    {
        //public bool Debug { get; set; }
        //public bool NoInventory { get; set; }
        public float ScalingFactor { get; set; }

        public float PositionX { get; set; }
        public float PositionY { get; set; }

        //public float InventoryPositionX { get; set; }
        //public float InventoryPositionY { get; set; }

        public string StringFontName { get; set; }
        public bool ShowBossStatus { get; set; }

        public PluginConfiguration()
        {
            //Debug = false;
            //NoInventory = true;
            ScalingFactor = 1f;
            PositionX = 5f;
            PositionY = 50f;
            ShowBossStatus = true;
            //InventoryPositionX = -1;
            //InventoryPositionY = -1;
            StringFontName = "Courier New";
        }
    }
}
