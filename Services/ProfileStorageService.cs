using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Json;
using IFCAlignmentManager.Core;

namespace IFCAlignmentManager.Services
{
    public class ProfileStorageService
    {
        private readonly string _profileFolder;

        public ProfileStorageService(string profileFolder = null)
        {
            if (string.IsNullOrEmpty(profileFolder))
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                _profileFolder = Path.Combine(appData, "IFCAlignmentManager", "Profiles");
            }
            else
            {
                _profileFolder = profileFolder;
            }

            if (!Directory.Exists(_profileFolder))
                Directory.CreateDirectory(_profileFolder);
        }

        public string ProfileFolder => _profileFolder;

        // Save profile to [ProfileId].json (Atomic write pattern to prevent corrupt JSON on crash)
        public bool SaveProfile(ConsultantAlignmentProfile profile, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                if (profile == null)
                {
                    errorMessage = "Profile cannot be null.";
                    return false;
                }

                if (string.IsNullOrEmpty(profile.ProfileId))
                    profile.ProfileId = Guid.NewGuid().ToString();

                profile.LastUsedDate = DateTime.Now.ToString("o");
                if (string.IsNullOrEmpty(profile.CreatedDate))
                    profile.CreatedDate = profile.LastUsedDate;

                string finalPath = Path.Combine(_profileFolder, $"{profile.ProfileId}.json");
                string tempPath  = finalPath + ".tmp";

                var serializer = new DataContractJsonSerializer(typeof(ConsultantAlignmentProfile));
                using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    serializer.WriteObject(fs, profile);
                }
                // Atomic rename — on Windows, File.Move with overwrite is atomic at NTFS level
                if (File.Exists(finalPath)) File.Delete(finalPath);
                File.Move(tempPath, finalPath);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        // Load all profiles from folder, return list sorted by LastUsedDate descending
        public List<ConsultantAlignmentProfile> LoadAllProfiles()
        {
            var list = new List<ConsultantAlignmentProfile>();
            if (!Directory.Exists(_profileFolder))
                return list;

            var files = Directory.GetFiles(_profileFolder, "*.json");
            foreach (var file in files)
            {
                try
                {
                    var serializer = new DataContractJsonSerializer(typeof(ConsultantAlignmentProfile));
                    using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var profile = (ConsultantAlignmentProfile)serializer.ReadObject(fs);
                        if (profile != null) list.Add(profile);
                    }
                }
                catch
                {
                    // ignore corrupt files
                }
            }

