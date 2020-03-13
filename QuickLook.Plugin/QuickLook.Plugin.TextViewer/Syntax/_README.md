Synopsis
========
This project is a compilation of lexers I had written for [SharpDevelop](https://github.com/icsharpcode)'s TextEditor component. I am indeed late to the party, as the component is no longer supported by SharpDevelop team; however, I spent a good chunk of time writing these lexers through the years and now I thought why not make them open source for everyone who's still using the component.

All lexers were created exclusively for ICSharpCode.TextEditor v3.2. They were not tested on any earlier or later releases.

Languages
---------
List of all supported languages, sorted alphabetically:

A            | B     | C            | D      | E      | F          | G       | H       | I       | J          | K       | L    | N       | O     | P          | R        | S        | T          | V           | X
-------------|-------|--------------|--------|--------|------------|---------|---------|---------|------------|---------|------|---------|-------|------------|----------|----------|------------|-------------|----
ActionScript | Batch | C#           | D      | Eiffel | F#         | Go      | Haskell | Icon    | Java       | KiXtart | Lean | Nemerle | Obj-C | ParaSail   | R        | Scala    | TCL        | Vala        | X10
Ada          | Boo   | C            | Dart   | Erlang | Falcon     | Groovy  | Haxe    | ILYC    | JavaScript | Kotlin  | Lisp | Nim     | OCaml | Pascal     | Registry | Scheme   | Thrift     | VB.NET      | XC
ANTLR        |       | C++          | Delphi |        | Fantom     | Gui4Cli | HTML    | INI/INF | JSON       |         | Lua  |         |       | PHP        | Resource | Solidity | TypeScript | VBScript    | XML
Assembly     |       | Ceylon       |        |        | Fortran95  |         |         | Io      | Julia      |         |      |         |       | Pike       | Rexx     | Spike    |            | Verilog     | Xtend
AutoHotkey   |       | ChucK        |        |        |            |         |         |         |            |         |      |         |       | PowerShell | Rust     | SQF      |            | VHDL        |
‌‌             |       | Clojure      |        |        |            |         |         |         |            |         |      |         |       | Prolog     |          | SQL      |            | VS Solution |
‌‌             |       | Cocoa        |        |        |            |         |         |         |            |         |      |         |       | PureScript |          | Swift    |            | Volt        |
‌‌             |       | CoffeeScript |        |        |            |         |         |         |            |         |      |         |       | Python     |          |          |            |             |
‌‌             |       | Cool         |        |        |            |         |         |         |            |         |      |         |       |            |          |          |            |             |
‌‌             |       | CSS          |        |        |            |         |         |         |            |         |      |         |       |            |          |          |            |             |
 
That makes 85 languages in total.

> **Notes**

> - **PHP**: Heredoc highlighting is not supported. The syntax parser simply can't do it.
> - **CSS**: Omitting the last semicolon in the block is not supported. Same reason as above.

Usage
-----
If you are on this page, you most likely already know how to activate the lexer and use it.<br/>For a complete noob, this is how it is done:

C#
```c#
using System.IO;
using ICSharpCode.TextEditor.Document;
```
```c#
// insert the directory path of the desired .xshd file
var synDir = Application.StartupPath + "\\Syntax";

// check if directory exists to prevent throwing an exception
if (Directory.Exists(synDir))
{
    // create new provider with the highlighting directory
    var fsmProvider = new FileSyntaxModeProvider(synDir);
    // attach to the text editor
    HighlightingManager.Manager.AddSyntaxModeFileProvider(fsmProvider);
    // activate the highlighting, use the name from the SyntaxDefinition node in the .xshd file
    TextEditorControl.SetHighlighting("YourHighlighting");
}
else { MessageBox.Show("\u0027" + synDir + "\u0027" + " doesn't exist"); }
```

VB.NET
```vb.net
Imports System.IO
Imports ICSharpCode.TextEditor.Document
```
```vb.net
' insert the directory path of the desired .xshd file
Dim synDir As String = Application.StartupPath & "\Syntax"
' syntax provider
Dim fsmProvider As FileSyntaxModeProvider

If Directory.Exists(synDir) Then
    ' create new provider with the highlighting directory
    fsmProvider = New FileSyntaxModeProvider(synDir)
    ' attach to the text editor
    HighlightingManager.Manager.AddSyntaxModeFileProvider(fsmProvider)
    ' activate the highlighting, use the name from the SyntaxDefinition node in the .xshd file
    TextEditorControl.SetHighlighting("YourHighlighting")
Else
    MessageBox.Show(ChrW(39) + synDir + ChrW(39) + "doesn't exist")
End If
```

For more information on XSHD files,  [see here](https://github.com/icsharpcode/SharpDevelop/wiki/Syntax-highlighting#attach-a-syntaxhighlighting-to-the-text-editor).

Contact Author
-------
[Email me your love letters](mailto:xviyy@aol.com)
<br>
[Tweet me maybe?](https://twitter.com/xviyy)

Legal
-----
This project is distributed under the [MIT License](https://opensource.org/licenses/MIT)


/******************************************************/


[![Build status](https://ci.appveyor.com/api/projects/status/s19eint5cqhjxh5h/branch/master?svg=true)](https://ci.appveyor.com/project/Dirkster99/avalonedithighlightingthemes/branch/master) [![Release](https://img.shields.io/github/release/Dirkster99/AvalonEditHighlightingThemes.svg)](https://github.com/Dirkster99/AvalonEditHighlightingThemes/releases/latest) [![NuGet](https://img.shields.io/nuget/dt/Dirkster.HL.svg)](http://nuget.org/packages/Dirkster.HL)

# AvalonEditHighlightingThemes
Implements a sample implementation for using Highlightings with different (Light/Dark) WPF themes

# ThemedHighlightingManager for AvalonEdit

This [AvalonEdit](https://github.com/icsharpcode/AvalonEdit) extension implements its own highlighting manager that extends the classic way of handling highlighting definitions (see also [Dirkster99/AvalonEdit-Samples](https://github.com/Dirkster99/AvalonEdit-Samples)).

The inital release contains 5 highlighting themes:
- Dark
- Light
- True Blue-Light
- True Blue-Dark
- VS 2019-Dark

but you can easily define more themes. Just create a pull request with the [XSHTD file](https://github.com/Dirkster99/AvalonEditHighlightingThemes/tree/master/source/HL/Resources/Themes) at this site.

The standard highlighting in AvalonEdit is dependent on the currently viewed type
of text (eg C# or SQL), but a highlighting definition designed for a **Light** WPF theme may look [ugly](https://github.com/Dirkster99/AvalonEditHighlightingThemes/wiki/Highlighting-without-a-Theme) if viewed with a **Dark**
WPF theme, and vice versa. This is why the **ThemedHighlightingManager** extension associates each highlighting definition
with:

- A WPF Theme (Light, Dark) and
- A type of text (C#, SQL etc)

This approach is very similar to the implementation in [Notepad++](https://github.com/notepad-plus-plus/notepad-plus-plus) except Notepad++ uses a plain [xml file](https://lonewolfonline.net/notepad-colour-schemes/) to configure a highlighting theme whereas the **ThemedHighlightingManager** uses an [XSHTD file](https://github.com/Dirkster99/AvalonEditHighlightingThemes/tree/master/source/HL/Resources/Themes) to do the same. But at the end of the day, its XML in both projects, and cloning a highlighting theme from Notepad++ is almost too easy (thats how similar both implementations are).

Assuming that an application already use a WPF theming/management library, such as:
- [MahApps.Metro](https://github.com/MahApps/MahApps.Metro),
- [MLib](https://github.com/Dirkster99/MLib), or
- [MUI](https://github.com/firstfloorsoftware/mui)

enables an applications author to switch highlighting definitions to a matching color palette whenever the user
switches a given WPF theme. See [AvalonEditHighlightingThemes](https://github.com/Dirkster99/AvalonEditHighlightingThemes)
and [Aehnlich](https://github.com/Dirkster99/Aehnlich) for detailed sample implementations.

# Themes
![](screenshots/Themes.png)

## True Blue Light Theme
![](screenshots/TrueBlue_Light.png)

## VS 2019 Dark
![](screenshots/VS2019_Dark.png)

## Dark Theme
![](screenshots/Dark.png)

## Light Theme
![](screenshots/Light.png)

## True Blue Dark Theme
![](screenshots/TrueBlue_Dark.png)

# Concept
## WPF Theme

A WPF theme is a way of styling and theming WPF controls. This is usually implemented in a seperate library, such as:
- [MahApps.Metro](https://github.com/MahApps/MahApps.Metro),
- [MLib](https://github.com/Dirkster99/MLib), or
- [MUI](https://github.com/firstfloorsoftware/mui)

and takes advantage of WPFs way of defining and using themes ('Dark', 'Light', 'True Blue'...) with XAML resources etc.

## Generic Highlighting Theme

A Generic highlighting theme is a classic collection of AvalonEdit V2 highlighting definitions
(collection of xshd files). In this project, there is only one such theme, the **'Light'** highlighting
theme. This theme is defined in a classic collection of xshd resource files at 
[HL.Resources.Light](https://github.com/Dirkster99/AvalonEditHighlightingThemes/tree/master/source/Apps/HL/Resources/Light).

## Derived Highlighting Theme

A derived highlighting theme is a highlighting theme that makes use of a
[Generic Highlighting Theme](#Generic-Highlighting-Theme) and overwrites
formattings defined in named colors by incorporating an additional xsh**t**d file.

This approach re-uses the highlighting patterns of the generic theme but applies
different colors and formattings to better support:

- different background colors of different WPF themes or
- different taste towards different color schemes by different users

This project has multiple derived highlighting themes

- 'Dark'
- 'True Blue'
- 'VS 2019 Dark'

which are based on the highlighting patterns of the 'Light' generic highlighting theme.

## Data Design - Extension with Themable Highlighting

![](screenshots/HighlightingManagerV2.png)

## Data Design - Classic Highlighting Manager V5.04

![](screenshots/ClassicHighlighting.png)

## Other AvalonEdit Demo Projects:

More demo projects may be listed at the [AvalonEdit's Wiki page](https://github.com/icsharpcode/AvalonEdit/wiki/Samples-and-Articles)