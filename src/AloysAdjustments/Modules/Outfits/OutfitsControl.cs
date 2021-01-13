﻿using AloysAdjustments.Configuration;
using AloysAdjustments.Data;
using AloysAdjustments.Logic;
using AloysAdjustments.Utility;
using Decima;
using EnumsNET;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AloysAdjustments.UI;
using PresentationControls;

namespace AloysAdjustments.Modules.Outfits
{
    [Flags]
    public enum OutfitModelFilter
    {
        [Description("Armors")]
        Armor = 1,
        [Description("Unique Characters")]
        Characters = 2,
        [Description("All Characters")]
        AllCharacters = 4
    }

    public partial class OutfitsControl : ModuleBase
    {
        private bool _updatingLists;
        private bool _loading;

        private OutfitsLogic OutfitLogic { get; }
        private CharacterLogic CharacterLogic { get; }

        private HashSet<Outfit> DefaultOutfits { get; set; }
        private ReadOnlyCollection<Outfit> Outfits { get; set; }
        private ReadOnlyCollection<Model> Models { get; set; }

        private Outfit AllOutfitStub { get; }

        private ListWrapper<OutfitModelFilter> Filters { get; set; }

        public override string ModuleName => "Outfits";

        public OutfitsControl()
        {
            _loading = true;

            AllOutfitStub = new Outfit();
            AllOutfitStub.SetDisplayName("All Outfits");

            IoC.Bind(Configs.LoadModuleConfig<OutfitConfig>(ModuleName));

            OutfitLogic = new OutfitsLogic();
            CharacterLogic = new CharacterLogic();
            Outfits = new List<Outfit>().AsReadOnly();

            InitializeComponent();
            
            SetupLists();
            LoadSettings();

            _loading = false;
        }
        
        private void SetupLists()
        {
            Filters = new ListWrapper<OutfitModelFilter>(
                Enums.GetValues<OutfitModelFilter>(), x => x.AsString(EnumFormat.Description));
            ccbModelFilter.DataSource = Filters;
            ccbModelFilter.DisplayMemberSingleItem = "Name";
            ccbModelFilter.DisplayMember = "NameConcatenated";
            ccbModelFilter.ValueMember = "Selected";
            ccbModelFilter.DropDownHeight = (int)(ccbModelFilter.CheckBoxItems.Sum(x => x.Height) * 1.3);

            clbModels.DisplayMember = "DisplayName";

            lbOutfits.DisplayMember = "DisplayName";
            lbOutfits.DrawMode = DrawMode.OwnerDrawVariable;
            lbOutfits.ItemHeight = lbOutfits.Font.Height + 2;
            lbOutfits.DrawItem += (s, e) =>
            {
                if (e.Index < 0)
                    return;

                var l = (ListBox)s;

                if (e.State.HasFlag(DrawItemState.Selected))
                {
                    e.DrawBackground();
                }
                else
                {
                    var backColor = e.BackColor;
                    var all = IoC.Settings.ApplyToAllOutfits;

                    if ((all && Outfits.Any(x => x.Modified)) ||
                        (!all && Outfits.Count > e.Index && Outfits[e.Index].Modified))
                    {
                        backColor = Color.LightSkyBlue;
                    }

                    using (var b = new SolidBrush(backColor))
                        e.Graphics.FillRectangle(b, e.Bounds);
                }

                using (var b = new SolidBrush(e.ForeColor))
                {
                    var text = l.GetItemText(l.Items[e.Index]);
                    e.Graphics.DrawString(text, e.Font, b, e.Bounds, StringFormat.GenericDefault);
                }
                e.DrawFocusRectangle();
            };
        }

        private void LoadSettings()
        {
            cbAllOutfits.Checked = IoC.Settings.ApplyToAllOutfits;
            var filters = (OutfitModelFilter)IoC.Settings.OutfitModelFilter;

            foreach (var f in Filters.Where(x => filters.HasFlag(x.Item)))
                f.Selected = true;
        }

