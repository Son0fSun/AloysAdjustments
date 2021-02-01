﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AloysAdjustments.Configuration;
using AloysAdjustments.Logic;
using AloysAdjustments.Logic.Patching;
using AloysAdjustments.Modules.Settings;
using AloysAdjustments.Plugins;
using AloysAdjustments.UI;
using Decima;
using Decima.HZD;
using AloysAdjustments.Utility;
using Newtonsoft.Json;
using Application = System.Windows.Forms.Application;

namespace AloysAdjustments
{
    public partial class Main : Form
    {
        private const string ConfigPath = "config.json";
        
        private bool _initialized = false;

        private List<IInteractivePlugin> Plugins { get; set; }
        private SettingsControl Settings { get; set; }
        public PluginManager PluginManager { get; }

        public Main()
        {
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            IoC.Bind(new Notifications(SetStatus, SetAppStatus, SetProgress));
            IoC.Bind(new Uuid());
            LoadConfigs();

            PluginManager = new PluginManager();

            InitializeComponent();

            WindowMemory.ActivateWindow(this, "Main");
            
            RTTI.SetGameMode(GameType.HZD);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            SetStatus($"Error: {e.ExceptionObject}", true);
            SetProgress(0, 0, false, false);
            Errors.WriteError(e.ExceptionObject);
        }

        private void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            SetStatus($"Error: {e.Exception.Message}", true);
            SetProgress(0, 0, false, false);
            Errors.WriteError(e.Exception);
        }

        public void SetAppStatus(string text)
        {
            this.TryBeginInvoke(() =>
            {
                tssAppStatus.Text = text;
            });
        }

        public void SetStatus(string text, bool error)
        {
            this.TryBeginInvoke(() =>
            {
                tssStatus.Text = text;
                tssStatus.ForeColor = error ? UIColors.ErrorColor : SystemColors.ControlText;
            });
        }

        public void SetProgress(int current, int max, bool unknown, bool visible)
        {
            this.TryBeginInvoke(() =>
            {
                tssAppStatus.Visible = !visible;
                tpbStatus.Visible = visible;
                if (visible)
                {
                    tpbStatus.Style = unknown ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
                    if (!unknown)
                    {
                        tpbStatus.Maximum = max;
                        tpbStatus.Value = current;
                    }
                }
            });
        }

        private async void Main_LoadCommand(object sender, EventArgs e) => await Relay.To(sender, e, Main_Load);
        private async Task Main_Load(object sender, EventArgs e)
        {
            IoC.Notif.ShowStatus("Loading config...");

            IoC.Bind(new Archiver());
            IoC.Bind(new Localization(ELanguage.English));

            Settings = new SettingsControl();
            Plugins = PluginManager.LoadAll<IInteractivePlugin>()
                .OrderBy(x => x.PluginName.Contains("Misc") ? 1 : 0) //TODO: fix
                .ToList();

            IoC.Notif.ShowStatus("Loading Plugins...");
            foreach (var module in Plugins.AsEnumerable().Concat(new[] { Settings }).Reverse())
            {
                var tab = new TabPage();

                tab.UseVisualStyleBackColor = true;
                tab.Text = module.PluginName;
                tab.Controls.Add(module.PluginControl);
                module.PluginControl.Dock = DockStyle.Fill;

                tcMain.TabPages.Insert(0, tab);
            }

            await Settings.Initialize();
            Settings.SettingsOkay += Settings_SettingsOkayCommand;

            tcMain.SelectedIndex = 0;
            if (!await InitializePlugins())
            {
                tcMain.SelectedIndex = tcMain.TabPages.Count - 1;
            }
        }

        private async void Settings_SettingsOkayCommand() => await Relay.To(Settings_SettingsOkay);
        private async Task Settings_SettingsOkay()
        {
            if (!_initialized)
                await InitializePlugins();
        }

        private async Task<bool> InitializePlugins()
        {
            if (!Settings.ValidateAll())
                return false;

            IoC.Notif.ShowUnknownProgress();

            await Async.Run(() =>
            {
                IoC.Notif.ShowStatus("Removing old version...");
                //remove failed / old patches
                Compatibility.CleanupOldVersions();
                FileBackup.CleanupBackups(Configs.PatchPath);
            });

            IoC.Notif.ShowStatus("Initializing Plugins...");
            foreach (var module in Plugins)
                await module.Initialize();

            _initialized = true;

            if (File.Exists(Configs.PatchPath))
                await LoadExistingPack(Configs.PatchPath, true);

            IoC.Notif.HideProgress();
            IoC.Notif.ShowStatus("Loading complete");
            return true;
        }