            list.Sort((a, b) => string.Compare(b.LastUsedDate ?? "", a.LastUsedDate ?? "", StringComparison.Ordinal));
            return list;
        }

        // Load single profile by GUID
        public ConsultantAlignmentProfile LoadProfile(string profileId)
        {
            var filePath = Path.Combine(_profileFolder, $"{profileId}.json");
            if (!File.Exists(filePath))
                return null;

            try
            {
                var serializer = new DataContractJsonSerializer(typeof(ConsultantAlignmentProfile));
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return (ConsultantAlignmentProfile)serializer.ReadObject(fs);
                }
            }
            catch
            {
                return null;
            }
        }

        // Delete profile file
        public bool DeleteProfile(string profileId, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                var filePath = Path.Combine(_profileFolder, $"{profileId}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                errorMessage = "Profile not found.";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        // Validate that a current snapshot is compatible with a stored profile's baseline.
        // Compatible = all baseline coordinate values within profile.ValidationTolerance_Feet
        public ProfileValidationResult ValidateProfileAgainstSnapshot(ConsultantAlignmentProfile profile, CoordinateSnapshot currentSnapshot)
        {
            var result = new ProfileValidationResult
            {
                IsCompatible = true,
                Mismatches = new List<string>(),
                MaxDeviation_Feet = 0
            };

            if (profile == null || profile.BaselineSnapshot == null || currentSnapshot == null)
            {
                result.IsCompatible = false;
                result.Mismatches.Add("Missing snapshot or profile data.");
                return result;
            }

            var tol = profile.ValidationTolerance_Feet > 0 ? profile.ValidationTolerance_Feet : 0.01;

            void Check(double baseline, double current, string name)
            {
                double diff = Math.Abs(current - baseline);
                if (diff > tol)
                {
                    result.IsCompatible = false;
                    result.Mismatches.Add($"{name}: difference of {diff:F4} ft exceeds tolerance {tol:F4} ft");
                    if (diff > result.MaxDeviation_Feet)
                        result.MaxDeviation_Feet = diff;
                }
            }

            Check(profile.BaselineSnapshot.PBP_X, currentSnapshot.PBP_X, "PBP_X");
            Check(profile.BaselineSnapshot.PBP_Y, currentSnapshot.PBP_Y, "PBP_Y");
            Check(profile.BaselineSnapshot.PBP_Z, currentSnapshot.PBP_Z, "PBP_Z");
            Check(profile.BaselineSnapshot.SP_X, currentSnapshot.SP_X, "SP_X");
            Check(profile.BaselineSnapshot.SP_Y, currentSnapshot.SP_Y, "SP_Y");
            Check(profile.BaselineSnapshot.SP_Z, currentSnapshot.SP_Z, "SP_Z");
            Check(profile.BaselineSnapshot.SharedOrigin_EastWest, currentSnapshot.SharedOrigin_EastWest, "SharedOrigin_EastWest");
            Check(profile.BaselineSnapshot.SharedOrigin_NorthSouth, currentSnapshot.SharedOrigin_NorthSouth, "SharedOrigin_NorthSouth");
            Check(profile.BaselineSnapshot.SharedOrigin_Elevation, currentSnapshot.SharedOrigin_Elevation, "SharedOrigin_Elevation");
            Check(profile.BaselineSnapshot.TrueNorthAngle_Radians, currentSnapshot.TrueNorthAngle_Radians, "TrueNorthAngle_Radians");

            return result;
        }

        /// <summary>
        /// Exports all profiles in the profile folder to a single .zip archive at <paramref name="zipPath"/>.
        /// Returns the number of profiles exported, or -1 on error.
        /// </summary>
        public int ExportProfilesAsZip(string zipPath, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                var files = Directory.GetFiles(_profileFolder, "*.json");
                if (files.Length == 0)
                {
                    errorMessage = "No profiles found to export.";
                    return 0;
                }

                using (var zip = System.IO.Compression.ZipFile.Open(zipPath, System.IO.Compression.ZipArchiveMode.Create))
                {
                    foreach (var f in files)
                        zip.CreateEntryFromFile(f, Path.GetFileName(f));
                }
                return files.Length;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return -1;
            }
        }

        /// <summary>
        /// Imports profiles from a .zip archive into the profile folder.
        /// Skips files that are not valid JSON profiles. Does not overwrite
        /// existing profiles with the same ProfileId unless <paramref name="overwrite"/> is true.
        /// Returns a tuple of (imported count, skipped count).
        /// </summary>
        public (int Imported, int Skipped) ImportProfilesFromZip(string zipPath, bool overwrite, out string errorMessage)
        {
            errorMessage = null;
            int imported = 0, skipped = 0;

            try
            {
                using (var zip = System.IO.Compression.ZipFile.OpenRead(zipPath))
                {
                    foreach (var entry in zip.Entries)
                    {
                        if (!entry.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;

                        string destPath = Path.Combine(_profileFolder, entry.Name);
                        if (File.Exists(destPath) && !overwrite) { skipped++; continue; }

                        // Validate before writing
                        try
                        {
                            using (var stream = entry.Open())
                            {
                                var serializer = new DataContractJsonSerializer(typeof(ConsultantAlignmentProfile));
                                var profile = (ConsultantAlignmentProfile)serializer.ReadObject(stream);
                                if (profile == null || string.IsNullOrEmpty(profile.ProfileId))
                                { skipped++; continue; }
                            }
                        }
                        catch { skipped++; continue; }

                        entry.ExtractToFile(destPath, overwrite: true);
                        imported++;
                    }
                }
                return (imported, skipped);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return (imported, skipped);
            }
        }
    }
}
