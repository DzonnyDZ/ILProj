# ILProj
Are you crazy? Wanna write .NET applications and libraries in assembler? But aren't you crazy enough to compile from command line? This is project for you. CIL assembler suppor for Visual Studio.

This is sister project of JScript.NET and coevolution is expected.

## Capabilities
* Write EXEs and DLLs in CIL in Visual Studio
* Compile them
* Debug them
* Reference DLLs written in JavaScript form other .NET languages (Visual Basic, C#, C++/CLI, PHP, F#, JScript.NET)
* Reference DLLs written in other .NET languages from CIL

## Known limitations
* Many changes currently cannot be done via Visual Studio, you must unload the project and edit project file manualy
** E.g. to change project type from console application to DLL look for <OutputType> element and change its value to DLL.
* Not all ILASM options are supported
* Impossible to reference ILASM project from C#. As a workaround use DLL reference instead.
* No editor support (colorization, intelli sense)
* Impossible to add new files to project via right-click | Add | New item
** Copy-paste the default item instead, or create the item outside of Visual Studio and then include it in a project.

Install from VS gallery https://visualstudiogallery.msdn.microsoft.com/c931673f-3f9d-4bc9-98e9-c9c1f65f929c or directly from VS.

This is sister project of JScript.NET  https://github.com/DzonnyDZ/JScript.NET