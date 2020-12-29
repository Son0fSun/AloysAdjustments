﻿using System.Collections.Generic;
using System.Linq;
using Decima;

namespace HZDUtility.Models
{
    public class OutfitMap
    {
        public string File { get; set; }
        public List<(BaseGGUUID ModelId, BaseGGUUID RefId)> Refs { get; set; }

        public OutfitMap()
        {
            Refs = new List<(BaseGGUUID ModelId, BaseGGUUID RefId)>();
        }

        public OutfitMap Clone()
        {
            var map = new OutfitMap()
            {
                File = File
            };

            map.Refs = Refs
                .Select(x => (
                    BaseGGUUID.FromOther(x.ModelId), 
                    BaseGGUUID.FromOther(x.RefId)))
                .ToList();

            return map;
        }
    }
}