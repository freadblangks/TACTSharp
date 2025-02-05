﻿namespace TACTSharp
{
    public class BuildInstance
    {
        public Config BuildConfig { get; private set; }
        public Config CDNConfig { get; private set; }

        public EncodingInstance? Encoding { get; private set; }
        public RootInstance? Root { get; private set; }
        public InstallInstance? Install { get; private set; }
        public IndexInstance? GroupIndex { get; private set; }
        public IndexInstance? FileIndex { get; private set; }

        public BuildInstance(string buildConfig, string cdnConfig)
        {
            // Always load configs so we have basic information available, loading the full build is optional.
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            if (File.Exists(buildConfig))
                BuildConfig = new Config(buildConfig, true);
            else if (buildConfig.Length == 32 && buildConfig.All(c => "0123456789abcdef".Contains(c)))
                BuildConfig = new Config(buildConfig, false);

            if (File.Exists(cdnConfig))
                CDNConfig = new Config(cdnConfig, true);
            else if (cdnConfig.Length == 32 && cdnConfig.All(c => "0123456789abcdef".Contains(c)))
                CDNConfig = new Config(cdnConfig, false);

            if (BuildConfig == null || CDNConfig == null)
                throw new Exception("Failed to load configs");
            timer.Stop();
            Console.WriteLine("Configs loaded in " + Math.Ceiling(timer.Elapsed.TotalMilliseconds) + "ms");
        }

        public void Load()
        {
            var timer = new System.Diagnostics.Stopwatch();

            timer.Start();
            if (!CDNConfig.Values.TryGetValue("archive-group", out var groupArchiveIndex))
            {
                Console.WriteLine("No group index found in CDN config, generating fresh group index...");
                var groupIndexHash = TACTSharp.GroupIndex.Generate("", CDNConfig.Values["archives"]);
                var groupIndexPath = Path.Combine(Settings.CacheDir, "wow", "data", groupIndexHash + ".index");
                GroupIndex = new IndexInstance(groupIndexPath);
            }
            else
            {
                if (!string.IsNullOrEmpty(Settings.BaseDir) && File.Exists(Path.Combine(Settings.BaseDir, "Data", "indices", groupArchiveIndex[0] + ".index")))
                {
                    GroupIndex = new IndexInstance(Path.Combine(Settings.BaseDir, "Data", "indices", groupArchiveIndex[0] + ".index"));
                }
                else
                {
                    var groupIndexPath = Path.Combine(Settings.CacheDir, "wow", "data", groupArchiveIndex[0] + ".index");
                    if (!File.Exists(groupIndexPath))
                        TACTSharp.GroupIndex.Generate(groupArchiveIndex[0], CDNConfig.Values["archives"]);
                    GroupIndex = new IndexInstance(groupIndexPath);
                }
            }
            timer.Stop();
            Console.WriteLine("Group index loaded in " + Math.Ceiling(timer.Elapsed.TotalMilliseconds) + "ms");

            timer.Restart();
            if (!CDNConfig.Values.TryGetValue("file-index", out var fileIndex))
                throw new Exception("No file index found in CDN config");

            if (!string.IsNullOrEmpty(Settings.BaseDir) && File.Exists(Path.Combine(Settings.BaseDir, "Data", "indices", fileIndex[0] + ".index")))
            {
                FileIndex = new IndexInstance(Path.Combine(Settings.BaseDir, "Data", "indices", fileIndex[0] + ".index"));
            }
            else
            {
                var fileIndexPath = CDN.GetFilePath("wow", "data", fileIndex[0] + ".index");
                FileIndex = new IndexInstance(fileIndexPath);
            }

            timer.Stop();
            Console.WriteLine("File index loaded in " + Math.Ceiling(timer.Elapsed.TotalMilliseconds) + "ms");

            var encodingSize = ulong.Parse(BuildConfig.Values["encoding-size"][0]);
            timer.Restart();
            Encoding = new EncodingInstance(CDN.GetDecodedFilePath("wow", "data", BuildConfig.Values["encoding"][1], ulong.Parse(BuildConfig.Values["encoding-size"][1]), encodingSize), (int)encodingSize);
            timer.Stop();
            Console.WriteLine("Encoding loaded in " + Math.Ceiling(timer.Elapsed.TotalMilliseconds) + "ms");

            timer.Restart();
            if (!BuildConfig.Values.TryGetValue("root", out var rootKey))
                throw new Exception("No root key found in build config");

            var rootEncodingKeys = Encoding.FindContentKey(Convert.FromHexString(rootKey[0]));
            if (!rootEncodingKeys)
                throw new Exception("Root key not found in encoding");

            Root = new RootInstance(CDN.GetDecodedFilePath("wow", "data", Convert.ToHexStringLower(rootEncodingKeys[0]), 0, rootEncodingKeys.DecodedFileSize));
            timer.Stop();
            Console.WriteLine("Root loaded in " + Math.Ceiling(timer.Elapsed.TotalMilliseconds) + "ms");

            timer.Restart();
            if (!BuildConfig.Values.TryGetValue("install", out var installKey))
                throw new Exception("No root key found in build config");

            var installEncodingKeys = Encoding.FindContentKey(Convert.FromHexString(installKey[0]));
            if (!installEncodingKeys)
                throw new Exception("Install key not found in encoding");

            Install = new InstallInstance(CDN.GetDecodedFilePath("wow", "data", Convert.ToHexStringLower(installEncodingKeys[0]), 0, installEncodingKeys.DecodedFileSize));
            timer.Stop();
            Console.WriteLine("Install loaded in " + Math.Ceiling(timer.Elapsed.TotalMilliseconds) + "ms");
        }

        public byte[] OpenFileByFDID(uint fileDataID)
        {
            if (Root == null)
                throw new Exception("Root not loaded");

            var fileData = Root.GetEntryByFDID(fileDataID) ?? throw new Exception("File not found in root");

            return OpenFileByCKey(fileData.md5);
        }

        public byte[] OpenFileByCKey(string cKey) => OpenFileByCKey(Convert.FromHexString(cKey));

        public byte[] OpenFileByCKey(ReadOnlySpan<byte> cKey)
        {
            if (Encoding == null)
                throw new Exception("Encoding not loaded");

            var encodingResult = Encoding.FindContentKey(cKey);
            if (encodingResult.Length == 0)
                throw new Exception("File not found in encoding");

            return OpenFileByEKey(encodingResult[0], encodingResult.DecodedFileSize);
        }

        public byte[] OpenFileByEKey(string eKey, ulong decodedSize = 0) => OpenFileByEKey(Convert.FromHexString(eKey), decodedSize);

        public byte[] OpenFileByEKey(ReadOnlySpan<byte> eKey, ulong decodedSize = 0)
        {
            if (GroupIndex == null || FileIndex == null)
                throw new Exception("Indexes not loaded");

            var (offset, size, archiveIndex) = GroupIndex.GetIndexInfo(eKey);
            byte[] fileBytes;

            if (offset == -1)
            {
                var fileIndexEntry = FileIndex.GetIndexInfo(eKey);
                if (fileIndexEntry.size == -1)
                {
                    Console.WriteLine("Warning: EKey " + Convert.ToHexStringLower(eKey) + " not found in group or file index and might not be available on CDN.");
                    fileBytes = CDN.GetFile("wow", "data", Convert.ToHexStringLower(eKey), 0, decodedSize, true);
                }
                else
                {
                    fileBytes = CDN.GetFile("wow", "data", Convert.ToHexStringLower(eKey), (ulong)fileIndexEntry.size, decodedSize, true);
                }
            }
            else
            {
                fileBytes = CDN.GetFileFromArchive(Convert.ToHexStringLower(eKey), "wow", CDNConfig.Values["archives"][archiveIndex], offset, size, decodedSize, true);
            }

            return fileBytes;
        }
    }
}
