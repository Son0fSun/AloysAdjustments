﻿using Decima;
using HZDUtility.Utility;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HZDUtility.Models;

namespace HZDUtility
{
    public partial class Main : Form
    {
        private const string ConfigPath = "config.json";

        private readonly Color _errorColor = Color.FromArgb(255, 51, 51);

        private bool _updatingLists = false;
        private bool _invalidConfig = true;

        private Logic Logic { get; set; }

        private OutfitMap[] DefaultMaps { get; set; }
        private OutfitMap[] NewMaps { get; set; }

        private List<Outfit> Outfits { get; set; }
        private List<Model> Models { get; set; }

        public Main()
        {
            InitializeComponent();

            SetupOutfitList();
            RTTI.SetGameMode(GameType.HZD);

            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            SetStatus($"Error: {e.ExceptionObject}", true);
        }

        private void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            SetStatus($"Error: {e.Exception.Message}", true);
        }

        private void SetupOutfitList()
        {
            lbOutfits.DrawMode = DrawMode.OwnerDrawVariable;
            lbOutfits.ItemHeight = lbOutfits.Font.Height + 2;
            lbOutfits.DrawItem += (s, e) =>
            {
                var l = (ListBox)s;
                
                if (e.State.HasFlag(DrawItemState.Selected))
                {
                    e.DrawBackground();
                }
                else
                {
                    var backColor = Outfits?.Any() == true && Outfits[e.Index].Modified ? Color.LightSkyBlue : e.BackColor;

                    using (var b = new SolidBrush(backColor))
                        e.Graphics.FillRectangle(b, e.Bounds);
                }
                
                using (var b = new SolidBrush(e.ForeColor))
                {
                    e.Graphics.DrawString(l.Items[e.Index].ToString(),
                        e.Font, b, e.Bounds, StringFormat.GenericDefault);
                }
                e.DrawFocusRectangle();
            };
        }

        public void SetStatus(string text, bool error = false)
        {
            this.TryBeginInvoke(() =>
            {
                tssStatus.Text = text;
                tssStatus.ForeColor = error ? _errorColor : SystemColors.ControlText;
            });
        }

        private async void Main_Load(object sender, EventArgs e)
        {
            SetStatus("Loading config");
            Logic = await Logic.FromConfig(ConfigPath);

            tbGameDir.EnableTypingEvent = false;
            tbGameDir.Text = Logic.Config.Settings.GamePath;
            tbGameDir.EnableTypingEvent = true;

            await Initialize();
        }

        private async Task Initialize()
        {
            var game = UpdateGameDirStatus();
            var decima = UpdateDecimaStatus();

            if (!game)
            {
                SetStatus("Missing Game Folder", true);
                return;
            }

            if (decima)
            {
                SetStatus("Generating outfit maps");
                DefaultMaps = await Logic.GenerateOutfitMaps();
                NewMaps = DefaultMaps.Select(x => x.Clone()).ToArray();

                SetStatus("Loading outfit list");
                Outfits = await Logic.LoadOutfitList();
                lbOutfits.Items.Clear();
                foreach (var item in Outfits)
                    lbOutfits.Items.Add(item);

                SetStatus("Loading models list");
                Models = await Logic.LoadModelList();
                clbModels.Items.Clear();
                foreach (var item in Models)
                    clbModels.Items.Add(item);

                _invalidConfig = false;
                SetStatus("Loading complete");
            }
            else
            {
                SetStatus("Missing Decima", true);
            }
        }

        private Model FindMatchingModel(Outfit outfit)
        {
            //get first reference with same outfit id from the default mapping
            var mapRef = DefaultMaps.SelectMany(x => x.Refs).FirstOrDefault(x => x.ModelId.Equals(outfit.Id));

            if (mapRef.RefId == null)
                return null;

            //find the mapped outfit in the new mapping
            var newRef = NewMaps.SelectMany(x => x.Refs).FirstOrDefault(x => x.RefId.Equals(mapRef.RefId));

            return Models.FirstOrDefault(x => x.Id.Equals(newRef.ModelId));
        }

        private void UpdateMapping(Outfit outfit, Model model)
        {
            //get all references with same outfit id from the default mapping
            var mapRefs = DefaultMaps.SelectMany(x => x.Refs)
                .Where(x => x.ModelId.Equals(outfit.Id)).Select(x=>x.RefId)
                .ToHashSet();

            outfit.Modified = !outfit.Id.Equals(model.Id);

            //find the outfit in the new mapping by reference and update the model
            foreach (var map in NewMaps)
            {
                foreach (var reference in map.Refs.Where(x=> mapRefs.Contains(x.RefId)))
                {
                    reference.ModelId.AssignFromOther(model.Id);
                }
            }
        }

        private void lbOutfits_SelectedValueChanged(object sender, EventArgs e)
        {
            _updatingLists = true;

            var lb = (ListBox)sender;

            var outfits = lb.SelectedItems.Cast<Outfit>()
                .Select(FindMatchingModel).Where(x => x != null).ToHashSet();

            for (int i = 0; i < clbModels.Items.Count; i++)
            {
                if (outfits.Contains(Models[i]))
                    clbModels.SetItemCheckState(i, outfits.Count > 1 ? CheckState.Indeterminate : CheckState.Checked);
                else
                    clbModels.SetItemCheckState(i, CheckState.Unchecked);
            }

            _updatingLists = false;
        }

