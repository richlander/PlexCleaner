# PlexCleaner

Utility to optimize media files for DirectPlay on Plex.

## License

Licensed under the [MIT License](./LICENSE)  
![GitHub](https://img.shields.io/github/license/ptr727/PlexCleaner)

## Build Status

Code and Pipeline is on [GitHub](https://github.com/ptr727/PlexCleaner)  
![GitHub Last Commit](https://img.shields.io/github/last-commit/ptr727/PlexCleaner?logo=github)  
![GitHub Workflow Status](https://img.shields.io/github/workflow/status/ptr727/PlexCleaner/Build%20and%20Publish%20Pipeline?logo=github)  
![GitHub Latest Release)](https://img.shields.io/github/v/release/ptr727/PlexCleaner?logo=github)

## Getting Started

### Use Cases

The objective of the tool is to modify media content such that it will [Direct Play](https://support.plex.tv/articles/200250387-streaming-media-direct-play-and-direct-stream/) in Plex.  
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
- Some WiFi or 100mbps Ethernet connected devices with small read buffers cannot play high bitrate content, verify content bitrate does not exceed the network bitrate.

### Installation

#### Windows

- Install [.NET 5 Runtime](https://docs.microsoft.com/en-us/dotnet/core/install/windows).
- [Download](https://github.com/ptr727/PlexCleaner/releases/latest) and extract pre-compiled binaries.
- Or compile from [code](https://github.com/ptr727/PlexCleaner.git) using [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/) or [Visual Studio Code](https://code.visualstudio.com/download) or the [.NET 5 SDK](https://dotnet.microsoft.com/download).
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

#### Linux

- Precompiled Linux builds are not currently available.
- Downloading Linux 3rd party tools are not currently supported.
- Listed steps are for Ubuntu, adjust as appropriate for your distribution.
- Install prerequisites:
  - `sudo apt update`
  - `sudo apt install -y wget git apt-transport-https lsb-release software-properties-common p7zip-full`
- Install [.NET 5 SDK](https://docs.microsoft.com/en-us/dotnet/core/install/linux):
  - TODO: How to convert distribution to lowercase using $(lsb_release -si) to create a generic download URL, e.g. `debian` instead of `Debian`?
  - `wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -sr)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb`
  - `sudo dpkg -i packages-microsoft-prod.deb`
  - `sudo apt update`
  - `sudo apt install -y dotnet-sdk-5.0`
  - `dotnet --version`
- Install the required 3rd Party tools:
  - Install [FfMpeg](https://launchpad.net/~savoury1/+archive/ubuntu/ffmpeg4):
    - `sudo add-apt-repository -y ppa:savoury1/ffmpeg4`
    - `sudo apt update`
    - `sudo apt install -y ffmpeg`
    - `ffmpeg -version`
  - Install [MediaInfo](https://mediaarea.net/en/MediaInfo/Download/Ubuntu):
    - `wget https://mediaarea.net/repo/deb/repo-mediaarea_1.0-13_all.deb`
    - `sudo dpkg -i repo-mediaarea_1.0-13_all.deb`
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
  - Keep the 3rd party tools updated by periodically running `sudo apt upgrade`.
- Clone the [repo](https://github.com/ptr727/PlexCleaner.git) and compile the code:
  - `git clone https://github.com/ptr727/PlexCleaner.git`
  - `cd PlexCleaner`
  - `dotnet build`

#### Docker

Build and run shell from source:

```console
cd Docker
docker build -t plexcleaner .  
docker image ls
docker run -it plexcleaner /bin/bash
cd /PlexCleaner/PlexCleaner/bin/Debug/net5.0
./PlexCleaner --help
exit
```

Run PlexCleaner from the published container:

```console
docker run \
  -it \
  ptr727/plexcleaner \
  --volume /mnt/media:/media:rw \
  --volume /mnt/config/plexcleaner:/config:r \
  /PlexCleaner/PlexCleaner/bin/Debug/net5.0/PlexCleaner --settingsfile /config/PlexCleaner.json process --mediafiles /media
```

### Configuration File

Create a default configuration file by running:  
`PlexCleaner.exe --settingsfile PlexCleaner.json defaultsettings`

```jsonc
{
  "ToolsOptions": {
    // Use system installed tools
    "UseSystem": false,
    // Tools folder
    "RootPath": ".\\Tools\\",
    // Tools directory relative to binary location
    "RootRelative": true
  },
  "ConvertOptions": {
    // Enable H.265 encoding, else use H.264
    "EnableH265Encoder": true,
    // Video encoding CRF quality, H.264 default is 23, H.265 default is 28
    "VideoEncodeQuality": 20,
    // Audio encoding codec
    "AudioEncodeCodec": "ac3"
  },
  "ProcessOptions": {
    // Delete empty folders
    "DeleteEmptyFolders": true,
    // Delete non-media files
    "DeleteUnwantedExtensions": true,
    // Files to keep, e.g. subtitle or partial files
    "KeepExtensions": ".partial~",
    // Enable re-mux
    "ReMux": true,
    // Remux files to MKV if the extension matches
    "ReMuxExtensions": ".avi,.m2ts,.ts,.vob,.mp4,.m4v,.asf,.wmv",
    // Enable de-interlace
    // Note de-interlace detection is not absolute
    "DeInterlace": true,
    // Enable re-encode
    "ReEncode": true,
    // Re-encode the video if the format, codec, and profile values match
    // * will match anything, the number of filter entries must match
    // Use FFProbe attribute naming, and the `printmediainfo` command to get media info
    "ReEncodeVideoFormats": "mpeg2video,mpeg4,msmpeg4v3,msmpeg4v2,vc1,h264",
    "ReEncodeVideoCodecs": "*,dx50,div3,mp42,*,*",
    "ReEncodeVideoProfiles": "*,*,*,*,*,Constrained Baseline@30",
    // Re-encode matching audio codecs
    // If the video format is not H264 or H265, video will automatically be converted to H264 to avoid audio sync issues
    // Use FFProbe attribute naming, and the `printmediainfo` command to get media info
    "ReEncodeAudioFormats": "flac,mp2,vorbis,wmapro,pcm_s16le",
    // Set default language if tracks have an undefined language
    "SetUnknownLanguage": true,
    // Default track language
    "DefaultLanguage": "eng",
    // Enable removing of unwanted language tracks
    "RemoveUnwantedLanguageTracks": true,
    // Track languages to keep
    // Use ISO 639-2 3 letter short form
    "KeepLanguages": "eng,afr,chi,ind",
    // Enable removing of duplicate tracks of the same type and language
    // Priority is given to tracks marked as Default
    // Forced subtitle tracks are prioritized
    // Subtitle tracks containing "SDH" in the title are de-prioritized
    // Audio tracks containing "Commentary" in the title are de-prioritized
    "RemoveDuplicateTracks": true,
    // If no Default audio tracks are found, tracks are prioritized by codec type
    // Use MKVMerge attribute naming, and the `printmediainfo` command to get media info
    "PreferredAudioFormats": "truehd atmos,truehd,dts-hd master audio,dts-hd high resolution audio,dts,e-ac-3,ac-3",
    // Enable removing of all tags from the media file
    // Track title information is not removed
    "RemoveTags": true,
    // Speedup media metadata processing by saving media info in sidecar files
    "UseSidecarFiles": true,
    // Invalidate sidecar files when tool versions change
    "SidecarUpdateOnToolChange": true,
    // Enable verify
    "Verify": true,
    // List of media files to ignore, e.g. repeat processing failures, but media still plays
    "FileIgnoreList": [
      "\\\\server\\share1\\path1\\file1.mkv",
      "\\\\server\\share2\\path2\\file2.mkv"
    ]
  },
  "MonitorOptions": {
    // Time to wait after detecting a file change
    "MonitorWaitTime": 60,
    // Time to wait between file retry operations
    "FileRetryWaitTime": 5,
    // Number of times to retry a file operation
    "FileRetryCount": 2
  },
  "VerifyOptions": {
    // Attempt to repair media files that fail verification
    "AutoRepair": true,
    // Delete media files that fail verification and fail repair
    "DeleteInvalidFiles": true,
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

### Commandline

Commandline options:  
`PlexCleaner.exe --help`

```console
PS C:\..\netcoreapp3.1> .\PlexCleaner.exe --help
PlexCleaner:
  Utility to optimize media files for DirectPlay on Plex

Usage:
  PlexCleaner [options] [command]

Options:
  --settingsfile <settingsfile> (REQUIRED)    Path to settings file
  --logfile <logfile>                         Path to log file
  --logappend                                 Append to the log file vs. default overwrite
  --version                                   Show version information
  -?, -h, --help                              Show help and usage information

Commands:
  defaultsettings     Write default values to settings file
  checkfornewtools    Check for and download new tools
  process             Process media files
  monitor             Monitor and process media file changes in folders
  remux               Re-Multiplex media files
  reencode            Re-Encode media files
  deinterlace         De-Interlace media files
  verify              Verify media files
  createsidecar       Create sidecar files
  getsidecar          Print sidecar file attribute information
  gettagmap           Print attribute tag-map created from media files
  getmediainfo        Print media file attribute information
  getbitrateinfo      Print media file bitrate information
```

The `--settingsfile` JSON settings file is required.  
The `--logfile` output log file is optional, the file will be overwritten unless `--logappend` is set.

One of the commands must be specified.

### Process Media Files

```console
PS C:\...\netcoreapp3.1> .\PlexCleaner.exe process --help
process:
  Process media files.

Usage:
  PlexCleaner process [options]

Options:
  --mediafiles <mediafiles> (REQUIRED)    List of media files or folders.
  --testsnippets                          Create short video clips, useful during testing.
  --testnomodify                          Do not make any modifications, useful during testing.
  -?, -h, --help                          Show help and usage information
```

The `process` command will use the JSON configuration settings to conditionally modify the media content.  
The `--mediafiles` option can point to a combination of files or folders.  

Example:  
`PlexCleaner.exe --settingsfile "PlexCleaner.json" --logfile "PlexCleaner.log" --appendtolog process --mediafiles "C:\Foo\Test.mkv" "D:\Media"`

The following processing will be done:

- Delete files with extensions not in the `KeepExtensions` list.
- Re-multiplex containers in the `ReMuxExtensions` list to MKV container format.
- Remove all tags from the media file.
- Set the language to `DefaultLanguage` for any track with an undefined language.
- Remove tracks with languages not in the `KeepLanguages` list.
- Remove duplicate tracks, where duplicates are tracks of the same type and language.
- Re-multiplex the media file if required.
- De-interlace the video track if interlaced.
- Re-encode video to H.264 at `VideoEncodeQuality` if video matches the `ReEncodeVideoFormats`, `ReEncodeVideoCodecs`, and `ReEncodeVideoProfiles` list.
- Re-encode audio to `AudioEncodeCodec` if audio matches the `ReEncodeAudioFormats` list.
- Verify the media container and stream integrity, if corrupt try to automatically repair, else delete the file.

### Re-Multiplex, Re-Encode, De-Interlace, Verify

The `remux` command will re-multiplex the media files using `MKVMerge`.

The `reencode` command will re-encode the media files using `FFMPeg` and H.264 at `VideoEncodeQuality` for video, and `AudioEncodeCodec` for audio.

The `deinterlace` command will de-interlace interlaced media files using `HandBrake --comb-detect --decomb`.

The `verify` command will use `FFmpeg` to render the file streams and report on any container or stream errors.

Unlike the `process` command, no conditional logic will be applied, the file will always be modified.

### Monitor

The `monitor` command will watch the specified folders for changes, and process the directories with changes.

Note that the [FileSystemWatcher](https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher) is not always reliable on Linux or NAS Samba shares.  
Also note that changes made directly to the underlying filesystem will not trigger when watching the SMB shares, e.g. when a Docker container writes to a mapped volume, the SMB view of that volume will not trigger.

### CreateSidecar, GetSidecar, GetTagMap, GetMediaInfo, GetBitrateInfo

The `createsidecar` command will create sidecar files.

The `getsidecar` command will print sidecar file attributes.

The `gettagmap` command will calculate and print attribute mappings between between different media information tools.

The `getmediainfo` command will print media attribute information.

The `getbitrateinfo` command will calculate and print media bitrate information.

## Tools and Utilities

Tools, libraries, and utilities used in the project.

### NuGet Component Dependencies

```console
PS C:\Users\piete\source\repos\PlexCleaner> dotnet list package
Project 'PlexCleaner' has the following package references
   [net5.0]:
   Top-level Package                  Requested             Resolved
   > HtmlAgilityPack                  1.11.28               1.11.28
   > InsaneGenius.Utilities           2.0.1                 2.0.1
   > Microsoft.SourceLink.GitHub      1.1.0-beta-20204-02   1.1.0-beta-20204-02
   > Newtonsoft.Json                  12.0.3                12.0.3
   > System.CommandLine               2.0.0-beta1.20574.7   2.0.0-beta1.20574.7
```

### 3rd Party Tools

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

### Sample Media Files

- [Kodi](https://kodi.wiki/view/Samples)
- [JellyFish](http://jell.yfish.us/)
- [DemoWorld](https://www.demo-world.eu/2d-demo-trailers-hd/)
- [MPlayer](https://samples.mplayerhq.hu/)
