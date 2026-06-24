[Setup]
AppName=YoutubeMusic
AppVersion=1.0
DefaultDirName={autopf}\YoutubeMusic
DefaultGroupName=YoutubeMusic
OutputDir=bin\Installers
OutputBaseFilename=Setup_YoutubeMusic
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
DisableProgramGroupPage=yes

[Files]
Source: "bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\YoutubeMusic"; Filename: "{app}\YoutubeMusic.exe"
Name: "{autodesktop}\YoutubeMusic"; Filename: "{app}\YoutubeMusic.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Run]
Filename: "{app}\YoutubeMusic.exe"; Description: "{cm:LaunchProgram,YoutubeMusic}"; Flags: nowait postinstall skipifsilent
