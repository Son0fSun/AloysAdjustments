﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AloysAdjustments.Utility;
using Decima;
using Decima.HZD;

namespace AloysAdjustments.Logic
{
    public class HzdCore
    {
        public string Source { get; set; }
        public string FilePath { get; private set; }
        public List<object> Components { get; private set; }

        public static HzdCore Load(string file, string source)
        {
            return new HzdCore()
            {
                FilePath = file,
                Source = source,
                Components = CoreBinary.Load(file)
            };
        }
        public static HzdCore Load(Stream stream, string source)
        {
            return new HzdCore()
            {
                Source = source,
                Components = CoreBinary.Load(stream)
            };
        }

        private HzdCore() { }

        public void Save(string filePath = null)
        {
            var savePath = filePath ?? FilePath;
            if (savePath == null)
                throw new HzdException("Cannot save pack file, save path null");

            CoreBinary.Save(savePath, Components);
        }

        public Dictionary<BaseGGUUID, T> GetTypes<T>(string typeName = null) where T : RTTIRefObject
        {
            typeName ??= typeof(T).Name;

            return Components.Where(x => x.GetType().Name == typeName)
                .ToDictionary(x => (BaseGGUUID)((T)x).ObjectUUID, x => (T)x);
        }
    }
}
