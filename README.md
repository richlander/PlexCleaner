# PlexCleaner

Utility to optimize media files for Direct Play in Plex, Emby, Jellyfin.

## License

Licensed under the [MIT License](./LICENSE)  
![GitHub](https://img.shields.io/github/license/ptr727/PlexCleaner)

## Build Status

Code and Pipeline is on [GitHub](https://github.com/ptr727/PlexCleaner).  
Docker images are published on [Docker Hub](https://hub.docker.com/u/ptr727/plexcleaner).

![GitHub Last Commit](https://img.shields.io/github/last-commit/ptr727/PlexCleaner?logo=github)  
![GitHub Workflow Status](https://img.shields.io/github/workflow/status/ptr727/PlexCleaner/Build%20and%20Publish%20Pipeline?logo=github)  
![GitHub Latest Release)](https://img.shields.io/github/v/release/ptr727/PlexCleaner?logo=github)  
![Docker Latest Release](https://img.shields.io/docker/v/ptr727/plexcleaner/latest?label=latest&logo=docker)  

## Release Notes

- Version 2.5:
  - Changed the config file JSON schema to simplify authoring of multi-value settings, resolves [Refactor codec encoding options #85](https://github.com/ptr727/PlexCleaner/issues/85)
    - Older file schemas will automatically be upgraded without requiring user input.
    - Comma separated lists in string format converted to array of strings.
      - Old: `"ReMuxExtensions": ".avi,.m2ts,.ts,.vob,.mp4,.m4v,.asf,.wmv,.dv",`
      - New: `"ReMuxExtensions": [ ".avi", ".m2ts", ".ts", ".vob", ".mp4", ".m4v", ".asf", ".wmv", ".dv" ]`
    - Multiple VideoFormat comma separated lists in strings converted to array of objects.
      - Old:
        - `"ReEncodeVideoFormats": "mpeg2video,mpeg4,msmpeg4v3,msmpeg4v2,vc1,h264,wmv3,msrle,rawvideo,indeo5"`
        - `"ReEncodeVideoCodecs": "*,dx50,div3,mp42,*,*,*,*,*,*"`
        - `"ReEncodeVideoProfiles": "*,*,*,*,*,Constrained Baseline@30,*,*,*,*"`
      - New: `"ReEncodeVideo": [ { "Format": "mpeg2video" }, { "Format": "mpeg4", "Codec": "dx50" }, ... ]`
  - Replaced [GitVersion](https://github.com/GitTools/GitVersion) with [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) as versioning tool, resolves [Release develop builds as pre-release #16](https://github.com/ptr727/PlexCleaner/issues/16).
    - Main branch will now build using `Release` configuration, other branches will continue building with `Debug` configuration.
    - Prerelease builds are now posted to GitHub releases tagges as `pre-release`, Docker builds continue to be tagged as `develop`.
  - Docker builds are now also pushed to [GitHub Container Registry](https://github.com/ptr727/PlexCleaner/pkgs/container/plexcleaner).
    - Builds will continue to push to Docker Hub while it remains free to use.
  - Added a xUnit unit test project.
    - Currently the only tests are for config and sidecar JSON schema backwards compatibility.
  - Code cleanup and refactoring to make current versions of Visual Studio and Rider happy.
- See [Release History](./HISTORY.md) for older Release Notes.

## Use Cases

The objective of the tool is to modify media content such that it will Direct Play in [Plex](https://support.plex.tv/articles/200250387-streaming-media-direct-play-and-direct-stream/), [Emby](https://support.emby.media/support/solutions/articles/44001920144-direct-play-vs-direct-streaming-vs-transcoding), [Jellyfin](https://jellyfin.org/docs/plugin-api/MediaBrowser.Model.Session.PlayMethod.html).  
Different Plex server and client versions suffer different playback issues, and issues are often associated with specific media attributes.  
Occasionally Plex would fix the issue, other times the only solution is modification of the media file.

Below are a few examples of issues I've experienced over the many years of using Plex on Roku, NVidia Shield, and Apple TV:

- Container file formats other than MKV and MP4 are not supported by the client platform, re-multiplex to MKV.
- MPEG2 licensing prevents the platform from hardware decoding the content, re-encode to H.264.
- Some video codecs like MPEG-4 or VC1 cause playback issues, re-encode to H.264.
- Some H.264 video profiles like "Constrained Baseline@30" cause hangs on Roku, re-encode to H.264 "High@40".
- Interlaced video cause playback issues, re-encode to H.264 using HandBrake and de-interlace using `--comb-detect --decomb` options.
- Some audio codecs like Vorbis or WMAPro are not supported by the client platform, re-encode to AC3.
- Some subtitle tracks like VOBsub cause hangs when the MuxingMode attribute is not set, re-multiplex the file.
- Automatic audio and subtitle track selection requires the track language to be set, set the language for unknown tracks.
- Automatic track selection ignores the Default track attribute and uses the first track when multiple tracks are present, remove duplicate tracks.
- Corrupt files cause playback issues, verify stream integrity, try to automatically repair, or delete.
- Some WiFi or 100mbps Ethernet connected devices with small read buffers cannot play high bitrate content, warn when media bitrate exceeds the network bitrate.
- Dolby Vision is only supported on DV capable hardware, warn when the HDR profile is `Dolby Vision` (profile 5) vs. `Dolby Vision / SMPTE ST 2086` (profile 7).

## Installation

### Windows

- Install [.NET 6 Runtime](https://docs.microsoft.com/en-us/dotnet/core/install/windows).
- Download [PlexCleaner](https://github.com/ptr727/PlexCleaner/releases/latest) and extract pre-compiled binaries.
- Or compile from [code](https://github.com/ptr727/PlexCleaner.git) using [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/) or [Visual Studio Code](https://code.visualstudio.com/download) or the [.NET 6 SDK](https://dotnet.microsoft.com/download).
- Install the required 3rd Party tools:
  - The 3rd party tools are downloaded in the `Tools` folder.
  - Make sure the folder exists, the default location is in the same folder as the binary.
  - [Download](https://www.7-zip.org/download.html) the 7-Zip commandline tool, e.g. [7z1805-extra.7z](https://www.7-zip.org/a/7z1805-extra.7z)
  - Extract the contents of the archive to the `Tools\SevenZip` folder.
  - The 7-Zip commandline tool should be in `Tools\SevenZip\x64\7za.exe`
  - With 7-Zip ready, the other 3rd party tools can automatically be downloaded and extracted by running:
    - `PlexCleaner.exe --settingsfile PlexCleaner.json checkfornewtools`
  - The tool version information will be stored in `Tools\Tools.json`
  - Keep the 3rd party tools updated by periodically running the `checkfornewtools` command.

### Linux

- Automatic downloading of Linux 3rd party tools are not currently supported, consider using the [Docker](https://hub.docker.com/u/ptr727/plexcleaner) build instead.
- Listed steps are for Ubuntu, adjust as appropriate for your distribution.
- Install prerequisites:
  - `sudo apt update`
  - `sudo apt upgrade -y`
  - `sudo apt install -y wget git apt-transport-https lsb-release software-properties-common p7zip-full`
- Install [.NET 6 Runtime](https://docs.microsoft.com/en-us/dotnet/core/install/linux):
  - `wget -q https://packages.microsoft.com/config/ubuntu/$(lsb_release -sr)/packages-microsoft-prod.deb`
  - `sudo dpkg -i packages-microsoft-prod.deb`
  - `sudo apt update`
  - `sudo apt install -y dotnet-runtime-6.0`
  - `dotnet --info`
- Install the required 3rd Party tools:
  - Install [FfMpeg](https://launchpad.net/~savoury1/+archive/ubuntu/ffmpeg5):
    - `sudo add-apt-repository -y ppa:savoury1/graphics`
    - `sudo add-apt-repository -y ppa:savoury1/multimedia`
    - `sudo add-apt-repository -y ppa:savoury1/ffmpeg4`
    - `sudo add-apt-repository -y ppa:savoury1/ffmpeg5`
    - `sudo apt update`
    - `sudo apt install -y ffmpeg`
    - `ffmpeg -version`
  - Install [MediaInfo](https://mediaarea.net/en/MediaInfo/Download/Ubuntu):
    - `wget https://mediaarea.net/repo/deb/repo-mediaarea_1.0-19_all.deb`
    - `sudo dpkg -i repo-mediaarea_1.0-19_all.deb`
    - `sudo apt update`
    - `sudo apt install -y mediainfo`
    - `mediainfo --version`
  - Install [HandBrake](https://handbrake.fr/docs/en/latest/get-handbrake/download-and-install.html):
    - `sudo add-apt-repository -y ppa:stebbins/handbrake-releases`
    - `sudo apt update`
    - `sudo apt install -y handbrake-cli`
    - `HandBrakeCLI --version`
  - Install [MKVToolNix](https://mkvtoolnix.download/downloads.html):
    - `wget -q -O - https://mkvtoolnix.download/gpg-pub-moritzbunkus.txt | sudo apt-key add -`
    - `sudo sh -c 'echo "deb https://mkvtoolnix.download/ubuntu/ $(lsb_release -sc) main" >> /etc/apt/sources.list.d/bunkus.org.list'`
    - `sudo apt update`
    - `sudo apt install -y mkvtoolnix`
    - `mkvmerge --version`
  - Keep the 3rd party tools updated by periodically running `sudo apt update` and `sudo apt upgrade`.
- Download [PlexCleaner](https://github.com/ptr727/PlexCleaner/releases/latest) and extract pre-compiled binaries.
- Or compile from [code](https://github.com/ptr727/PlexCleaner.git) using [.NET 6 SDK](https://docs.microsoft.com/en-us/dotnet/core/install/linux).

### Docker

- Docker builds are published on [Docker Hub](https://hub.docker.com/u/ptr727/plexcleaner), and updated weekly.
- The container has all the prerequisite 3rd party tools pre-installed.
- Map your host volumes, and make sure the user has permission to access and modify media files.
- The container is intended to be used in interactive mode, for long running operations run in a `screen` session.

Example, run an interactive shell:

```console
docker pull ptr727/plexcleaner

docker run \
  -it \
  --rm \
  --user nobody:users \
  --volume /data/media:/media:rw \
  ptr727/plexcleaner \
  /bin/bash

/PlexCleaner/PlexCleaner --version
```

Example, run a command:

```console
docker pull ptr727/plexcleaner

screen

docker run \
  -it \
  --rm \
  --user nobody:users \
  --env TZ=America/Los_Angeles \
  --volume /data/media:/media:rw \
  ptr727/plexcleaner \
  /PlexCleaner/PlexCleaner \
    --settingsfile /media/PlexCleaner/PlexCleaner.json \
    --logfile /media/PlexCleaner/PlexCleaner.log --logappend \
    process \
    --mediafiles /media/Movies \
    --mediafiles /media/Series
```

## Configuration

Create a default configuration file by running:  
`PlexCleaner.exe --settingsfile PlexCleaner.json defaultsettings`

```jsonc
{
  // JSON Schema version
  "SchemaVersion": 2,
  // Tools options
  "ToolsOptions": {
    // Use system installed tools
    "UseSystem": false,
    // Tools folder
    "RootPath": ".\\Tools\\",
    // Tools directory relative to binary location
    "RootRelative": true,
    // Automatically check for new tools
    "AutoUpdate": false
  },
  // Convert options
  "ConvertOptions": {
    // Enable H.265 encoding, else use H.264
    "EnableH265Encoder": true,
    // Video encoding CRF quality, H.264 default is 23, H.265 default is 28
    "VideoEncodeQuality": 20,
    // Audio encoding codec
    "AudioEncodeCodec": "ac3"
  },
  // Process options
  "ProcessOptions": {
    // Delete empty folders
    "DeleteEmptyFolders": true,
    // Delete non-media files
    // Any file that is not in KeepExtensions or in ReMuxExtensions or MKV will be deleted
    "DeleteUnwantedExtensions": true,
    // File extensions to keep but not process, e.g. subtitles, cover art, info, partial, etc.
    "KeepExtensions": [
      ".partial~",
      ".nfo",
      ".jpg",
      ".srt",
      ".smi",
      ".ssa",
      ".ass",
      ".vtt"
    ],
    // Enable re-mux
    "ReMux": true,
    // File extensions to remux to MKV
    "ReMuxExtensions": [
      ".avi",
      ".m2ts",
      ".ts",
      ".vob",
      ".mp4",
      ".m4v",
      ".asf",
      ".wmv",
      ".dv"
    ],
    // Enable de-interlace
    // Note de-interlace detection is not absolute
    "DeInterlace": true,
    // Enable re-encode
    "ReEncode": true,
    // Re-encode the video if the Format, Codec, and Profile values match
    // Empty fields will match with any value
    // Use FFProbe attribute naming, and the `printmediainfo` command to get media info
    "ReEncodeVideo": [
      {
        "Format": "mpeg2video"
      },
      {
        "Format": "mpeg4",
        "Codec": "dx50"
      },
      {
        "Format": "msmpeg4v3",
        "Codec": "div3"
      },
      {
        "Format": "msmpeg4v2",
        "Codec": "mp42"
      },
      {
        "Format": "vc1"
      },
      {
        "Format": "h264",
        "Profile": "Constrained Baseline@30"
      },
      {
        "Format": "wmv3"
      },
      {
        "Format": "msrle"
      },
      {
        "Format": "rawvideo"
      },
      {
        "Format": "indeo5"
      }
    ],
    // Re-encode matching audio codecs
    // If the video format is not H264/5, video will automatically be converted to H264/5 to avoid audio sync issues
    // Use FFProbe attribute naming, and the `printmediainfo` command to get media info
    "ReEncodeAudioFormats": [
      "flac",
      "mp2",
      "vorbis",
      "wmapro",
      "pcm_s16le",
      "opus",
      "wmav2",
      "pcm_u8",
      "adpcm_ms"
    ],
    // Set default language if tracks have an undefined language
    "SetUnknownLanguage": true,
    // Default track language
    "DefaultLanguage": "eng",
    // Enable removing of unwanted language tracks
    "RemoveUnwantedLanguageTracks": true,
    // Track languages to keep
    // Use ISO 639-2 3 letter short form, see https://www.loc.gov/standards/iso639-2/php/code_list.php
    "KeepLanguages": [
      "eng",
      "afr",
      "chi",
      "ind"
    ],
    // Enable removing of duplicate tracks of the same type and language
    // Priority is given to tracks marked as Default
    // Forced subtitle tracks are prioritized
    // Subtitle tracks containing "SDH" in the title are de-prioritized
    // Audio tracks containing "Commentary" in the title are de-prioritized
    "RemoveDuplicateTracks": true,
    // If no Default audio tracks are found, tracks are prioritized by codec type
    // Use MKVMerge attribute naming, and the `printmediainfo` command to get media info
    "PreferredAudioFormats": [
      "truehd atmos",
      "truehd",
      "dts-hd master audio",
      "dts-hd high resolution audio",
      "dts",
      "e-ac-3",
      "ac-3"
    ],
    // Enable removing of all tags from the media file
    // Track title information is not removed
    "RemoveTags": true,
    // Speedup media metadata processing by saving media info in sidecar files
    "UseSidecarFiles": true,
    // Invalidate sidecar files when tool versions change
    "SidecarUpdateOnToolChange": false,
    // Enable verify
    "Verify": true,
    // Restore media file modified timestamp to original value
    "RestoreFileTimestamp": false,
    // List of media files to ignore, e.g. repeat processing failures, but media still plays
    // Non-ascii characters must be JSON escaped
    "FileIgnoreList": [
      "\\\\server\\share1\\path1\\file1.mkv",
      "\\\\server\\share2\\path2\\file2.mkv"
    ]
  },
  // Monitor options
  "MonitorOptions": {
    // Time to wait after detecting a file change
    "MonitorWaitTime": 60,
    // Time to wait between file retry operations
    "FileRetryWaitTime": 5,
    // Number of times to retry a file operation
    "FileRetryCount": 2
  },
  // Verify options
  "VerifyOptions": {
    // Attempt to repair media files that fail verification
    "AutoRepair": true,
    // Delete media files that fail repair
    "DeleteInvalidFiles": false,
    // Add media files that fail repair to the FileIgnoreList setting
    "RegisterInvalidFiles": true,
    // Minimum required playback duration in seconds
    "MinimumDuration": 300,
    // Time in seconds to verify media streams, 0 will verify entire file
    "VerifyDuration": 0,
    // Time in seconds to count interlaced frames, 0 will count entire file
    "IdetDuration": 0,
    // Maximum bitrate in bits per second, 0 will skip computation
    "MaximumBitrate": 100000000,
    // Skip files older than the minimum file age in days, 0 will process all files
    "MinimumFileAge": 0
  }
}
```

## Usage

Commandline options:  
`PlexCleaner.exe --help`

```console
> ./PlexCleaner --help
Description:
  Utility to optimize media files for Direct Play in Plex, Emby, Jellyfin

Usage:
  PlexCleaner [command] [options]

Options:
  --settingsfile <settingsfile> (REQUIRED)  Path to settings file
  --logfile <logfile>                       Path to log file
  --logappend                               Append to the log file vs. default overwrite
  --version                                 Show version information
  -?, -h, --help                            Show help and usage information

Commands:
  defaultsettings   Write default values to settings file
  checkfornewtools  Check for and download new tools
  process           Process media files
  monitor           Monitor and process media file changes in folders
  remux             Re-Multiplex media files
  reencode          Re-Encode media files
  deinterlace       De-Interlace media files
  verify            Verify media files
  createsidecar     Create sidecar files
  getsidecarinfo    Print sidecar file attribute information
  gettagmap         Print attribute tag-map created from media files
  getmediainfo      Print media file attribute information
  gettoolinfo       Print tool file attribute information
  getbitrateinfo    Print media file bitrate information
  upgradesidecar    Upgrade sidecar file schemas
  removesubtitles   Remove subtitles
```

The `--settingsfile` JSON settings file is required.  
The `--logfile` output log file is optional, the file will be overwritten unless `--logappend` is set.

One of the commands must be specified.

### Process Media Files

```console
> .\PlexCleaner.exe process --help
process:
  Process media files.

Usage:
  PlexCleaner process [options]

Options:
  --mediafiles <mediafiles> (REQUIRED)    List of media files or folders.
  --testsnippets                          Create short video clips, useful during testing.
  --testnomodify                          Do not make any modifications, useful during testing.
  -?, -h, --help                          Show help and usage information.
```

The `process` command will use the JSON configuration settings to conditionally modify the media content.  
The `--mediafiles` option can include multiple files and directories, e.g. `--mediafiles path1 --mediafiles path2 --mediafiles file1 --mediafiles file2`.  

Example:  
`PlexCleaner.exe --settingsfile "PlexCleaner.json" --logfile "PlexCleaner.log" --appendtolog process --mediafiles "C:\Foo\Test.mkv" --mediafiles "D:\Media"`

The following processing will be done:

- Delete files with extensions not in the `KeepExtensions` list.
- Re-multiplex containers in the `ReMuxExtensions` list to MKV container format.
- Remove all tags, titles, thumbnails, and attachments from the media file.
- Set the language to `DefaultLanguage` for any track with an undefined language.
- If multiple audio tracks of the same language but different encoding formats are present, set the default track based on `PreferredAudioFormats`.
- Remove tracks with languages not in the `KeepLanguages` list.
- Remove duplicate tracks, where duplicates are tracks of the same type and language.
- Re-multiplex the media file if required.
- De-interlace the video track if interlaced.
- Re-encode video to H.264/5 if video format matches `ReEncodeVideo`.
- Re-encode audio to `AudioEncodeCodec` if audio matches the `ReEncodeAudioFormats` list.
- Verify the media container and stream integrity, if corrupt try to automatically repair, else conditionally delete the file.

### Re-Multiplex, Re-Encode, De-Interlace, Verify

The `remux` command will re-multiplex the media files using `MKVMerge`.

The `reencode` command will re-encode the media files using `FFMPeg` and H.264 at `VideoEncodeQuality` for video, and `AudioEncodeCodec` for audio.

The `deinterlace` command will de-interlace interlaced media files using `HandBrake --comb-detect --decomb`.

The `verify` command will use `FFmpeg` to render the file streams and report on any container or stream errors.  

### Monitor

The `monitor` command will watch the specified folders for changes, and process the directories with changes.

Note that the [FileSystemWatcher](https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher) is not always reliable on Linux or NAS Samba shares.  
Also note that changes made directly to the underlying filesystem will not trigger when watching the SMB shares, e.g. when a Docker container writes to a mapped volume, the SMB view of that volume will not trigger.

### CreateSidecar, UpgradeSidecar

The `createsidecar` command will create sidecar files.  
All state attributes will be deleted, e.g. the file will be re-verified.

The `upgradesidecar` command will upgrade the sidecar schemas (not tool info) to the current version.  
When possible the verified state of the file will be maintained, avoiding the cost of unnecessary and time consuming re-verification operations.

### GetTagMap, GetMediaInfo, GetToolInfo, GetSidecarInfo, GetBitrateInfo

The `gettagmap` command will calculate and print attribute mappings between between different media information tools.

The `getmediainfo` command will print media attribute information.

The `gettoolinfo` command will print tool attribute information.

The `getsidecarinfo` command will print sidecar attribute information.

The `getbitrateinfo` command will calculate and print media bitrate information.

### RemoveSubtitles

The `removesubtitles` command will remove all subtitle tracks.

## 3rd Party Tools

- [7-Zip](https://www.7-zip.org/)
- [MediaInfo](https://mediaarea.net/en-us/MediaInfo/)
- [HandBrake](https://handbrake.fr/)
- [MKVToolNix](https://mkvtoolnix.download/)
- [FFmpeg](https://www.ffmpeg.org/)
- [ISO language codes](http://www-01.sil.org/iso639-3/download.asp)
- [Xml2CSharp](http://xmltocsharp.azurewebsites.net/)
- [quicktype](https://quicktype.io/)
- [regex101.com](https://regex101.com/)
- [HtmlAgilityPack](https://html-agility-pack.net/)
- [Newtonsoft.Json](https://www.newtonsoft.com/json)
- [System.CommandLine](https://github.com/dotnet/command-line-api)
- [Serilog](https://serilog.net/)

## Sample Media Files

- [Kodi](https://kodi.wiki/view/Samples)
- [JellyFish](http://jell.yfish.us/)
- [DemoWorld](https://www.demo-world.eu/2d-demo-trailers-hd/)
- [MPlayer](https://samples.mplayerhq.hu/)