        private async void btnPatch_Click(object sender, EventArgs e)
        {
            btnPatch.Enabled = false;

            SetStatus("Generating patch...");
            var patch = await Logic.GeneratePatch(NewMaps);

            SetStatus("Copying patch...");
            await Logic.InstallPatch(patch);

            //await FileManager.Cleanup(Logic.Config.TempPath);

            SetStatus("Patch installed");

            btnPatch.Enabled = true;
        }

        private void clbModels_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_updatingLists)
                return;
            _updatingLists = true;

            if (e.CurrentValue == CheckState.Indeterminate)
                e.NewValue = CheckState.Checked;

            if (e.NewValue == CheckState.Checked)
            {
                for (int i = 0; i < clbModels.Items.Count; i++)
                {
                    if (i != e.Index)
                        clbModels.SetItemCheckState(i, CheckState.Unchecked);
                }

                var model = Models[e.Index];

                foreach (var outfit in lbOutfits.SelectedItems.Cast<Outfit>())
                    UpdateMapping(outfit, model);
                
                lbOutfits.Invalidate();
            }

            _updatingLists = false;
        }

        private async void btnDecima_Click(object sender, EventArgs e)
        {
            SetStatus("Downloading Decima...");
            await Logic.Decima.Download();
            SetStatus("Copying Decima library...");
            await Logic.Decima.GetLibrary();
            SetStatus("Decima updated");
            
            if (UpdateDecimaStatus() && _invalidConfig)
                await Initialize();
        }

        private async void btnLoadPatch_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.CheckFileExists = true;
                ofd.Multiselect = false;
                ofd.Filter = "Pack files (*.bin)|*.bin|All files (*.*)|*.*";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    SetStatus("Loading pack...");
                    NewMaps = await Logic.GenerateOutfitMapsFromPack(ofd.FileName);

                    //should be 1-1 relationship for the default outfits
                    var defaultRefs = new Dictionary<BaseGGUUID, BaseGGUUID>();
                    foreach (var sRef in DefaultMaps.SelectMany(x => x.Refs))
                        defaultRefs[sRef.ModelId] = sRef.RefId;

                    var newRefs = NewMaps.SelectMany(x => x.Refs)
                        .ToDictionary(x => x.RefId, x => x.ModelId);

                    foreach (var outfit in Outfits)
                    {
                        if (defaultRefs.TryGetValue(outfit.Id, out var defaultRefId))
                        {
                            outfit.Modified = newRefs.TryGetValue(defaultRefId, out var newModelId) && 
                                !outfit.Id.Equals(newModelId);
                        }
                    }

                    RefreshLists();

                    Logic.Config.Settings.LastOpen = ofd.FileName;
                    await Logic.SaveConfig();

                    SetStatus($"Loaded pack: {Path.GetFileName(ofd.FileName)}");
                }
            }
        }

        private void lbOutfits_KeyDown(object sender, KeyEventArgs e)
        {
            var lb = (ListBox)sender;

            if (e.KeyCode == Keys.A && e.Control)
            {
                for (int i = 0; i < lb.Items.Count; i++)
                    lb.SetSelected(i, true);
                e.SuppressKeyPress = true;
            }
        }

        private async void btnReset_Click(object sender, EventArgs e)
        {
            btnReset.Enabled = false;

            await Initialize();
            RefreshLists();

            SetStatus("Reset complete");

            btnReset.Enabled = true;
        }

        private void RefreshLists()
        {
            lbOutfits.ClearSelected();
            lbOutfits.Invalidate();
        }

        private void tbGameDir_TextChanged(object sender, EventArgs e)
        {
            Logic.Config.Settings.GamePath = tbGameDir.Text;
        }
        private async void tbGameDir_TypingFinished(object sender, EventArgs e)
        {
            await Logic.SaveConfig();
            if (UpdateGameDirStatus() && _invalidConfig)
                await Initialize();

        }

        private bool UpdateGameDirStatus()
        {
            if (Logic.CheckGameDir())
            {
                lblGameDir.Text = "Game Folder";
                lblGameDir.ForeColor = SystemColors.ControlText;
                return true;
            }
            else
            {
                lblGameDir.Text = "Game Folder - Invalid";
                lblGameDir.ForeColor = _errorColor;
                return false;
            }
        }
        private bool UpdateDecimaStatus()
        {
            if (Logic.Decima.CheckDecima())
            {
                lblDecima.Text = "Decima";
                lblDecima.ForeColor = SystemColors.ControlText;
                return true;
            }
            else
            {
                lblDecima.Text = "Decima - Invalid";
                lblDecima.ForeColor = _errorColor;
                return false;
            }
        }

        private async void btnGameDir_Click(object sender, EventArgs e)
        {
            using var ofd = new FolderBrowserDialog();

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                tbGameDir.EnableTypingEvent = false;
                tbGameDir.Text = ofd.SelectedPath;
                tbGameDir.EnableTypingEvent = true;

                if (UpdateGameDirStatus() && _invalidConfig)
                    await Initialize();
            }
        }
    }
}