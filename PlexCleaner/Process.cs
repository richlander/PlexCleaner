﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using InsaneGenius.Utilities;

namespace PlexCleaner
{
    internal class Process
    {
        public Process()
        {
            // TODO : Add cleanup for extra empty entry when string is empty
            // extensionlist = extensionlist.Where(s => !String.IsNullOrWhiteSpace(s)).Distinct().ToList();

            // Sidecar extension, use the enum names
            sidecarExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                $".{Info.MediaInfo.ParserType.MediaInfo.ToString()}",
                $".{Info.MediaInfo.ParserType.MkvMerge.ToString()}",
                $".{Info.MediaInfo.ParserType.FfProbe.ToString()}"
            };

            // Wanted extensions, always keep .mkv and sidecar files
            List<string> stringlist = EncodeOptions.Default.KeepExtensions.Split(',').ToList();
            keepExtensions = new HashSet<string>(stringlist, StringComparer.OrdinalIgnoreCase)
            {
                ".mkv"
            };
            foreach (string extension in sidecarExtensions)
                keepExtensions.Add(extension);

            // Containers types that can be remuxed to MKV
            stringlist = EncodeOptions.Default.ReMuxExtensions.Split(',').ToList();
            remuxExtensions = new HashSet<string>(stringlist, StringComparer.OrdinalIgnoreCase);

            // Languages are in short form using ISO 639-2 notation
            // https://www.loc.gov/standards/iso639-2/php/code_list.php
            // zxx = no linguistic content, und = undetermined
            // Default language
            defaultLanguage = EncodeOptions.Default.DefaultLanguage;
            if (string.IsNullOrEmpty(defaultLanguage))
                defaultLanguage = "eng";

            // Languages to keep, always keep no linguistic content and the default language
            // The languages must be in ISO 639-2 form
            stringlist = EncodeOptions.Default.KeepLanguages.Split(',').ToList();
            keepLanguages = new HashSet<string>(stringlist, StringComparer.OrdinalIgnoreCase)
            {
                "zxx",
                defaultLanguage
            };

            // Re-encode any video track that match the list
            // We use ffmpeg to re-encode, so we use ffprobe formats
            // All other formats will be encoded to h264
            List<string> codeclist = EncodeOptions.Default.ReEncodeVideoCodec.Split(',').ToList();
            List<string> profilelist = EncodeOptions.Default.ReEncodeVideoProfile.Split(',').ToList();
            reencodeVideoCodecs = new List<Info.VideoInfo>();
            for (int i = 0; i < codeclist.Count; i++)
            {
                // We match against the format and profile
                // Match the logic in VideoInfo.CompareVideo
                Info.VideoInfo videoinfo = new Info.VideoInfo
                {
                    Codec = "*",
                    Format = codeclist.ElementAt(i),
                    Profile = profilelist.ElementAt(i)
                };
                reencodeVideoCodecs.Add(videoinfo);
            }

            // Re-encode any audio track that match the list
            // We use ffmpeg to re-encode, so we use ffprobe formats
            // All other formats will be encoded to the default codec, e.g. ac3
            audioEncodeCodec = EncodeOptions.Default.AudioEncodeCodec;
            if (string.IsNullOrEmpty(audioEncodeCodec))
                audioEncodeCodec = "ac3";
            stringlist = EncodeOptions.Default.ReEncodeAudioCodec.Split(',').ToList();
            reencodeAudioCodecs = new HashSet<string>(stringlist, StringComparer.OrdinalIgnoreCase);

            // Video encode constant quality factor, e.g. 20
            videoEncodeQuality = EncodeOptions.Default.VideoEncodeQuality;
            if (videoEncodeQuality == 0)
                videoEncodeQuality = 20;
        }

        public bool ProcessFiles(List<FileInfo> fileList)
        {
            ConsoleEx.WriteLine("");
            ConsoleEx.WriteLine("Processing files ...");
            ConsoleEx.WriteLine("");

            // Start the stopwatch
            Stopwatch timer = new Stopwatch();
            timer.Start();

            // Process all files
            int errorcount = 0;
            int modifiedcount = 0;
            foreach (FileInfo fileinfo in fileList)
            {
                // Process the file
                ConsoleEx.WriteLine($"Processing \"{fileinfo.FullName}\"");
                if (!ProcessFile(fileinfo, out bool modified))
                    errorcount++;
                else if (modified)
                    modifiedcount++;

                // Cancel handler
                if (Program.Default.Cancel.State)
                    return false;

                // Next file
            }

            // Stop the timer
            timer.Stop();
            ConsoleEx.WriteLine("");
            ConsoleEx.WriteLine($"Total files : {fileList.Count}");
            ConsoleEx.WriteLine($"Modified files : {modifiedcount}");
            ConsoleEx.WriteLine($"Error files : {errorcount}");
            ConsoleEx.WriteLine($"Processing time : {timer.Elapsed}");
            ConsoleEx.WriteLine("");

            return true;
        }

