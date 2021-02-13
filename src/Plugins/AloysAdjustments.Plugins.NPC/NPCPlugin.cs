﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using AloysAdjustments.Configuration;
using AloysAdjustments.Logic;
using AloysAdjustments.Logic.Patching;
using AloysAdjustments.Plugins.Outfits;
using AloysAdjustments.Plugins.Outfits.Data;
using AloysAdjustments.UI;
using AloysAdjustments.Utility;
using EnumsNET;

namespace AloysAdjustments.Plugins.NPC
{
    public class NPCPlugin : InteractivePlugin, INotifyPropertyChanged
    {
        public override string PluginName => "NPC Models";

        private CharacterGenerator CharacterGen { get; }
        private OutfitPatcher Patcher { get; }

        public ValuePair<Model> AllNpcStub { get; set; }
        public ObservableCollection<ValuePair<Model>> Npcs { get; set; }
        public ICollectionView NpcsView { get; set; }
        public ObservableCollection<Model> Models { get; set; }
        public ICollectionView ModelsView { get; set; }

        public ReadOnlyCollection<ModelFilter> Filters { get; set; }

        public IList SelectedNpcModels { get; set; }
        public Model SelectedModelMapping { get; set; }

        public ModelFilter FilterValue { get; set; }

        public bool ApplyToAll { get; set; }

        public NPCPlugin()
        {
            IoC.Bind(Configs.LoadModuleConfig<OutfitConfig>(PluginName));

            Reset = new ControlRelay(OnResetAll);
            ResetSelected = new ControlRelay(OnResetSelected);

            CharacterGen = new CharacterGenerator();
            Patcher = new OutfitPatcher();

            Filters = Enums.GetValues<ModelFilter>().ToList().AsReadOnly();

            LoadSettings();

            PluginControl = new NPCPluginView();
            PluginControl.DataContext = this;

            Models = new ObservableCollection<Model>();
            ModelsView = CollectionViewSource.GetDefaultView(Models);
            Npcs = new ObservableCollection<ValuePair<Model>>();
            NpcsView = CollectionViewSource.GetDefaultView(Npcs);

            ModelsView.Filter = Filter;
            NpcsView.Filter = NpcFilter;

            var allNpc = new Model() {DisplayName = "All Outfits"};
            AllNpcStub = new ValuePair<Model>(allNpc, allNpc);
        }

        private void LoadSettings()
        {
            ApplyToAll = IoC.Settings.ApplyToAllOutfits;
            FilterValue = (ModelFilter)IoC.Settings.OutfitModelFilter;
        }

        public override Task LoadPatch(string path)
        {
            return Task.CompletedTask;
        }

        public override void ApplyChanges(Patch patch)
        {

        }

        public override async Task Initialize()
        {
            ResetSelected.Enabled = false;
            IoC.Notif.ShowUnknownProgress();

            Models.Clear();
            await UpdateModelList(x => Models.Add(x));

            Npcs.Clear();
            await UpdateModelList(x => Npcs.Add(new ValuePair<Model>(x, x)));
            Npcs.Add(AllNpcStub);
        }

        private async Task UpdateModelList(Action<Model> add)
        {
            var models = await LoadCharacterModelList(true);
            foreach (var model in models)
            {
                model.DisplayName = model.ToString();
                add(model);
            }

            models = await LoadCharacterModelList(false);
            foreach (var model in models)
            {
                model.DisplayName = model.ToString();
                add(model);
            }
        }

        private async Task<List<CharacterModel>> LoadCharacterModelList(bool unique)
        {
            return await Async.Run(() =>
                {
                    IoC.Notif.ShowStatus("Loading characters list...");
                    return CharacterGen.GetCharacterModels(unique);
                });
        }

        public bool Filter(object obj)
        {
            var model = (CharacterModel)obj;
            if ((int)FilterValue == OutfitModelFilter.Characters.Value)
                return model.UniqueCharacter;
            return true;
        }
        public bool NpcFilter(object obj)
        {
            return (obj == AllNpcStub) == ApplyToAll;
        }

        private void OnNpcSelectionChanged()
        {
            ResetSelected.Enabled = SelectedNpcModels?.Count > 0;

            var selectedModelIds = GetSelectedOutfits().Select(x => x.Value.Id).ToHashSet();

            foreach (var model in Models)
            {
                if (selectedModelIds.Contains(model.Id))
                    model.Checked = selectedModelIds.Count > 1 ? null : (bool?)true;
                else
                    model.Checked = false;
            }
        }
        private List<ValuePair<Model>> GetSelectedOutfits()
        {
            if (IoC.Settings.ApplyToAllOutfits)
                return Npcs.ToList();
            return SelectedNpcModels?.Cast<ValuePair<Model>>().ToList() ?? new List<ValuePair<Model>>();
        }

        private void OnModelsSelectionChanged()
        {
            if (SelectedModelMapping == null)
                return;

            SelectedModelMapping.Checked = true;
            foreach (var model in Models)
            {
                if (!ReferenceEquals(SelectedModelMapping, model))
                    model.Checked = false;
            }

            foreach (var outfit in GetSelectedOutfits())
                UpdateMapping(outfit, SelectedModelMapping);
        }

        private void UpdateMapping(ValuePair<Model> npc, Model model)
        {
            npc.Value = model;
        }
        
        private void OnApplyToAll()
        {
            IoC.Settings.ApplyToAllOutfits = ApplyToAll;
            NpcsView.Refresh();
        }

        private Task OnResetSelected()
        {
            foreach (var npc in GetSelectedOutfits())
                npc.Value = npc.Default;

            return Task.CompletedTask;
        }

        private Task OnResetAll()
        {
            foreach (var npc in Npcs)
                npc.Value = npc.Default;

            return Task.CompletedTask;
        }

        public void OnPropertyChanged(string propertyName, object before, object after)
        {
            switch (propertyName)
            {
                case nameof(SelectedNpcModels):
                    OnNpcSelectionChanged();
                    break;
                case nameof(SelectedModelMapping):
                    OnModelsSelectionChanged();
                    break;
                case nameof(ApplyToAll):
                    OnApplyToAll();
                    OnNpcSelectionChanged();
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