        public override async Task Initialize()
        {
            ResetSelected.Enabled = false;
            IoC.Notif.ShowUnknownProgress();

            IoC.Notif.ShowStatus("Loading outfit list...");
            DefaultOutfits = (await OutfitLogic.GenerateOutfits()).ToHashSet();
            
            var outfits = DefaultOutfits.Select(x => x.Clone()).ToList();
            await UpdateOutfitDisplayNames(outfits);
            Outfits = outfits.OrderBy(x => x.DisplayName).ToList().AsReadOnly();

            PopulateOutfitsList();

            var filter = (OutfitModelFilter)IoC.Settings.OutfitModelFilter;
            var noneFilter = filter == 0;

            var models = new List<Model>();
            if (noneFilter || filter.HasFlag(OutfitModelFilter.Armor))
                models.AddRange(await LoadOutfitModelList());
            if (noneFilter || filter.HasFlag(OutfitModelFilter.Characters))
                models.AddRange(await LoadCharacterModelList(false));
            if (filter.HasFlag(OutfitModelFilter.AllCharacters))
                models.AddRange(await LoadCharacterModelList(true));

            Models = models.AsReadOnly();

            UpdateModelDisplayNames(Outfits, Models);

            clbModels.Items.Clear();
            foreach (var item in Models)
                clbModels.Items.Add(item);
            
            UpdateAllOutfitsSelection();
        }

        private async Task<IEnumerable<Model>> LoadCharacterModelList(bool all)
        {
            IoC.Notif.ShowStatus("Loading characters list...");
            var models = await CharacterLogic.Search.GetCharacterModels(all);
            return models.OrderBy(x => x.ToString() + " - Character");
        }

        private async Task<IEnumerable<Model>> LoadOutfitModelList()
        {
            IoC.Notif.ShowStatus("Loading models list...");
            var models = await OutfitLogic.GenerateModelList();

            //sort models to match outfits
            var outfitSorting = Outfits.Select((x, i) => (x, i)).ToSoftDictionary(x => x.x.ModelId, x => x.i);
            return models.OrderBy(x => outfitSorting.TryGetValue(x.Id, out var sort) ? sort : int.MaxValue);
        }

        public async Task UpdateOutfitDisplayNames(List<Outfit> outfits)
        {
            foreach (var o in outfits)
            {
                o.SetDisplayName(await IoC.Localization.GetString(o.LocalNameFile, o.LocalNameId));
            }
        }

        public void UpdateModelDisplayNames(IList<Outfit> outfits, IList<Model> models)
        {
            var names = outfits.ToSoftDictionary(x => x.ModelId, x => x.DisplayName);

            foreach (var m in models)
            {
                if (names.TryGetValue(m.Id, out var outfitName))
                    m.DisplayName = $"Armor - {outfitName}";
                else
                    m.DisplayName = m.ToString();
            }
        }

        public override async Task LoadPatch(string path)
        {
            IoC.Notif.ShowStatus("Loading outfits...");
            
            var patchOutfits = await OutfitLogic.GenerateOutfits(path, false);
            
            if (!patchOutfits.Any())
                return;

            var loadedOutfits = patchOutfits.ToHashSet();

            await Initialize();

            var variantMapping = await CharacterLogic.GetVariantMapping(path, OutfitLogic);

            foreach (var outfit in Outfits)
            {
                if (loadedOutfits.TryGetValue(outfit, out var loadedOutfit))
                {
                    if (!variantMapping.TryGetValue(loadedOutfit.ModelId, out var varId))
                        varId = loadedOutfit.ModelId;

                    outfit.Modified = !outfit.ModelId.Equals(varId);
                    outfit.ModelId.AssignFromOther(varId);
                }
            }

            UpdateAllOutfitStub();
            RefreshLists();
        }

        public override async Task ApplyChanges(Patch patch)
        {
            if (true)
            {
                await CharacterLogic.CreatePatch(patch, Outfits, 
                    Models.Cast<CharacterModel>(), OutfitLogic);
            }
            else
            {
                await OutfitLogic.CreatePatch(patch, Outfits);
            }
        }
        
        private void lbOutfits_SelectedValueChanged(object sender, EventArgs e)
        {
            _updatingLists = true;
            
            ResetSelected.Enabled = lbOutfits.SelectedIndex >= 0;

            var modelIds = GetSelectedOutfits().Select(x => x.ModelId).ToHashSet();
            var checkState = modelIds.Count > 1 ? CheckState.Indeterminate : CheckState.Checked;

            for (int i = 0; i < clbModels.Items.Count; i++)
            {
                if (modelIds.Contains(Models[i].Id))
                    clbModels.SetItemCheckState(i, checkState);
                else
                    clbModels.SetItemCheckState(i, CheckState.Unchecked);
            }

            _updatingLists = false;
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

                foreach (var outfit in GetSelectedOutfits())
                    UpdateMapping(outfit, model);
                UpdateAllOutfitStub();

                lbOutfits.Invalidate();
            }