        public bool ProcessFolders(List<string> folderList)
        {
            // Create the file and directory list
            List<FileInfo> fileList;
            List<DirectoryInfo> directoryList;
            if (!FileEx.EnumerateDirectories(folderList, out fileList, out directoryList))
                return false;

            // Process the files
            return ProcessFiles(fileList);
        }

        public bool DeleteEmptyFolders(List<string> folderList)
        {
            if (!AppOptions.Default.DeleteEmptyFolders)
                return true;

            ConsoleEx.WriteLine("");
            ConsoleEx.WriteLine("Deleting empty folders ...");
            ConsoleEx.WriteLine("");

            // Delete all empty folders
            int deleted = 0;
            foreach (string folder in folderList)
            {
                ConsoleEx.WriteLine($"Deleting \"{folder}\"");
                FileEx.DeleteEmptyDirectories(folder, ref deleted);
            }

            ConsoleEx.WriteLine("");
            ConsoleEx.WriteLine($"Deleted folders : {deleted}");
            ConsoleEx.WriteLine("");

            return true;
        }

        public bool ReMuxFiles(List<FileInfo> fileList)
        {
            ConsoleEx.WriteLine("");
            ConsoleEx.WriteLine("ReMuxing files ...");
            ConsoleEx.WriteLine("");

            // Start the stopwatch
            Stopwatch timer = new Stopwatch();
            timer.Start();

            // Process all files
            int errorcount = 0;
            int modifiedcount = 0;
            foreach (FileInfo fileinfo in fileList)
            {
                // Cancel handler
                if (Program.Default.Cancel.State)
                    return false;

                // Handle only MKV files, and files in the remux extension list
                if (!Tools.IsMkvFile(fileinfo) &&
                    !remuxExtensions.Contains(fileinfo.Extension))
                    continue;

                // ReMux file
                ConsoleEx.WriteLine($"ReMuxing \"{fileinfo.FullName}\"");
                if (!ReMuxFile(fileinfo, out bool modified))
                    errorcount++;
                else if (modified)
                    modifiedcount++;

                // Next file
            }

            // Stop the timer
            timer.Stop();
            ConsoleEx.WriteLine("");
            ConsoleEx.WriteLine($"Total files : {fileList.Count}");
            ConsoleEx.WriteLine($"Modified files : {modifiedcount}");
            ConsoleEx.WriteLine($"Error files : {errorcount}");
            ConsoleEx.WriteLine($"Processing time : {timer.Elapsed}");
            ConsoleEx.WriteLine("");

            return true;
        }

        public bool ReEncodeFiles(List<FileInfo> fileList)
        {
            ConsoleEx.WriteLine("");
            ConsoleEx.WriteLine("ReEncoding files ...");
            ConsoleEx.WriteLine("");

            // Start the stopwatch
            Stopwatch timer = new Stopwatch();
            timer.Start();

            // Process all files
            int errorcount = 0;
            int modifiedcount = 0;
            foreach (FileInfo fileinfo in fileList)
            {
                // Cancel handler
                if (Program.Default.Cancel.State)
                    return false;

                // Handle only MKV files
                // ReMux before re-encode, so the track attribute logic works as expected
                if (!Tools.IsMkvFile(fileinfo))
                    continue;

                // Process the file
                ConsoleEx.WriteLine($"ReEncoding \"{fileinfo.FullName}\"");
                if (!ReEncodeFile(fileinfo, out bool modified))
                    errorcount++;
                else if (modified)
                    modifiedcount++;

                // Next file
            }

            // Stop the timer
            timer.Stop();
            ConsoleEx.WriteLine("");
            ConsoleEx.WriteLine($"Total files : {fileList.Count}");
            ConsoleEx.WriteLine($"Modified files : {modifiedcount}");
            ConsoleEx.WriteLine($"Error files : {errorcount}");
            ConsoleEx.WriteLine($"Processing time : {timer.Elapsed}");
            ConsoleEx.WriteLine("");

            return true;
        }

