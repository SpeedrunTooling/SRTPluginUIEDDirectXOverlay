﻿using GameOverlay.Drawing;
using GameOverlay.Windows;
using SRTPluginBase;
using SRTPluginProviderED;
using SRTPluginProviderED.Structs;
using SRTPluginProviderED.Structs.GameStructs;
using SRTPluginUIEDDirectXOverlay;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Linq;

namespace SRTPluginUIRE7DirectXOverlay
{
    public class SRTPluginUIEDDirectXOverlay : PluginBase, IPluginUI
    {
        internal static PluginInfo _Info = new PluginInfo();

        public override IPluginInfo Info => _Info;
        public string RequiredProvider => "SRTPluginProviderED";
        private IPluginHostDelegates hostDelegates;
        private IGameMemoryED gameMemory;

        // DirectX Overlay-specific.
        private OverlayWindow _window;
        private Graphics _graphics;
        private SharpDX.Direct2D1.WindowRenderTarget _device;

        private Font _consolasBold;

        private SolidBrush _black;
        private SolidBrush _white;
        private SolidBrush _grey;
        private SolidBrush _darkred;
        private SolidBrush _red;
        private SolidBrush _lightred;
        private SolidBrush _lightyellow;
        private SolidBrush _lightgreen;
        private SolidBrush _lawngreen;
        private SolidBrush _goldenrod;
        private SolidBrush _greydark;
        private SolidBrush _greydarker;
        private SolidBrush _darkgreen;
        private SolidBrush _darkyellow;

        //private IReadOnlyDictionary<ItemEnumeration, SharpDX.Mathematics.Interop.RawRectangleF> itemToImageTranslation;
        //private IReadOnlyDictionary<Weapon, SharpDX.Mathematics.Interop.RawRectangleF> weaponToImageTranslation;
        //private SharpDX.Direct2D1.Bitmap _invItemSheet1;
        //private SharpDX.Direct2D1.Bitmap _invItemSheet2;
        //private int INV_SLOT_WIDTH;
        //private int INV_SLOT_HEIGHT;
        public PluginConfiguration config;
        private Process GetProcess() => Process.GetProcessesByName("eldenring")?.FirstOrDefault();
        private Process gameProcess;
        private IntPtr gameWindowHandle;

        //STUFF
        SolidBrush HPBarColor;
        SolidBrush TextColor;

        private string PlayerName = "";

        [STAThread]
        public override int Startup(IPluginHostDelegates hostDelegates)
        {
            this.hostDelegates = hostDelegates;
            config = LoadConfiguration<PluginConfiguration>();

            gameProcess = GetProcess();
            if (gameProcess == default)
                return 1;
            gameWindowHandle = gameProcess.MainWindowHandle;

            DEVMODE devMode = default;
            devMode.dmSize = (short)Marshal.SizeOf<DEVMODE>();
            PInvoke.EnumDisplaySettings(null, -1, ref devMode);

            // Create and initialize the overlay window.
            _window = new OverlayWindow(0, 0, devMode.dmPelsWidth, devMode.dmPelsHeight);
            _window?.Create();

            // Create and initialize the graphics object.
            _graphics = new Graphics()
            {
                MeasureFPS = false,
                PerPrimitiveAntiAliasing = false,
                TextAntiAliasing = true,
                UseMultiThreadedFactories = false,
                VSync = false,
                Width = _window.Width,
                Height = _window.Height,
                WindowHandle = _window.Handle
            };
            _graphics?.Setup();

            // Get a refernence to the underlying RenderTarget from SharpDX. This'll be used to draw portions of images.
            _device = (SharpDX.Direct2D1.WindowRenderTarget)typeof(Graphics).GetField("_device", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_graphics);

            _consolasBold = _graphics?.CreateFont("Consolas", 12, true);

            _black = _graphics?.CreateSolidBrush(0, 0, 0);
            _white = _graphics?.CreateSolidBrush(255, 255, 255);
            _grey = _graphics?.CreateSolidBrush(128, 128, 128);
            _greydark = _graphics?.CreateSolidBrush(64, 64, 64);
            _greydarker = _graphics?.CreateSolidBrush(24, 24, 24, 100);
            _darkred = _graphics?.CreateSolidBrush(153, 0, 0, 100);
            _darkgreen = _graphics?.CreateSolidBrush(0, 102, 0, 100);
            _darkyellow = _graphics?.CreateSolidBrush(218, 165, 32, 100);
            _red = _graphics?.CreateSolidBrush(255, 0, 0);
            _lightred = _graphics?.CreateSolidBrush(255, 183, 183);
            _lightyellow = _graphics?.CreateSolidBrush(255, 255, 0);
            _lightgreen = _graphics?.CreateSolidBrush(0, 255, 0);
            _lawngreen = _graphics?.CreateSolidBrush(124, 252, 0);
            _goldenrod = _graphics?.CreateSolidBrush(218, 165, 32);
            HPBarColor = _grey;
            TextColor = _white;

            //if (!config.NoInventory)
            //{
            //    INV_SLOT_WIDTH = 112;
            //    INV_SLOT_HEIGHT = 112;
            //
            //    _invItemSheet1 = ImageLoader.LoadBitmap(_device, Properties.Resources.ui0100_iam_texout);
            //    _invItemSheet2 = ImageLoader.LoadBitmap(_device, Properties.Resources.ui0100_wp_iam_texout);
            //    GenerateClipping();
            //}

            return 0;
        }

