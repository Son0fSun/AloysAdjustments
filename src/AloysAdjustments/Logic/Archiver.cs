﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AloysAdjustments.Utility;
using Decima;

namespace AloysAdjustments.Logic
{
    public class Archiver
    {
        private const string PatchPrefix = "Patch";
        private const string PackExt = ".bin";

        private readonly ConcurrentDictionary<PackList, Dictionary<ulong, string>> _packCache;

        public HashSet<string> IgnoreList { get; }

        public Archiver(IEnumerable<string> ignoreList)
        {
            IgnoreList = ignoreList.ToHashSet(StringComparer.OrdinalIgnoreCase);
            _packCache = new ConcurrentDictionary<PackList, Dictionary<ulong, string>>();
        }

        public bool CheckArchiverLib()
        {
            return File.Exists(IoC.Config.ArchiverLib);
        }

        public void ValidatePackager()
        {
            if (!CheckArchiverLib())
                throw new HzdException($"Packager support library not found: {IoC.Config.ArchiverLib}");
        }

        public void ClearCache()
        {
            _packCache.Clear();
        }

        public async Task ExtractFile(string dir, string file, string output)
        {
            ValidatePackager();

            await Async.Run(() =>
            {
                using var fs = File.OpenWrite(output);

                if (!TryExtractFile(dir, fs, file))
                    throw new HzdException($"Unable to extract file, file not found: {file}");
            });
        }
        
        public async Task<HzdCore> LoadFileAsync(string dir, string file, bool throwError = true)
        {
            return await Async.Run(() => LoadFile(dir, file, throwError));
        }
        public HzdCore LoadFile(string dir, string file, bool throwError = true)
        {
            ValidatePackager();

            using var ms = new MemoryStream();
            if (!TryExtractFile(dir, ms, file))
            {
                if (throwError)
                    throw new HzdException($"Unable to extract file, file not found: {file}");
                return null;
            }

            ms.Position = 0;
            return HzdCore.Load(ms, file);
        }

        private bool TryExtractFile(string path, Stream stream, string file)
        {
            PackList packs;
            bool isDir;

            if (Directory.Exists(path))
            {
                packs = GetPackFiles(path, PackExt);
                isDir = true;
            }
            else if (File.Exists(path))
            {
                packs = new PackList(new[] { path });
                isDir = false;
            }
            else
                throw new HzdException($"Unable to extract file, source path not found: {path}");

            var fileMap = BuildFileMap(packs, isDir);

            file = HzdCore.EnsureExt(file);
            var hash = Packfile.GetHashForPath(file);
            if (!fileMap.TryGetValue(hash, out var packFile))
                return false;

            using (var pack = new PackfileReader(packFile))
                pack.ExtractFile(hash, stream);

            return true;
        }

        private PackList GetPackFiles(string dir, string ext)
        {
            var files = Directory.GetFiles(dir, $"*{ext}", SearchOption.AllDirectories)
                .OrderBy(x => x).ToList();
            files.RemoveAll(x => IgnoreList.Contains(Path.GetFileName(x)));

            //move patch files to end, same as game load order
            int moved = 0;
            for (int i = 0; i < files.Count - moved; i++)
            {
                if (Path.GetFileName(files[i]).StartsWith(PatchPrefix))
                {
                    var file = files[i];
                    files.RemoveAt(i);
                    files.Add(file);

                    i--;
                    moved++;
                }
            }

            return new PackList(files);
        }

        private Dictionary<ulong, string> BuildFileMap(PackList packFiles, bool useCache)
        {
            if (useCache && _packCache.TryGetValue(packFiles, out var files))
                return files;

            files = new Dictionary<ulong, string>();

            foreach (var packFile in packFiles.Packs)
            {
                using var pack = new PackfileReader(packFile);
                for (int i = 0; i < pack.FileEntries.Count; i++)
                {
                    var hash = pack.FileEntries[i].PathHash;
                    files[hash] = packFile;
                }
            }

            if (useCache)
                _packCache.TryAdd(packFiles, files);
            return files;
        }

        public async Task PackFiles(string dir, string output)
        {
            ValidatePackager();

            if (!Directory.Exists(dir))
                throw new HzdException($"Unable to create pack, directory not found: {dir}");

            dir = Path.GetFullPath(dir);
            output = Path.GetFullPath(output);

            await Async.Run(() =>
            {
                var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
                var fileNames = files.Select(x => x.Substring(dir.Length + 1).Replace("\\", "/")).ToArray();

                using var pack = new PackfileWriterFast(output, false, true);
                pack.BuildFromFileList(dir, fileNames);
            });
        }
        
        public async Task GetLibrary()
        {
            var libPath = Path.Combine(IoC.Settings.GamePath, IoC.Config.ArchiverLib);
            if (!File.Exists(libPath))
                throw new HzdException($"Unable to find archiver support library in: {IoC.Settings.GamePath}");
            
            await Async.Run(() => File.Copy(libPath, IoC.Config.ArchiverLib, true));
        }
    }
}