        public bool CreateTagMapFiles(List<FileInfo> fileList)
        {
            ConsoleEx.WriteLine("");
            ConsoleEx.WriteLine("Creating tag map for folders ...");
            ConsoleEx.WriteLine("");

            // Start the stopwatch
            Stopwatch timer = new Stopwatch();
            timer.Start();

            // We want to create a dictionary of ffprobe to mkvmerge and mediainfo tag strings
            // And how they map to each other for the same media file
            Info.TagMapDictionary fftags = new Info.TagMapDictionary();
            Info.TagMapDictionary mktags = new Info.TagMapDictionary();
            Info.TagMapDictionary mitags = new Info.TagMapDictionary();

            // Process all files
            int errorcount = 0;
            int modifiedcount = 0;
            foreach (FileInfo fileinfo in fileList)
            {
                // Cancel handler
                if (Program.Default.Cancel.State)
                    return false;

                // Handle only MKV files
                if (!Tools.IsMkvFile(fileinfo))
                    continue;

                // Write all the different sidecar file types
                Info.MediaInfo mi = null, mk = null, ff = null;
                foreach (Info.MediaInfo.ParserType parser in Enum.GetValues(typeof(Info.MediaInfo.ParserType)))
                    if (GetMediaInfoSidecar(fileinfo, parser, out bool modified, out Info.MediaInfo info))
                    {
                        switch (parser)
                        {
                            case Info.MediaInfo.ParserType.MediaInfo:
                                mi = info;
                                break;
                            case Info.MediaInfo.ParserType.MkvMerge:
                                mk = info;
                                break;
                            case Info.MediaInfo.ParserType.FfProbe:
                                ff = info;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        if (modified)
                            modifiedcount++;
                    }
                    else
                        errorcount++;

                // Compare to make sure we can map tracks between tools
                if (!Info.MatchTracks(mi, mk, ff))
                    continue;

                // Add all the tags
                fftags.Add(ff, mk, mi);
                mktags.Add(mk, ff, mi);
                mitags.Add(mi, ff, mk);

                // Next file
            }

            // Print the results
            ConsoleEx.WriteLine("");
            ConsoleEx.WriteLine("FFProbe:");
            fftags.WriteLine();
            ConsoleEx.WriteLine("");
            ConsoleEx.WriteLine("MKVMerge:");
            mktags.WriteLine();
            ConsoleEx.WriteLine("");
            ConsoleEx.WriteLine("MediaInfo:");
            mitags.WriteLine();
            ConsoleEx.WriteLine("");

            // Stop the timer
            timer.Stop();
            ConsoleEx.WriteLine("");
            ConsoleEx.WriteLine($"Total files : {fileList.Count}");
            ConsoleEx.WriteLine($"Modified files : {modifiedcount}");
            ConsoleEx.WriteLine($"Error files : {errorcount}");
            ConsoleEx.WriteLine($"Processing time : {timer.Elapsed}");
            ConsoleEx.WriteLine("");

            return true;
        }

        public bool WriteSidecarFiles(List<FileInfo> fileList)
        {
            ConsoleEx.WriteLine("");
            ConsoleEx.WriteLine("Writing sidecar files ...");
            ConsoleEx.WriteLine("");

            // Start the stopwatch
            Stopwatch timer = new Stopwatch();
            timer.Start();

            // Process all files
            int errorcount = 0;
            int modifiedcount = 0;
            foreach (FileInfo fileinfo in fileList)
            {
                // Cancel handler
                if (Program.Default.Cancel.State)
                    return false;

                // Handle only MKV files
                if (!Tools.IsMkvFile(fileinfo))
                    continue;

                // Write all the different sidecar file types
                foreach (Info.MediaInfo.ParserType parser in Enum.GetValues(typeof(Info.MediaInfo.ParserType)))
                    if (!GetMediaInfoSidecar(fileinfo, parser, out bool modified, out Info.MediaInfo _))
                        errorcount++;
                    else if (modified)
                        modifiedcount++;

                // Next file
            }

            // Stop the timer
            timer.Stop();
            ConsoleEx.WriteLine("");
            ConsoleEx.WriteLine($"Total files : {fileList.Count}");
            ConsoleEx.WriteLine($"Modified files : {modifiedcount}");
            ConsoleEx.WriteLine($"Error files : {errorcount}");
            ConsoleEx.WriteLine($"Processing time : {timer.Elapsed}");
            ConsoleEx.WriteLine("");

            return true;
        }

        private bool ProcessFile(FileInfo fileinfo, out bool modified)
        {
            // Init
            modified = false;

            // Delete unwanted files, anything not in our extensions lists
            if (!keepExtensions.Contains(fileinfo.Extension)  &&
                !remuxExtensions.Contains(fileinfo.Extension))
            {
                // Delete the file
                ConsoleEx.WriteLine($"Deleting file with undesired extension : {fileinfo.Extension}");
                if (!FileEx.DeleteFile(fileinfo.FullName))
                    return false;

                // File deleted, do not continue processing
                modified = true;
                return true;
            }

            // Sidecar files must have a matching MKV media file
            if (sidecarExtensions.Contains(fileinfo.Extension))
            {
                // Get the matching MKV file
                string mediafile = Path.ChangeExtension(fileinfo.FullName, ".mkv");

                // If the media file does not exists, delete the sidecar file
                if (!File.Exists(mediafile))
                {
                    ConsoleEx.WriteLine("Deleting sidecar file with no matching MKV file");
                    if (!FileEx.DeleteFile(fileinfo.FullName))
                        return false;

                    // File deleted, do not continue processing
                    modified = true;
                    return true;
                }
            }

            // ReMux undesirable containers matched by extension
            if (remuxExtensions.Contains(fileinfo.Extension))
            {
                // ReMux the file
                ConsoleEx.WriteLine($"ReMux file matched by extension : {fileinfo.Extension}");
                if (!Convert.ReMuxToMkv(fileinfo.FullName, out string outputname))
                    return false;

                // Continue processing with the new file name
                modified = true;
                fileinfo = new FileInfo(outputname);
            }

            // By now all the media files we are processing should be MKV files
            if (!Tools.IsMkvFile(fileinfo))
                // Skip non-MKV files
                return true;

            // Get the file media info
            if (!GetMediaInfo(fileinfo, out Info.MediaInfo ffprobe, out Info.MediaInfo mkvmerge, out Info.MediaInfo mediainfo))
                return false;

            // Re-Encode formats that cannot be direct-played, e.g. MPEG2, WMAPro
            // Logic uses FFProbe data
            if (ffprobe.FindNeedReEncode(reencodeVideoCodecs, reencodeAudioCodecs, out Info.MediaInfo keep, out Info.MediaInfo reencode))
            {
                ConsoleEx.WriteLine("Found tracks that need to be re-encoded:");
                keep.WriteLine("Passthrough");
                reencode.WriteLine("ReEncode");

                // Convert streams that need re-encoding, copy the rest of the streams as is
                if (!Convert.ConvertToMkv(fileinfo.FullName, videoEncodeQuality, audioEncodeCodec, keep, reencode, out string outputname))
                    return false;

                // Continue processing with the new file name and new media info
                modified = true;
                fileinfo = new FileInfo(outputname);
                if (!GetMediaInfo(fileinfo, out ffprobe, out mkvmerge, out mediainfo))
                    return false;
            }

            // Change all tracks with an unknown language to the default language
            // Logic uses MKVMerge data
            if (mkvmerge.FindUnknownLanguage(out Info.MediaInfo known, out Info.MediaInfo unknown))
            {
                ConsoleEx.WriteLine($"Found tracks with an unknown language, setting to \"{defaultLanguage}\":");
                known.WriteLine("Known");
                unknown.WriteLine("Unknown");

                // Set the track language to the default language
                // MKVPropEdit uses track numbers, not track id's
                if (unknown.GetTrackList().Any(info => !Info.SetMkvTrackLanguage(fileinfo.FullName, info.Number, defaultLanguage)))
                {
                    return false;
                }

                // Continue processing with new media info
                modified = true;
                fileinfo.Refresh();
                if (!GetMediaInfo(fileinfo, out ffprobe, out mkvmerge, out mediainfo))
                    return false;
            }

            // Filter out all the undesired tracks
            if (mkvmerge.FindNeedRemove(keepLanguages, out keep, out Info.MediaInfo remove))
            {
                ConsoleEx.WriteLine("Found tracks that need to be removed:");
                keep.WriteLine("Keep");
                remove.WriteLine("Remove");

                // ReMux and only keep the specified tracks
                // MKVMerge uses track id's, not track numbers
                if (!Convert.ReMuxToMkv(fileinfo.FullName, keep, out string outputname))
                    return false;

                // Continue processing with new media info
                modified = true;
                fileinfo = new FileInfo(outputname);
                if (!GetMediaInfo(fileinfo, out ffprobe, out mkvmerge, out mediainfo))
                    return false;
            }

            // Do we need to remux any tracks
            if (mediainfo.FindNeedReMux(out keep, out Info.MediaInfo remux))
            {
                ConsoleEx.WriteLine("Found tracks that need to be re-muxed:");
                keep.WriteLine("Keep");
                remux.WriteLine("ReMux");

                // ReMux the file in-place, we ignore the track details
                if (!Convert.ReMuxToMkv(fileinfo.FullName, out string outputname))
                    return false;

                // Continue processing with new media info
                modified = true;
                fileinfo = new FileInfo(outputname);
                if (!GetMediaInfo(fileinfo, out ffprobe, out mkvmerge, out mediainfo))
                    return false;
            }

            // TODO : Verify content can DirectPlay
            // TODO : Verify the integrity of the media file and the streams in the file

            // Stream check
            // Our file must have just 1 video track, 1 or more audio tracks, 0 or more subtitle tracks
            if (mediainfo.Video.Count != 1 || mediainfo.Audio.Count == 0)
            {
                // File is missing required streams
                ConsoleEx.WriteLineError($"File missing required tracks : Video Count : {mediainfo.Video.Count} : Audio Count {mediainfo.Audio.Count} : Subtitle Count {mediainfo.Subtitle.Count}");

                // Delete the file
                if (AppOptions.Default.DeleteFailedFiles)
                    FileEx.DeleteFile(fileinfo.FullName);

                return false;
            }

            // Done
            return true;
        }

        private bool ReMuxFile(FileInfo fileinfo, out bool modified)
        {
            // Init
            modified = false;

            // ReMux the file
            if (!Convert.ReMuxToMkv(fileinfo.FullName, out string _))
                return false;

            // Modified
            modified = true;
            return true;
        }

        private bool ReEncodeFile(FileInfo fileinfo, out bool modified)
        {
            // Init
            modified = false;

            // ReEncode the file
            if (!Convert.ConvertToMkv(fileinfo.FullName, out string _))
                return false;

            // Modified
            modified = true;
            return true;
        }

        private bool GetMediaInfoSidecar(FileInfo fileinfo, Info.MediaInfo.ParserType parser, out bool modified, out Info.MediaInfo mediainfo)
        {
            // Init
            modified = false;
            mediainfo = null;

            // MKV tools only work on MKV files
            if (parser == Info.MediaInfo.ParserType.MkvMerge &&
                !Tools.IsMkvFile(fileinfo))
            {
                return true;
            }

            // TODO : Chance of race condition between reading media, external write to same media, and writing sidecar

            // Create or read the sidecar file
            bool createsidecar = false;
            string sidecarfile = Path.ChangeExtension(fileinfo.FullName, $".{parser.ToString()}");
            if (File.Exists(sidecarfile))
            {
                // We are explicitly setting the sidecar modified time to match the media modified time
                // We look for changes to the media file by comparing the modified times
                FileInfo sidecarinfo = new FileInfo(sidecarfile);
                fileinfo = new FileInfo(fileinfo.FullName);
                if (fileinfo.LastWriteTimeUtc != sidecarinfo.LastWriteTimeUtc)
                    createsidecar = true;
            }
            else
                // Create the file
                createsidecar = true;

            // Create the sidecar
            string sidecartext;
            if (createsidecar)
            {
                // Use the specified stream parser tool
                switch (parser)
                {
                    case Info.MediaInfo.ParserType.MediaInfo:
                        // Get the stream info from MediaInfo
                        if (!Info.GetMediaInfoXml(fileinfo.FullName, out sidecartext) ||
                            !Info.GetMediaInfoFromXml(sidecartext, out mediainfo))
                            return false;
                        break;
                    case Info.MediaInfo.ParserType.MkvMerge:
                        // Get the stream info from MKVMerge
                        if (!Info.GetMkvInfoJson(fileinfo.FullName, out sidecartext) ||
                            !Info.GetMkvInfoFromJson(sidecartext, out mediainfo))
                            return false;
                        break;
                    case Info.MediaInfo.ParserType.FfProbe:
                        // Get the stream info from FFProbe
                        if (!Info.GetFfProbeInfoJson(fileinfo.FullName, out sidecartext) ||
                            !Info.GetFfProbeInfoFromJson(sidecartext, out mediainfo))
                            return false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(parser), parser, null);
                }

                try
                {
                    // Write the text to the sidecar file
                    ConsoleEx.WriteLine($"Writing stream info to sidecar file : \"{sidecarfile}\"");
                    File.WriteAllText(sidecarfile, sidecartext);

                    // Set the sidecar modified time to match the media modified time
                    File.SetLastWriteTimeUtc(sidecarfile, fileinfo.LastWriteTimeUtc);
                }
                catch (Exception e)
                {
                    ConsoleEx.WriteLineError(e);
                    return false;
                }
            }
            else
            {
                // Read the sidecar file
                try
                {
                    ConsoleEx.WriteLine($"Reading stream info from sidecar file : \"{sidecarfile}\"");
                    sidecartext = File.ReadAllText(sidecarfile);
                }
                catch (Exception e)
                {
                    ConsoleEx.WriteLineError(e);
                    return false;
                }

                // Use the specified stream parser tool to convert the text
                switch (parser)
                {
                    case Info.MediaInfo.ParserType.MediaInfo:
                        // Convert the stream info using MediaInfo
                        if (!Info.GetMediaInfoFromXml(sidecartext, out mediainfo))
                            return false;
                        break;
                    case Info.MediaInfo.ParserType.MkvMerge:
                        // Convert the stream info using MKVMerge
                        if (!Info.GetMkvInfoFromJson(sidecartext, out mediainfo))
                            return false;
                        break;
                    case Info.MediaInfo.ParserType.FfProbe:
                        // Convert the stream info using FFProbe
                        if (!Info.GetFfProbeInfoFromJson(sidecartext, out mediainfo))
                            return false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(parser), parser, null);
                }
            }

