LAV Filters - ffmpeg based DirectShow Splitter and Decoders

LAV Filters are a set of DirectShow filters based on the libavformat and libavcodec libraries
from the ffmpeg project, which will allow you to play virtually any format in a DirectShow player.

The filters are still under development, so not every feature is finished, or every format supported.

Install
=============================
- Unpack
- Register (install_*.bat files)
	Registering requires administrative rights.
	On Vista/7 also make sure to start it in an elevated shell.

Using it
=============================
By default the splitter will register for all media formats that have been
tested and found working at least partially.
This currently includes (but is not limited to)
	MKV/WebM, AVI, MP4/MOV, TS/M2TS/MPG, FLV, OGG, BluRay (.bdmv and .mpls)

However, some other splitters register in a "bad" way and force all players
to use them. The Haali Media Splitter is one of those, and to give priority
to the LAVFSplitter you have to either uninstall Haali or rename its .ax file
at least temporarily.

The Audio and Video Decoder will register with a relatively high merit, which should make
it the preferred decoder by default. Most players offer a way to choose the preferred
decoder however.

Automatic Stream Selection
=============================
LAV Splitter offers different ways to pre-select streams when opening a file.
The selection of video streams is not configurable, and LAV Splitter will quite simply
pick the one with the best quality.

Audio stream selection offers some flexibility, specifically you can configure your preferred languages.
The language configuration is straight forward. Just enter a list of 3-letter language codes (ISO 639-2),
separated by comma or space.
For example: "eng ger fre". This would try to select a stream matching one of these languages,
in the order you specified them. First check if an English track is present, and only if not,
go to German, and after that, go to French.

If multiple audio tracks match one language, the choice is based on the quality. The primary attribute here
is the number of channels, and after that the codec used. PCM and lossless codecs have a higher priority
then lossy codecs.

Subtitle selection offers the most flexibility.
There is 4 distinct modes of subtitle selection.

"No Subtitles"
This mode is simple, by default subtitles will be off.

"Only Forced Subtitles"
This mode will only pre-select subtitles flagged with the "forced" flag. It'll also obey the language preferences, of course.

"Default"
The default mode will select subtitles matching your language preference. If there is no match, or you didn't configure
languages, no subtitles will be activated. In addion, subtitles flagged "default" or "forced" will always be used.

"Advanced"
The advanced mode lets you write your own combinations of rules with a special syntax. It also allows selecting subtitles
based on the audio language of the file.

The base syntax is simple, it always requires a pair of audio and subtitle language, separated by a colon, for example: "eng:ger"
In this example, LAV Splitter would select German subtitles if English audio was found.

Instead of language codes, the advanced mode supports two special cases: "*" and "off".
When you specify "*" for a language code, it'll match everything. For example "*:eng"  will activate English subtitles, independent
of the audio language. The reverse is also possible: "eng:*" will activate any subtitles when the audio is english.

The "off" flag is only valid for the subtitle language, and it instructs LAV Splitter to turn the subtitles off.
So "eng:off" means that when the audio is english, the subtitles will be deactivated.

Additionally to the syntax above, two flags are supported to enhance the subtitle selection.
Specifically, LAV Splitter understands the flag "d" for default subtitles, the flag "f" for forced subtitles,
the flag "h" for hearing impaired, and the flag "n" for normal streams (not default, forced, or impaired).
In addition, flags can be negated with a leading "!" before the whole flags block - "!h" becomes "dfn", etc.
Flags are appended to the subtitle language, separated by a pipe symbol ("|"). Example: "*:*|f"
This token specifys that on any audio language, you want any subtitle that is flagged forced.

The advanced rukes can be combined into a complete logic for subtitle selection by just appending them, separated with a comma or a space.
The rules will always be parsed from left to right, the first match taking precedence.

Consider the following rule set:
"eng:eng|f eng:ger|f eng:off *:eng *:ger"
This rule means the following:
If audio is english, load an english or a german forced subtitle track, otherwise turn subtitles off.
If audio is not english, load english or german subtitles.

BluRay Support
=============================
To play a BluRay, simply open the index.bdmv file in the BDMV folder on the BluRay disc.
LAV Splitter will then automatically detect the longest track on the disc (usually the main movie),
and start playing.
Alternatively, you can also open a playlist file (*.mpls, located in BDMV/PLAYLIST), and LAV Splitter
will then play that specific title.

In future versions you'll be able to choose the title from within the player, as well.

Compiling
=============================
Compiling is pretty straight forward using VC++2015 U1 (included project files).
Older versions of Visual Studio are not supported.

It does, however, require that you build your own ffmpeg and libbluray.
You need to place the full ffmpeg package in a directory called "ffmpeg" in the 
main source directory (the directory this file was in). There are scripts to 
build a proper ffmpeg included.

I recommend using my fork of ffmpeg, as it includes additional patches for 
media compatibility:
http://git.1f0.de/gitweb?p=ffmpeg.git;a=summary

libbluray is compiled with the MSVC project files, however a specially modified
version of libbluray is required. Similar to ffmpeg, just place the full tree
inside the "libbluray" directory in the main directory.

You can get the modified version here:
http://git.1f0.de/gitweb?p=libbluray.git;a=summary

Feedback
=============================
GitHub Project: https://github.com/Nevcairiel/LAVFilters
Doom9: http://forum.doom9.org/showthread.php?t=156191
You can, additionally, reach me on IRC in the MPC-HC channel on freenode (#mpc-hc)