            _updatingLists = false;
        }

        private void UpdateMapping(Outfit outfit, Model model)
        {
            DefaultOutfits.TryGetValue(outfit, out var defaultOutfit);

            outfit.Modified = !defaultOutfit.ModelId.Equals(model.Id);
            outfit.ModelId.AssignFromOther(model.Id);
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

        private async Task Reload()
        {
            await Initialize();
            RefreshLists();

            IoC.Notif.ShowStatus("");
            IoC.Notif.HideProgress();
        }

        protected override async Task Reset_Click()
        {
            using var _ = new ControlLock(Reset);

            await Reload();
            IoC.Notif.ShowStatus("Reset complete");
        }

        protected override Task ResetSelected_Click()
        {
            if (lbOutfits.SelectedIndex < 0)
                return Task.CompletedTask;
            
            var selected = GetSelectedOutfits();

            foreach (var outfit in selected)
            {
                if (DefaultOutfits.TryGetValue(outfit, out var defaultOutfit))
                {
                    outfit.Modified = false;
                    outfit.ModelId.AssignFromOther(defaultOutfit.ModelId);
                }
            }

            lbOutfits.Invalidate();
            lbOutfits_SelectedValueChanged(lbOutfits, EventArgs.Empty);

            return Task.CompletedTask;
        }

        private void RefreshLists()
        {
            lbOutfits.ClearSelected();
            lbOutfits.Invalidate();
        }

        private List<Outfit> GetSelectedOutfits()
        {
            if (IoC.Settings.ApplyToAllOutfits)
                return Outfits.ToList();
            return lbOutfits.SelectedItems.Cast<Outfit>().ToList();
        }

        private void cbAllOutfits_CheckedChanged(object sender, EventArgs e)
        {
            if (_loading) return;
            IoC.Settings.ApplyToAllOutfits = cbAllOutfits.Checked;

            PopulateOutfitsList();
            UpdateAllOutfitsSelection();
        }

        private void PopulateOutfitsList()
        {
            if (IoC.Settings.ApplyToAllOutfits)
            {
                lbOutfits.Items.Clear();
                lbOutfits.Items.Add(AllOutfitStub);
                UpdateAllOutfitStub();
            }
            else
            {
                lbOutfits.Items.Clear();
                foreach (var item in Outfits)
                    lbOutfits.Items.Add(item);
            }
        }

        private void UpdateAllOutfitStub()
        {
            AllOutfitStub.Modified = Outfits.Any(x => x.Modified);
        }

        private void UpdateAllOutfitsSelection()
        {
            lbOutfits.SelectionMode = IoC.Settings.ApplyToAllOutfits ? 
                SelectionMode.None : SelectionMode.MultiExtended;

            lbOutfits_SelectedValueChanged(lbOutfits, EventArgs.Empty);
        }

        private bool _disableFilterEvents = false;
        private void ccbModelFilter_CheckBoxCheckedChanged(object sender, EventArgs e)
        {
            if (_disableFilterEvents || _loading)
                return;
            _disableFilterEvents = true;
            
            var checkedItem = (ObjectWrapper<OutfitModelFilter>)((CheckBoxComboBoxItem)sender).ComboBoxItem;
            if (checkedItem.Item == OutfitModelFilter.Characters)
                Filters.FindObjectWithItem(OutfitModelFilter.AllCharacters).Selected = false;
            if (checkedItem.Item == OutfitModelFilter.AllCharacters)
                Filters.FindObjectWithItem(OutfitModelFilter.Characters).Selected = false;

            _disableFilterEvents = false;
        }

        private async void ccbModelFilter_DropDownClosedCommand(object sender, EventArgs e) 
            => await Relay.To(sender, e, ccbModelFilter_DropDownClosed);
        private async Task ccbModelFilter_DropDownClosed(object sender, EventArgs e)
        {
            IoC.Settings.OutfitModelFilter = Filters.Where(x => x.Selected).Sum(x => (int)x.Item);
            await Reload();
        }
    }
}
