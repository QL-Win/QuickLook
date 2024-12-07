LAV Filters - ffmpeg based DirectShow Splitter and Decoders

LAV Filters are a set of DirectShow filters based on the libavformat and libavcodec libraries
from the ffmpeg project, which will allow you to play virtually any format in a DirectShow player.

The filters are still under development, so not every feature is finished, or every format supported.

Install
=============================
- Unpack
- Register (install_*.bat files)
	Registering requires administrative rights, and an elevated shell ("Run as Administrator")

Using it
=============================
By default, the splitter will register for all media formats that have been
tested and found working at least partially.
This currently includes (but is not limited to)
	MKV/WebM, AVI, MP4/MOV, TS/M2TS/MPG, FLV, OGG, BluRay (.bdmv and .mpls)

However, some other splitters register in a "bad" way and force all players
to use them. The Haali Media Splitter is one of those, and to give priority
to the LAVFSplitter you have to either uninstall Haali or rename its .ax file
at least temporarily.

The Audio and Video Decoder will register with relatively high merit, which should make
it the preferred decoder by default. Most players offer a way to choose the preferred
decoder however.

Automatic Stream Selection
=============================
LAV Splitter offers different ways to pre-select streams when opening a file.
The selection of video streams is not configurable, and LAV Splitter will quite simply
pick the one with the best quality.

Audio stream selection offers some flexibility, specifically you can configure your preferred languages.
The language configuration is straightforward. Just enter a list of 3-letter language codes (ISO 639-2),
separated by comma or space.
For example: "eng ger fre". This would try to select a stream matching one of these languages,
in the order you specified them. First, check if an English track is present, and only if not,
go to German, and after that, go to French.

If multiple audio tracks match one language, the choice is based on the quality. The primary attribute here
is the number of channels, and after that is the codec used. PCM and lossless codecs have a higher priority
than lossy codecs.

Subtitle selection offers the most flexibility.
There are 4 distinct modes of subtitle selection.

"No Subtitles"
This mode is simple, by default subtitles will be off.

"Only Forced Subtitles"
This mode will only pre-select subtitles flagged with the "forced" flag. It'll also obey the language preferences, of course.

"Default"
The default mode will select subtitles matching your language preference. If there is no match, or you didn't configure
languages, no subtitles will be activated. In addition, subtitles flagged "default" or "forced" will always be used.

"Advanced"
The advanced mode lets you write your own combinations of rules with a special syntax. It also allows selecting subtitles
based on the audio language of the file.

The base syntax is simple, it always requires a pair of audio and subtitle language, separated by a colon, for example: "eng:ger"
In this example, LAV Splitter would select German subtitles if English audio was found.

Instead of language codes, the advanced mode supports two special cases: "*" and "off".
When you specify "*" for a language code, it'll match everything. For example "*:eng"  will activate English subtitles, independent
of the audio language. The reverse is also possible: "eng:*" will activate any subtitles when the audio is English.

The "off" flag is only valid for the subtitle language, and it instructs LAV Splitter to turn the subtitles off.
So "eng:off" means that when the audio is English, the subtitles will be deactivated.

Additionally to the syntax above, the following flags can be appended to the subtitle token separated by a pipe symbol ("|"):
 - "d" for default subtitles
 - "f" for forced subtitles
 - "h" for hearing impaired
 - "n" for normal streams (not default, forced, or impaired).
In addition, you can also check for the absence of flags by preceding the flags with a "!".
The advanced rules can be combined into a complete logic for subtitle selection by just appending them, separated with a comma or a space.
The rules will always be parsed from left to right, the first match taking precedence.

Finally, the rules can match the name of a stream, with some limitations. Only single words can be matched, as spaces are a separator for the next token.
A text match can be added to the end of the token with an @ sign.

Example: (basic flag usage)
  "*:*|f"
Explanation:
  On any audio language, load any subtitles that are flagged forced.

Example: (basic ruleset)
  "eng:eng|f eng:ger|f eng:off *:eng *:ger"
Explanation:
  If the audio is English, load an English or a German forced subtitle track, otherwise, turn subtitles off.
  If the audio is not English, load English or German subtitles.

Example: (flag usage with negation)
  "jpn:ger|d!f"
Explanation:
  In the Japanese language, load German subtitles that have the default-flag but not together with forced-flag.
  This is useful when you have files where the default and forced flags are set together.

Example: (advanced ruleset for files with multiple audio and subtitle-tracks)
  "jpn:ger|d!f  jpn:ger|!f  jpn:ger  ger:ger|f  ger:eng|f  ger:*|f"
Explanation:
  On Japanese audio, try to load German full subs (default but not forced), then unforced, and at last any german subs if there are no unforced subs.
  On German audio load only forced subs in the following order: German, English, any.

Example: (text match)
  "*:eng@Forced"
Explanation:
  On any audio, select english subtitle streams with "Forced" in the stream title.

BluRay Support
=============================
To play a BluRay, simply open the index.bdmv file in the BDMV folder on the BluRay disc.
LAV Splitter will then automatically detect the longest track on the disc (usually the main movie),
and start playing.
Alternatively, you can also open a playlist file (*.mpls, located in BDMV/PLAYLIST), and LAV Splitter
will then play that specific title.

In future versions, you'll be able to choose the title from within the player, as well.

Compiling
=============================
Compiling is pretty straightforward using VS2019 (included project files).
Older versions of Visual Studio are not officially supported, but may still work.

It does, however, require that you build your own ffmpeg and libbluray.
You need to place the full ffmpeg package in a directory called "ffmpeg" in the
main source directory (the directory this file was in). There are scripts to
build a proper ffmpeg included.

I recommend using my fork of ffmpeg, as it includes additional patches for
media compatibility:
https://gitea.1f0.de/LAV/FFmpeg

libbluray is compiled with the MSVC project files, however, a specially modified
version of libbluray is required. Similar to ffmpeg, just place the full tree
inside the "libbluray" directory in the main directory.

You can get the modified version here:
https://gitea.1f0.de/LAV/libbluray

Feedback
=============================
GitHub Project: https://github.com/Nevcairiel/LAVFilters
Doom9: https://forum.doom9.org/showthread.php?t=156191
