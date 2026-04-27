[Setup]
AppId={{4CE3F0CE-3958-4CF4-91A7-12074F3DB3F0}
AppName=NoteForge
AppVersion=1.0.1
VersionInfoVersion=1.0.1.0
AppPublisher=SeolJinn
AppPublisherURL=https://github.com/SeolJinn/NoteForge
DefaultDirName={autopf}\NoteForge
DefaultGroupName=NoteForge
UninstallDisplayIcon={app}\NoteForge.exe
OutputDir=dist
OutputBaseFilename=NoteForge-Setup-1.0.1
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
SetupIconFile=NoteForge\Assets\app.ico
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Files]
Source: "NoteForge\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion
Source: "redist\WindowsAppRuntimeInstall-x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{group}\NoteForge"; Filename: "{app}\NoteForge.exe"
Name: "{group}\Uninstall NoteForge"; Filename: "{uninstallexe}"
Name: "{autodesktop}\NoteForge"; Filename: "{app}\NoteForge.exe"; Tasks: desktopicon

[Run]
Filename: "{tmp}\WindowsAppRuntimeInstall-x64.exe"; Parameters: "--quiet"; StatusMsg: "Installing Windows App Runtime (required by NoteForge)..."; Flags: waituntilterminated
Filename: "{app}\NoteForge.exe"; Description: "Launch NoteForge"; Flags: nowait postinstall skipifsilent
