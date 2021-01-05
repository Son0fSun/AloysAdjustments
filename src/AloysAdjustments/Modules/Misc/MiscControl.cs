﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AloysAdjustments.Configuration;
using AloysAdjustments.Data;
using AloysAdjustments.Logic;
using AloysAdjustments.Utility;

namespace AloysAdjustments.Modules.Misc
{
    public partial class MiscControl : ModuleBase
    {
        private bool _loading;

        private MiscLogic MiscLogic { get; }
        private MiscAdjustments Adjustments { get; set; }
        
        public override string ModuleName => "Misc";

        public MiscControl()
        {
            _loading = true;

            IoC.Bind(Configs.LoadModuleConfig<MiscConfig>(ModuleName));

            MiscLogic = new MiscLogic();

            InitializeComponent();

            _loading = false;
        }

        public override async Task Initialize()
        {
            IoC.Notif.ShowUnknownProgress();

            IoC.Notif.ShowStatus("Loading misc data...");
            Adjustments = await MiscLogic.GenerateMiscData();

            RefreshControls();
        }

        public override async Task LoadPatch(string path)
        {
            await Initialize();

            IoC.Notif.ShowStatus("Loading misc data...");
            Adjustments = await MiscLogic.GenerateMiscData();
            var newAdj = await MiscLogic.GenerateMiscDataFromPath(path, false);

            if (newAdj.SkipIntroLogos.HasValue) Adjustments.SkipIntroLogos = newAdj.SkipIntroLogos;

            RefreshControls();
        }

        public override async Task CreatePatch(string patchDir)
        {
            await MiscLogic.CreatePatch(patchDir, Adjustments);
        }


        private async Task Reload()
        {
            await Initialize();

            IoC.Notif.HideProgress();
        }

        protected override async void Reset_Click()
        {
            using var _ = new ControlLock(Reset);

            await Reload();
            IoC.Notif.ShowStatus("Reset complete");
        }
        
        private void cbIntroLogos_CheckedChanged(object sender, EventArgs e)
        {
            if (_loading) return;
            Adjustments.SkipIntroLogos = cbIntroLogos.Checked;
        }

        private void RefreshControls()
        {
            _loading = true;

            cbIntroLogos.Checked = Adjustments.SkipIntroLogos == true;

            _loading = false;
        }
    }
}