        public override int Shutdown()
        {
            SaveConfiguration(config);

            //weaponToImageTranslation = null;
            //itemToImageTranslation = null;
            //
            //_invItemSheet2?.Dispose();
            //_invItemSheet1?.Dispose();

            _black?.Dispose();
            _white?.Dispose();
            _grey?.Dispose();
            _greydark?.Dispose();
            _greydarker?.Dispose();
            _darkred?.Dispose();
            _darkgreen?.Dispose();
            _darkyellow?.Dispose();
            _red?.Dispose();
            _lightred?.Dispose();
            _lightyellow?.Dispose();
            _lightgreen?.Dispose();
            _lawngreen?.Dispose();
            _goldenrod?.Dispose();

            _consolasBold?.Dispose();

            _device = null; // We didn't create this object so we probably shouldn't be the one to dispose of it. Just set the variable to null so the reference isn't held.
            _graphics?.Dispose(); // This should technically be the one to dispose of the _device object since it was pulled from this instance.
            _graphics = null;
            _window?.Dispose();
            _window = null;

            gameProcess?.Dispose();
            gameProcess = null;

            return 0;
        }

        public int ReceiveData(object gameMemory)
        {
            this.gameMemory = (IGameMemoryED)gameMemory;
            _window?.PlaceAbove(gameWindowHandle);
            _window?.FitTo(gameWindowHandle, true);

            try
            {
                _graphics?.BeginScene();
                _graphics?.ClearScene();

                if (config.ScalingFactor != 1f)
                    _device.Transform = new SharpDX.Mathematics.Interop.RawMatrix3x2(config.ScalingFactor, 0f, 0f, config.ScalingFactor, 0f, 0f);
                else
                    _device.Transform = new SharpDX.Mathematics.Interop.RawMatrix3x2(1f, 0f, 0f, 1f, 0f, 0f);
                DrawOverlay();
                
            }
            catch (Exception ex)
            {
                hostDelegates.ExceptionMessage.Invoke(ex);
            }
            finally
            {
                _graphics?.EndScene();
            }

            return 0;
        }

        private void SetColors()
        {
            foreach (KeyValuePair<string, int> bossOffset in GamePlayer.bossStatusOffsets)
            {

                if (gameMemory.BossStatus[bossOffset.Key] == 0)
                {
                    TextColor = _lawngreen;
                    return;
                }
                else
                {
                    TextColor = _red;
                    return;
                }
            }
        }


