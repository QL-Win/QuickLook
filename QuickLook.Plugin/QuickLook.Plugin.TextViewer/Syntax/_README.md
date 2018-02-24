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
