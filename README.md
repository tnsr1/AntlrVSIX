# AntlrVSIX

AntlrVSIX is an extension for Visual Studio 2019 and Visual Studio Code to support editing and refactoring of Antlr v4 grammars.
It is implemented using Microsoft's [Language Server Protocol (LSP)](https://langserver.org/) 
[client](https://www.nuget.org/packages/Microsoft.VisualStudio.LanguageServer.Client/) and
[server](https://www.nuget.org/packages/Microsoft.VisualStudio.LanguageServer.Protocol/) APIs,
[Antlr](https://www.antlr.org/), [Antlr4BuildTasks](https://github.com/kaby76/Antlr4BuildTasks),
and a number of other tools.
Most of the extension is implemented in C#, while the client for VS Code is in Typescript. 
Supported are colorized tagging, hover, go to def, find all refs,
replace, command completion, reformat, and go to visitor/listener.

Instead of providing railroad diagrams and ATN graphs,
this extension focuses on features useful for maintaining grammars,
i.e., refactoring grammars in order to make them cleaner, more readable, and more efficient.

The source code for the extension is open source, free of charge, and free of ads. For the latest developments on the extension,
check out my [blog](http://codinggorilla.com).

# Installation of Prerequisites

* Install Java tool chain, either [OpenJDK](https://openjdk.java.net/) or [Oracle JDK SE](https://www.oracle.com/technetwork/java/javase/downloads/index.html).

* Downloaded the Java-based Antlr tool chain. [Complete ANTLR 4.8 Java binaries jar](https://www.antlr.org/download/antlr-4.8-complete.jar).

* Set the environment variable "JAVA_HOME" to the directory of the java installation. See [this](https://confluence.atlassian.com/doc/setting-the-java_home-variable-in-windows-8895.html) for some instructions on how to do this
on Windows.

* Set the environment variable "Antlr4ToolPath" to the path of the downloaded Antlr jar file.

* Do not include the generated .cs Antlr parser files in the CSPROJ file for your program. The generated parser code is placed in the build temp output directory and automatically included.

* Make sure you do not have a version skew between the Java Antlr tool and the runtime versions.

# Verify Prerequisites

Please verify that you have these variables set up as expected. Try
*"$JAVA_HOME/bin/java.exe" -jar "$Antlr4ToolPath"*
from a Git Bash or
*"%JAVA_HOME%\bin\java.exe" -jar "%Antlr4ToolPath%"*
from a Cmd.exe.
That should execute the Antlr tool and print out the options expected
for the command. If it doesn't
work, adjust JAVA_HOME and Antlr4ToolPath. JAVA_HOME should be the full
path of the JDK; Antlr4ToolPath should be the full path of the Antlr
tool jar file. If you look at the generated .csproj file for the Antlr
Console program generated, you should see what it defaults if they
aren't set.

# Documentation

For information on how to use AntlrVSIX, see the [User Guide](doc/readme.md), which is geared specifically for Antlr programs. _Note--all this needs updating due to the new slimmed-down UI, which is now implemented using the Visual Studio LSP Client code._

# Caveats:

* I am in the process of reviewing the collection of Java grammars in
[Antlr's grammar github repository](https://github.com/antlr/grammars-v4). Currently,
Antlrvsix uses the slower [Java9 grammar](https://github.com/antlr/grammars-v4/blob/master/java9/Java9.g4).
The faster, "official" Java grammar is [here](https://github.com/antlr/grammars-v4/tree/master/java).

* Support for VS2017 and older editions has been removed.
If you are interested in those, you can try using an older version of the extension.

* The grammar used is the standard Antlr4 grammar in the examples: 
https://github.com/antlr/grammars-v4/tree/master/antlr4.

* If you want to make modifications for yourself, you should [reset your
Experimental Hive for Visual Studio](https://docs.microsoft.com/en-us/visualstudio/extensibility/the-experimental-instance?view=vs-2017). To do that,
Microsoft recommends using CreateExpInstance.exe.
Unfortunately, I've found CreateExpInstance doesn't always work because it copies from
previous hives stored under the AppData directory. It is often easier to
just recursively delete all directories ...\AppData\Local\Microsoft\VisualStudio\16.0_*.

* Use Visual Studio 2019 to build the extension. Note, the extension builds in two steps: (1) open VS on
the solution and perform a build, then quit. (2) reopen the solution in VS and perform a build. VS computes
the objects to pack into the .vsix file *at the time it opens the project*, not at build time (a really
bad thing MS). If your debug version of the
.vssix is 5M in size, it did not compute that the server executable needs to be added into the .vsix. Quit
VS, then reopen. Do not do "rebuild", only "build".

## Work in progress for v6.x:

* Add in Piggy for grammar rewriting and checking. The addition of Piggy will allow for rules to transform the
grammar e.g., warning/removal of LHS predicates in lexer, replacing factored grammar rules with Kleene star rules (which
result in faster parsers).

## Work in progress for v5.5:

* Add in more refactorings:
a) Sort rules alphabetically, DFS or BFS traversals.
b) Separate/combine grammars.
* Bug fixes.

## Release notes for v5.4:

* Add in a few refactoring transformations (remove useless parser productions, convert parser string literals to
lexer token symbols, move start rule to top of grammar). _Note: Lexer rules are prioritized, so transformations on these
types may not be totally correct at the moment._
* Fix goto visitor/listener.
* Add stability fixes.

## Release notes for v5.3:

* Fixed "Find references" of grammar symbols when opening only lexer grammar file.
* Fixed Format Document.
* Fixed Go To Listener/Visitor. _Caveat: only works for C# and you must save solution before using._
* Templates updated, Antlr4BuildTasks 2.2.

## Release notes for v5.2:

* Re-added About Box, Options Box, and next/previous grammar symbol.
* Options are now contained in ~/.antlrvsixrc, a JSON file.

## Release notes for v5.1:

* Re-added colorized tagging of grammar. When I switch to the LSP implementation, this functionality was lost because it's not
directly supported by the LSP Client/Server API that Microsoft provides. Microsoft says to implement this functionality using
TextMate files, but that is not how it should be done--it duplicates the purpose of the LSP server, and it's hard to
get right. Instead, it's implemented with a Visual Studio ITagger<> and a custom message to the LSP server.

## Release notes for v5.0:

* Restructuring the code as a Language Server Protocol client/server implementation with extensions for VS 2019 (IDE) and VS Code.

* Templates for C# and C++. Note, Antlr 4.8 currently does not have a C++ pre-built binary release for Windows. You will need to build the
runtime and update the generated .vcxproj file with path information. The template will generate a project that is expecting Debug Static and x64 target.

* These are the LSP features currently implemented. Note,
[Midrosoft.VisualStudio.LanguageServer.Protocol](https://www.nuget.org/packages/Microsoft.VisualStudio.LanguageServer.Protocol/)
version 16.4.30 does not implement LSP version 3.14, rather something around version 3.6. What is missing is color tagging.
I'm not sure how to deal with this other than pitch the inferior API, then use OmniSharp's API, or as usual write everything
myself.

* Computing the completion symbols is not complete. It only gives token types for completion, not the actual symbols that could
be inserted.

| Message  | Support |
| ---- | ---- |
| General | |
| initialize | yes (Dec 10, '19) |
| initiialized | yes (Dec 10, '19) |
| shutdown | yes (Dec 10, '19) |
| exit | yes (Dec 10, '19) |
| $/cancelRequest | no |
| ---- | ---- |
| Window | |
| window/showMessage | no |
| window/showMessageRequest | no |
| window/logMessage | no |
| ---- | ---- |
| Telemetry | |
| telemetry/event | no |
| ---- | ---- |
| Client | |
| client/registerCapability | no |
| client/unregisterCapability | no |
| ---- | ---- |
| Workspace | |
| workspace/workspaceFolders | no |
| workspace/didChangeWorkspaceFolders | no |
| workspace/didChangeConfiguration | no |
| workspace/configuration | no |
| workspace/didChangeWatchedFiles | no |
| workspace/symbol | no |
| workspace/executeCommand | no |
| workspace/applyEdit | no |
| ---- | ---- |
| Text Synchronization | |
| textDocument/didOpen | yes (Dec 10, '19) |
| textDocument/didChange | yes (Dec 15, '19) |
| textDocument/willSave | yes (Dec 18, '19) |
| textDocument/willSaveWaitUntil | yes (Dec 18, '19) |
| textDocument/didSave | yes (Dec 18, '19) |
| textDocument/didClose | yes (Dec 18, '19) |
| ---- | ---- |
| Diagnostics | |
| textDocument/publishDiagnostics | yes (Dec 10, '19) |
| ---- | ---- |
| Language Features | |
| textDocument/completion | no |
| completionItem/resolve | no |
| textDocument/hover | yes (Dec 10, '19) |
| textDocument/signatureHelp | no |
| textDocument/declaration | unavailable in API |
| textDocument/definition | yes (Dec 11, '19) |
| textDocument/typeDefinition | yes (same as "definition") |
| textDocument/implementation | yes (same as "definition") |
| textDocument/references | yes (Dec 11, '19) |
| textDocument/documentHighlight | yes (Dec 11, '19) |
| textDocument/documentSymbol | yes (Dec 10, '19) |
| textDocument/codeAction | no |
| textDocument/codeLens | no |
| codeLens/resolve | no |
| textDocument/documentLink | no |
| documentLink/resolve | no |
| textDocument/documentColor | unavailable in API |
| textDocument/colorPresentation | unavailable in API |
| textDocument/formatting | yes (Dec 17, '19) |
| textDocument/rangeFormatting | no |
| textDocument/onTypeFormatting | no |
| textDocument/rename | yes (Dec 18, '19) |
| textDocument/prepareRename | unavailable in API |
| textDocument/foldingRange | no |

## Release notes for v4.0.5:

* Fixing #26.

## Release notes for v4.0.4:

* Fixing solution loading crash.
* Removing parse tree print code from Python--it was used for debugging and should not have been there.
* Stubbing out Python and Rust targets because they could interfere with (currently) much better extensions. Will add them
back once they are up to snuff.

A shout out to all those folks using this extension and have opted-in to the automatic reporting!!

## Release notes for v4.0.3:

* Restructuring the code for options.
* Adding in reporting of caught exceptions to server.
* Some changes for the stability for Java soruce.

## Release notes for v4.0.2:

* Updated symbol table with new classes to represent files, directories, and search paths. This will help
support for better modeling of Java's ClassPath, Antlr's imports, etc., so that scopes can be cleared out quickly and
easily when the sources have changed.
* Fixing issues in stability with Antlr and Java files.
* Fixing https://github.com/kaby76/AntlrVSIX/issues/23
* Fixing tool tips and highlighting for Java.
* Updated symbol table to allow ambigous code editing.
* Changing About and Options boxes to non-modal.

## Release notes for v4.0.1:

* Fixing stability issues with Antlr and Java files.

## Release notes for v4.0:

* Major changes to architecture, focusing on separation of GUI from a backend that works like Language Server Protocol (LSP).
* In the GUI, tagging was improved in quality and speed.
* In the backend, a symbol table was added to represent all symbols in a project and solution.
* For Antlr grammars, imports and tokenVocab are now used to determine the scope of the symbol table.
* For Antlr grammars, Intellisense pop-ups now give the rule definition and location of the file defining the symbol.
* No specific improvements were made for Python, Rust, and Java support, in favor of focusing on the improved overall design and implementation of the extension.

## Release notes for v3.0:

* Supports Java, Python, Rust, Antlr in various stages. Description of languages abstracted into a "Grammar Description".

## Release notes for v2.0:

* The extension will support VS 2019 and VS 2017.

* A menu for the extension will be added to a submenu under Extensions. The functionality provided will
duplicate that in context menus.

* The source code build files will be updated and migrated to the most recent version
of .csproj format that is compatible with VS extensions. Unfortunately, updating to the latest
(version 16) is not possible.

* With my [Antlr4BuildTasks](https://www.nuget.org/packages/Antlr4BuildTasks/) NuGet package,
.g4 files can be automatically compiled to .cs input within
VS 2019 without having to manually run the Antlr4 Java tool on the command line.
Building of the extension itself will be upgraded to use the Antlr4BuildTasks package.

* Listener and Visitor classes are generated for a grammar with a right-click
menu operation. For Listeners, there are two methods associated with a nonterminal.
Depress the control key to select the Exit method, otherwise it will
select the Enter method.

* An options menu is provided to turn on incremental parsing. By default, incremental
* parsing is off because it is very slow.

## Release notes for v1.2.4:

* The extension is now both VS 2017 and 2015 compatible.

* The results windows of Antlr Find All References is now "Antlr Find Results".

## Release notes for v1.2.3:

* Color selection through VS Options/Environment/Fonts and Colors. Look for "Antlr ..." named items.

* Bug fixes with Context Menu entries for AntlrVSIX. AntlrVSIX commands are now only visible when cursor positioned at an Antlr symbol in the grammar. This fixes the segv's when selecting AntlrVSIX commands in non-Antlr files.

Any questions, email me at ken.domino <at> gmail.com