        private void DrawOverlay()
        {
            float baseXOffset = config.PositionX;
            float baseYOffset = config.PositionY;

            // Player HP
            float statsXOffset = baseXOffset + 5f;
            float statsYOffset = baseYOffset + 0f;
            float textOffsetX = 0f;

            // Map IDs
            int[] Limgrave = {-10000, 10000, 61000, 30110, 30020 };
            int[] Liurnia = { 62000, 62001, 30030, 30050, 14000 };

            int counter = 0;
            SetColors();

            
            foreach (KeyValuePair<string, int> bossOffset in GamePlayer.bossStatusOffsets)
            {
                
                string status = gameMemory.BossStatus[bossOffset.Key] == 0 || gameMemory.BossStatus[bossOffset.Key] == 104 ? "Alive" : "Dead";
                //Limgrave
                if (Limgrave.Contains(gameMemory.RegionID) || gameMemory.RegionID >= 6100000 && gameMemory.RegionID <= 6109999)
                {
                    if (counter <= 22 && status.Equals("Alive")) // Only display the first three items
                    {
                        if (counter == 0)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Limgrave", "");
                        }
                        DrawTextBlock(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                    else if(counter <= 22 && status.Equals("Dead"))
                    {
                        if (counter == 0)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Limgrave", "");
                        }
                        DrawTextBlockRed(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                }
                //Weepin Weepin Peninsula (Limgrave)
                else if (gameMemory.RegionID == 61002 || gameMemory.RegionID >= 6110000 && gameMemory.RegionID <= 6119999)
                {
                    if (counter >= 23 && counter <= 32 && status.Equals("Alive")) // Only display the 4th and 5th items
                    {
                        if (counter == 23)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Weepin Weepin Peninsula", "");
                        }
                        DrawTextBlock(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    } else if(counter >=23 && counter <= 32 && status.Equals("Dead"))
                    {
                        if (counter == 23)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Weepin Weepin Peninsula", "");
                        }
                        DrawTextBlockRed(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                }
                //Liurnia
                else if (Liurnia.Contains(gameMemory.RegionID) || gameMemory.RegionID >= 6200000 && gameMemory.RegionID <= 6299999)
                {
                    if (counter >= 33 && counter <= 60 && status.Equals("Alive")) // Only display the 4th and 5th items
                    {
                        if (counter == 33)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Liurnia Lake", "");
                        }
                        DrawTextBlock(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                    else if (counter >= 33 && counter <= 60 && status.Equals("Dead"))
                    {
                        if (counter == 33)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Liurnia Lake", "");
                        }
                        DrawTextBlockRed(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                }
                //Caelid
                else if (gameMemory.RegionID == 64000 || gameMemory.RegionID == 32070 || gameMemory.RegionID >= 6400000 && gameMemory.RegionID <= 6409999)
                {
                    if (counter >= 61 && counter <= 75 && status.Equals("Alive")) // Only display the 4th and 5th items
                    {
                        if (counter == 61)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Caelid", "");
                        }
                        DrawTextBlock(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                    else if (counter >= 61 && counter <= 75 && status.Equals("Dead"))
                    {
                        if (counter == 61)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Caelid", "");
                        }
                        DrawTextBlockRed(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                }
                //Dragonbarrow (Caelid)
                else if (gameMemory.RegionID == 64001 || gameMemory.RegionID >= 6410000 && gameMemory.RegionID <= 6419999)
                {
                    if (counter >= 76 && counter <= 86 && status.Equals("Alive")) // Only display the 4th and 5th items
                    {
                        if (counter == 76)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Dragonbarrow", "");
                        }
                        DrawTextBlock(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                    else if (counter >= 76 && counter <= 86 && status.Equals("Dead"))
                    {
                        if (counter == 76)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Dragonbarrow", "");
                        }
                        DrawTextBlockRed(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                }
                //Altus Plateau
                else if (gameMemory.RegionID == 63000 || gameMemory.RegionID == 32050 || gameMemory.RegionID >= 6300000 && gameMemory.RegionID <= 6309999)
                {
                    if (counter >= 87 && counter <= 106 && status.Equals("Alive")) // Only display the 4th and 5th items
                    {
                        if (counter == 87)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Altus Plateau", "");
                        }
                        DrawTextBlock(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                    else if (counter >= 76 && counter <= 106 && status.Equals("Dead"))
                    {
                        if (counter == 76)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Altus Plateau", "");
                        }
                        DrawTextBlockRed(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                }
                //Capital Outskirts
                else if (gameMemory.RegionID == 63002|| gameMemory.RegionID >= 6320000 && gameMemory.RegionID <= 6329999)
                {
                    if (counter >= 107 && counter <= 113 && status.Equals("Alive")) // Only display the 4th and 5th items
                    {
                        if (counter == 107)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Capital Outskirts", "");
                        }
                        DrawTextBlock(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                    else if (counter >= 107 && counter <= 113 && status.Equals("Dead"))
                    {
                        if (counter == 107)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Capital Outskirts", "");
                        }
                        DrawTextBlockRed(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                }
                //Leyendell, Royal Capital
                else if (gameMemory.RegionID == 60000 || gameMemory.RegionID == 35000 || gameMemory.RegionID >= 6000000 && gameMemory.RegionID <= 6009999)
                {
                    if (counter >= 114 && counter <= 117 && status.Equals("Alive")) // Only display the 4th and 5th items
                    {
                        if (counter == 114)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Leyendell, Royal Capital", "");
                        }
                        DrawTextBlock(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                    else if (counter >= 114 && counter <= 117 && status.Equals("Dead"))
                    {
                        if (counter == 114)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Leyendell, Royal Capital", "");
                        }
                        DrawTextBlockRed(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                }
                //Mt. Gelmir
                else if (gameMemory.RegionID == 63001 || gameMemory.RegionID >= 6310000 && gameMemory.RegionID <= 6319999)
                {
                    if (counter >= 118 && counter <= 127 && status.Equals("Alive")) // Only display the 4th and 5th items
                    {
                        if (counter == 118)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Mt. Gelmir", "");
                        }
                        DrawTextBlock(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                    else if (counter >= 118 && counter <= 127 && status.Equals("Dead"))
                    {
                        if (counter == 118)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Mt. Gelmir", "");
                        }
                        DrawTextBlockRed(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                }
                //Mountaintops of the Giants
                else if (gameMemory.RegionID == 65000 || gameMemory.RegionID == 31220 || gameMemory.RegionID >= 6500000 && gameMemory.RegionID <= 6509999)
                {
                    if (counter >= 128 && counter <= 136 && status.Equals("Alive")) // Only display the 4th and 5th items
                    {
                        if (counter == 128)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Mountaintops of the Giants", "");
                        }
                        DrawTextBlock(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                    else if (counter >= 128 && counter <= 136 && status.Equals("Dead"))
                    {
                        if (counter == 128)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Mountaintops of the Giants", "");
                        }
                        DrawTextBlockRed(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                }
                //Crumbling Farum Azula
                else if (gameMemory.RegionID == 13000)
                {
                    if (counter >= 137 && counter <= 139 && status.Equals("Alive")) // Only display the 4th and 5th items
                    {
                        if (counter == 137)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Crumbling Farum Azula", "");
                        }
                        DrawTextBlock(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                    else if (counter >= 137 && counter <= 139 && status.Equals("Dead"))
                    {
                        if (counter == 137)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Crumbling Farum Azula", "");
                        }
                        DrawTextBlockRed(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                }
                //Forbidden Lands
                else if (gameMemory.RegionID == 63003 || gameMemory.RegionID >= 6330000 && gameMemory.RegionID <= 6339999)
                {
                    if (counter >= 140 && counter <= 142 && status.Equals("Alive")) // Only display the 4th and 5th items
                    {
                        if (counter == 140)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Forbidden Lands", "");
                        }
                        DrawTextBlock(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                    else if (counter >= 140 && counter <= 142 && status.Equals("Dead"))
                    {
                        if (counter == 140)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Forbidden Lands", "");
                        }
                        DrawTextBlockRed(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                }
                //Consecrated Snowfields
                else if (gameMemory.RegionID == 65002 || gameMemory.RegionID == 32110 || gameMemory.RegionID >= 6510000 && gameMemory.RegionID <= 6519999)
                {
                    if (counter >= 143 && counter <= 149 && status.Equals("Alive")) // Only display the 4th and 5th items
                    {
                        if (counter == 143)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Consecrated Snowfields", "");
                        }
                        DrawTextBlock(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                    else if (counter >= 143 && counter <= 149 && status.Equals("Dead"))
                    {
                        if (counter == 143)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Consecrated Snowfields", "");
                        }
                        DrawTextBlockRed(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                }
                //Miquella's Haligtree
                else if (gameMemory.RegionID == 15000 || gameMemory.RegionID == 15001 || gameMemory.RegionID == 15002)
                {
                    if (counter >= 150 && counter <= 151 && status.Equals("Alive")) // Only display the 4th and 5th items
                    {
                        if (counter == 150)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Miquella's Haligtree", "");
                        }
                        DrawTextBlock(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                    else if (counter >= 150 && counter <= 151 && status.Equals("Dead"))
                    {
                        if (counter == 150)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Miquella's Haligtree", "");
                        }
                        DrawTextBlockRed(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                }
                //Siofra River
                else if (gameMemory.RegionID == 12070)
                {
                    if (counter >= 152 && counter <= 154 && status.Equals("Alive")) // Only display the 4th and 5th items
                    {
                        if (counter == 152)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Siofra River", "");
                        }
                        DrawTextBlock(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                    else if (counter >= 152 && counter <= 154 && status.Equals("Dead"))
                    {
                        if (counter == 152)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Siofra River", "");
                        }
                        DrawTextBlockRed(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                }
                //Ainsel River
                else if (gameMemory.RegionID == 9999)
                {
                    if (counter == 155 && status.Equals("Alive")) // Only display the 4th and 5th items
                    {
                        if (counter == 155)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Ainsel River", "");
                        }
                        DrawTextBlock(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                    else if (counter == 155 && status.Equals("Dead"))
                    {
                        if (counter == 155)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Ainsel River", "");
                        }
                        DrawTextBlockRed(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                }/*
                //Nokron Eternal City
                else if (gameMemory.RegionID == 12020)
                {
                    if (counter >= 156 && counter <= 158 && status.Equals("Alive")) // Only display the 4th and 5th items
                    {
                        if (counter == 156)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Nokron Eternal City", "");
                        }
                        DrawTextBlock(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                    else if (counter >= 156 && counter <= 158 && status.Equals("Dead"))
                    {
                        if (counter == 156)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Nokron Eternal City", "");
                        }
                        DrawTextBlockRed(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                }
                //Deeprot Depths
                else if (gameMemory.RegionID == 9999)
                {
                    if (counter >= 159 && counter <= 161 && status.Equals("Alive")) // Only display the 4th and 5th items
                    {
                        if (counter == 159)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Deeprot Depths", "");
                        }
                        DrawTextBlock(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                    else if (counter >= 159 && counter <= 161 && status.Equals("Dead"))
                    {
                        if (counter == 159)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Deeprot Depths", "");
                        }
                        DrawTextBlockRed(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                }
                Lake of Rot
                else if (gameMemory.RegionID == 99999)
                {
                    if (counter >= 162 && counter <= 163 && status.Equals("Alive")) // Only display the 4th and 5th items
                    {
                        if (counter == 162)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Lake of Rot", "");
                        }
                        DrawTextBlock(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                    else if (counter >= 162 && counter <= 163 && status.Equals("Dead"))
                    {
                        if (counter == 162)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Lake of Rot", "");
                        }
                        DrawTextBlockRed(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                }
                //Leyendell, Ashen Capital
                else if (gameMemory.RegionID == 11050)
                {
                    if (counter >= 164 && counter <= 165 && status.Equals("Alive")) // Only display the 4th and 5th items
                    {
                        if (counter == 164)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Leyendell, Ashen Capital", "");
                        }
                        DrawTextBlock(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                    else if (counter >= 164 && counter <= 165 && status.Equals("Dead"))
                    {
                        if (counter == 164)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Leyendell, Ashen Capital", "");
                        }
                        DrawTextBlockRed(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                }
                //Elden Throne
                else if (gameMemory.RegionID == 19000)
                {
                    if (counter == 166 && status.Equals("Alive")) // Only display the 4th and 5th items
                    {
                        if (counter == 166)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Elden Throne", "");
                        }
                        DrawTextBlock(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                    else if (counter == 166 && status.Equals("Dead"))
                    {
                        if (counter == 166)
                        {
                            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Elden Throne", "");
                        }
                        DrawTextBlockRed(ref textOffsetX, ref statsYOffset, bossOffset.Key, status);
                    }
                }*/

                counter++;
            }
        }

        private float GetStringSize(string str, float size = 20f)
        {
            return (float)_graphics?.MeasureString(_consolasBold, size, str).X;
        }

        private void DrawProgressBar(ref float xOffset, ref float yOffset, float chealth, float mhealth, float percentage = 1f)
        {
            string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
            float endOfBar = config.PositionX + 342f - GetStringSize(perc);
            _graphics.DrawRectangle(_greydark, xOffset, yOffset += 28f, xOffset + 342f, yOffset + 22f, 4f);
            _graphics.FillRectangle(_greydarker, xOffset + 1f, yOffset + 1f, xOffset + 340f, yOffset + 20f);
            _graphics.FillRectangle(_darkred, xOffset + 1f, yOffset + 1f, xOffset + (340f * percentage), yOffset + 20f);
            _graphics.DrawText(_consolasBold, 20f, _white, xOffset + 10f, yOffset - 2f, string.Format("{0} / {1}", chealth, mhealth));
            _graphics.DrawText(_consolasBold, 20f, _white, endOfBar, yOffset - 2f, perc);
        }

        private void DrawHealthBar(ref float xOffset, ref float yOffset, float chealth, float mhealth, float percentage = 1f)
        {
            string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
            float endOfBar = config.PositionX + 342f - GetStringSize(perc);
            _graphics.DrawRectangle(_greydark, xOffset, yOffset += 28f, xOffset + 342f, yOffset + 22f, 4f);
            _graphics.FillRectangle(_greydarker, xOffset + 1f, yOffset + 1f, xOffset + 340f, yOffset + 20f);
            _graphics.FillRectangle(HPBarColor, xOffset + 1f, yOffset + 1f, xOffset + (340f * percentage), yOffset + 20f);
            _graphics.DrawText(_consolasBold, 20f, TextColor, xOffset + 10f, yOffset - 2f, string.Format("{0}{1} / {2}", PlayerName, chealth, mhealth));
            _graphics.DrawText(_consolasBold, 20f, TextColor, endOfBar, yOffset - 2f, perc);
        }

        // DRAWS TEXT BLOCK ON SCREEN
        private void DrawTextBlock(ref float dx, ref float dy, string label, string val)
        {
            _graphics?.DrawText(_consolasBold, 20f, _white, config.PositionX + 15f, dy += 24, label);
            dx = config.PositionX + 15f + GetStringSize(label) + 10f;
            _graphics?.DrawText(_consolasBold, 20f, _lawngreen, dx, dy, val); //110f
        }

        private void DrawTextBlockRed(ref float dx, ref float dy, string label, string val)
        {
            _graphics?.DrawText(_consolasBold, 20f, _white, config.PositionX + 15f, dy += 24, label);
            dx = config.PositionX + 15f + GetStringSize(label) + 10f;
            _graphics?.DrawText(_consolasBold, 20f, _red, dx, dy, val); //110f
        }
    }
}
