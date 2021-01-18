﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AloysAdjustments.Configuration;
using AloysAdjustments.Data;
using AloysAdjustments.Logic;
using AloysAdjustments.Utility;
using Decima;
using Decima.HZD;
using Model = AloysAdjustments.Data.Model;

namespace AloysAdjustments.Modules.Outfits
{
    public class OutfitsLogic
    {
        public async Task<List<Outfit>> GenerateOutfits()
        {
            //extract game files
            var outfits = await GenerateOutfits(Configs.GamePackDir, true);
            return outfits;
        }

        public async Task<List<Outfit>> GenerateOutfits(string path, bool checkMissing)
        {
            var outfits = new List<Outfit>();

            var pcCore = await IoC.Archiver.LoadFileAsync(path, 
                IoC.Get<OutfitConfig>().PlayerComponentsFile, checkMissing);

            var models = pcCore != null ? GetPlayerModels(pcCore) : new List<StreamingRef<HumanoidBodyVariant>>();
            var variantFiles = models.ToSoftDictionary(x => x.GUID, x => x.ExternalFile?.ToString());

            var files = IoC.Get<OutfitConfig>().OutfitFiles;
            var cores = await Task.WhenAll(files.Select(
                async f => await IoC.Archiver.LoadFileAsync(path, f, checkMissing)));

            foreach (var core in cores.Where(x => x != null))
            {
                await foreach (var item in GetOutfits(core, variantFiles))
                {
                    item.SourceFile = core.Source;
                    outfits.Add(item);
                }
            }

            return outfits;
        }

        private async IAsyncEnumerable<Outfit> GetOutfits(HzdCore core, Dictionary<BaseGGUUID, string> variantFiles)
        {
            var items = core.GetTypes<InventoryEntityResource>();
            var itemComponents = core.GetTypesById<InventoryItemComponentResource>();
            var componentResources = core.GetTypesById<NodeGraphComponentResource>();
            var overrides = core.GetTypesById<OverrideGraphProgramResource>();
            var mappings = core.GetTypesById<NodeGraphHumanoidBodyVariantUUIDRefVariableOverride>();
            
            foreach (var item in items)
            {
                var outfit = new Outfit()
                {
                    Name = item.Name.ToString().Replace("InventoryEntityResource", "")
                };

                foreach (var component in item.EntityComponentResources)
                {
                    if (itemComponents.TryGetValue(component.GUID, out var invItem))
                    {
                        outfit.LocalName = await IoC.Localization.GetString(
                            invItem.LocalizedItemName.ExternalFile?.ToString(),
                            invItem.LocalizedItemName.GUID);
                    }

                    if (componentResources.TryGetValue(component.GUID, out var compRes))
                    {
                        var overrideRef = compRes.OverrideGraphProgramResource;
                        if (overrideRef?.GUID == null || !overrides.TryGetValue(overrideRef.GUID, out var rOverride))
                            continue;
                        
                        foreach (var mapRef in rOverride.VariableOverrides)
                        {
                            if (mappings.TryGetValue(mapRef.GUID, out var mapItem))
                            {
                                outfit.ModelId = mapItem.Object.GUID;
                                outfit.RefId = mapItem.ObjectUUID;

                                if (variantFiles.TryGetValue(outfit.ModelId, out var modelFile))
                                    outfit.ModelFile = modelFile;

                                break;
                            }
                        }
                    }
                }

                yield return outfit;
            }
        }

        public async Task<List<Model>> GenerateModelList()
        {
            return await GenerateModelList(Configs.GamePackDir);
        }
        public async Task<List<Model>> GenerateModelList(string path)
        {
            var models = new List<Model>();

            //player models
            var playerComponents = await IoC.Archiver.LoadFileAsync(
                path, IoC.Get<OutfitConfig>().PlayerComponentsFile);
            var playerModels = GetPlayerModels(playerComponents);

            models.AddRange(playerModels.Select(x => new Model
            {
                Id = x.GUID,
                Name = GetModelName(x),
                Source = x.ExternalFile.ToString()
            }));

            return models;
        }
        private string GetModelName(StreamingRef<HumanoidBodyVariant> model)
        {
            var source = model.ExternalFile.ToString();

            var key = "playercostume_";
            var idx = source.LastIndexOf(key);
            return idx < 0 ? source : source.Substring(idx + key.Length);
        }

        public static List<StreamingRef<HumanoidBodyVariant>> GetPlayerModels(HzdCore core)
        {
            var resource = core.GetTypes<BodyVariantComponentResource>().FirstOrDefault();
            if (resource == null)
                throw new HzdException("Unable to find PlayerBodyVariants");

            return resource.Variants;
        }
        
        public async Task CreatePatch(Patch patch, ReadOnlyCollection<Outfit> outfits,
            Dictionary<BaseGGUUID, BaseGGUUID> variantMapping)
        {
            var modifiedOutfits = outfits.Where(x => x.Modified).ToDictionary(x => x.RefId, x => x);
            var maps = modifiedOutfits.Values.Select(x => x.SourceFile).Distinct();

            foreach (var map in maps)
            {
                //extract original outfit files to temp
                var core = await patch.AddFile(map);
                
                //update references from based on new maps
                foreach (var reference in core.GetTypes<NodeGraphHumanoidBodyVariantUUIDRefVariableOverride>())
                {
                    if (modifiedOutfits.TryGetValue(reference.ObjectUUID, out var changed))
                    {
                        if (!variantMapping.TryGetValue(changed.ModelId, out var variantId))
                            variantId = changed.ModelId;
                        reference.Object.GUID.AssignFromOther(variantId);
                    }
                }

                await core.Save();
            }
        }
    }
}