        private async void btnPatch_ClickCommand(object sender, EventArgs e) => await Relay.To(sender, e, btnPatch_Click);
        private async Task btnPatch_Click(object sender, EventArgs e)
        {
            using var _ = new ControlLock(btnPatch);

            IoC.Notif.ShowStatus("Generating patch...");
            IoC.Notif.ShowUnknownProgress();

            if (Plugins.Any() && !Plugins.All(x => x.ValidateChanges()))
            {
                IoC.Notif.HideProgress();
                IoC.Notif.ShowStatus("Patch install aborted");
                return;
            }

            var sw = new Stopwatch(); sw.Start();

            await Async.Run(CreatePatch);

            Settings.UpdatePatchStatus();

            IoC.Notif.HideProgress();
            IoC.Notif.ShowStatus($"Patch installed ({sw.Elapsed.TotalMilliseconds:n0} ms)");
        }

        private void CreatePatch()
        {
            //remove failed patches
            FileBackup.CleanupBackups(Configs.PatchPath);

            using (var oldPatch = new FileBackup(Configs.PatchPath))
            {
                var patcher = new Patcher();

                var patch = patcher.StartPatch();
                foreach (var module in Plugins)
                {
                    IoC.Notif.ShowStatus($"Generating patch ({module.PluginName})...");
                    module.ApplyChanges(patch);
                }

                IoC.Notif.ShowStatus("Generating plugin patches...");
                patcher.ApplyCustomPatches(patch, PluginManager);

                IoC.Notif.ShowStatus("Generating patch (rebuild prefetch)...");

                var p = Prefetch.Load(patch);
                if (p.Rebuild(patch))
                    p.Save();

                IoC.Notif.ShowStatus("Generating patch (packing)...");
                patcher.PackPatch(patch);

                IoC.Notif.ShowStatus("Copying patch...");
                patcher.InstallPatch(patch);

                oldPatch.Delete();

#if !DEBUG
                Paths.Cleanup(IoC.Config.TempPath);
#endif
            }
        }

        private async void btnLoadPatch_ClickCommand(object sender, EventArgs e) => await Relay.To(sender, e, btnLoadPatch_Click);
        private async Task btnLoadPatch_Click(object sender, EventArgs e)
        {
            using var _ = new ControlLock(btnLoadPatch);
            using var ofd = new OpenFileDialog
            {
                CheckFileExists = true,
                Multiselect = false,
                Filter = "Pack files (*.bin)|*.bin|All files (*.*)|*.*"
            };

            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            await LoadExistingPack(ofd.FileName, false);
        }

        private async Task LoadExistingPack(string path, bool initial)
        {
            IoC.Notif.ShowUnknownProgress();

            foreach (var module in Plugins)
                await module.LoadPatch(path);
            
            if (!initial)
            {
                IoC.Settings.LastPackOpen = path;

                IoC.Notif.HideProgress();
                IoC.Notif.ShowStatus($"Loaded pack: {Path.GetFileName(path)}");
            }
        }

        public void LoadConfigs()
        {
            var json = File.ReadAllText(ConfigPath);
            IoC.Bind(JsonConvert.DeserializeObject<Config>(json));
            IoC.Bind(SettingsManager.Load());
        }

        private void tcMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tcMain.SelectedIndex >= 0 && tcMain.SelectedIndex < Plugins.Count)
            {
                btnReset.Enabled = true;

                for (int i = 0; i < Plugins.Count; i++)
                {
                    if (i == tcMain.SelectedIndex)
                        EnableModule(Plugins[i]);
                    else
                        DisableModule(Plugins[i]);
                }
            }
            else
            {
                btnReset.Enabled = false;
                btnResetSelected.Enabled = false;
            }
        }

        private void EnableModule(IInteractivePlugin module)
        {
            module.Reset.PropertyValueChanged = (p, v) => Relay_PropertyValueChanged(btnReset, p, v);
            //module.Reset.FirePropertyChanges();

            module.ResetSelected.PropertyValueChanged = (p, v) => Relay_PropertyValueChanged(btnResetSelected, p, v);
            module.ResetSelected.FirePropertyChanges();
        }

        private void DisableModule(IInteractivePlugin module)
        {
            module.Reset.PropertyValueChanged = null;
            module.ResetSelected.PropertyValueChanged = null;
        }

        private void Relay_PropertyValueChanged(Control control, string propertyName, object value)
        {
            var pi = control.GetType().GetProperty(propertyName);
            pi?.SetValue(control, value);
        }

        private void btnResetSelected_Click(object sender, EventArgs e)
        {
            if (tcMain.SelectedIndex >= 0 && tcMain.SelectedIndex < Plugins.Count)
                Plugins[tcMain.SelectedIndex].ResetSelected.OnClick();
        }
        private void btnReset_Click(object sender, EventArgs e)
        {
            if (tcMain.SelectedIndex >= 0 && tcMain.SelectedIndex < Plugins.Count)
                Plugins[tcMain.SelectedIndex].Reset.OnClick();
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Updater?.TryLaunchUpdater(false);
        }
    }
}