            return true;
        }

/*
        bool GetMediaInfo(string filename, out Info.MediaInfo ffprobe, out Info.MediaInfo mkvmerge, out Info.MediaInfo mediainfo)
        {
            FileInfo fileinfo = new FileInfo(filename);
            return GetMediaInfo(fileinfo, out ffprobe, out mkvmerge, out mediainfo);
        }
*/

        private bool GetMediaInfo(FileInfo fileinfo, out Info.MediaInfo ffprobe, out Info.MediaInfo mkvmerge, out Info.MediaInfo mediainfo)
        {
            ffprobe = null;
            mkvmerge = null;
            mediainfo = null;

            return GetMediaInfo(fileinfo, Info.MediaInfo.ParserType.FfProbe, out ffprobe) && 
                   GetMediaInfo(fileinfo, Info.MediaInfo.ParserType.MkvMerge, out mkvmerge) && 
                   GetMediaInfo(fileinfo, Info.MediaInfo.ParserType.MediaInfo, out mediainfo);
        }

/*
        bool GetMediaInfo(string filename, Info.MediaInfo.ParserType parser, out Info.MediaInfo mediainfo)
        {
            FileInfo fileinfo = new FileInfo(filename);
            return GetMediaInfo(fileinfo, parser, out mediainfo);
        }
*/

        private bool GetMediaInfo(FileInfo fileinfo, Info.MediaInfo.ParserType parser, out Info.MediaInfo mediainfo)
        {
            // Read or create sidecar file
            mediainfo = null;
            if (AppOptions.Default.UseSidecarFiles)
                return GetMediaInfoSidecar(fileinfo, parser, out bool _, out mediainfo);
            
            // Use the specified stream parser tool
            switch (parser)
            {
                case Info.MediaInfo.ParserType.MediaInfo:
                    return Info.GetMediaInfo(fileinfo.FullName, out mediainfo);
                case Info.MediaInfo.ParserType.MkvMerge:
                    return Info.GetMkvInfo(fileinfo.FullName, out mediainfo);
                case Info.MediaInfo.ParserType.FfProbe:
                    return Info.GetFfProbeInfo(fileinfo.FullName, out mediainfo);
                default:
                    throw new ArgumentOutOfRangeException(nameof(parser), parser, null);
            }
        }

        private HashSet<string> sidecarExtensions;
        private HashSet<string> keepExtensions;
        private HashSet<string> remuxExtensions;
        private HashSet<string> reencodeAudioCodecs;
        private HashSet<string> keepLanguages;
        private string defaultLanguage;
        private string audioEncodeCodec;
        private int videoEncodeQuality;
        private List<Info.VideoInfo> reencodeVideoCodecs;
    }
}
