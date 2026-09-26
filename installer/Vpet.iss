#define AppVersion GetStringFileInfo("..\bin\release\Vpet.exe", "ProductVersion")

[Setup]
AppId={{25F79B0F-454A-4E2F-BE0C-C13D6F067F65}
AppName=Vpet
AppVersion={#AppVersion}
AppVerName=Vpet {#AppVersion}
VersionInfoVersion={#AppVersion}.0
VersionInfoProductName=Vpet
VersionInfoProductVersion={#AppVersion}
DefaultDirName={localappdata}\Programs\Vpet
DefaultGroupName=Vpet
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.18362
OutputDir=..\dist
OutputBaseFilename=Vpet-Setup-{#AppVersion}-Windows-x64
SetupIconFile=..\assets\reference\Vpet.ico
UninstallDisplayIcon={app}\Vpet.exe
UninstallDisplayName=Vpet
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
AppMutex=Local\VpetPrototype
CloseApplications=no
RestartApplications=no
DisableProgramGroupPage=yes
InfoBeforeFile=Getting Started.txt

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "..\bin\release\Vpet.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\release\assets\reference\Base Vpet Sprite Sheet.png"; DestDir: "{app}\assets\reference"; Flags: ignoreversion
Source: "..\bin\release\assets\reference\Vpet.ico"; DestDir: "{app}\assets\reference"; Flags: ignoreversion
Source: "Getting Started.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Vpet"; Filename: "{app}\Vpet.exe"; WorkingDir: "{app}"; IconFilename: "{app}\assets\reference\Vpet.ico"
Name: "{autodesktop}\Vpet"; Filename: "{app}\Vpet.exe"; WorkingDir: "{app}"; IconFilename: "{app}\assets\reference\Vpet.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\Vpet.exe"; Description: "Launch Vpet"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
var
  Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release);
  if Result then Result := Release >= 528040;
  if not Result then
    MsgBox('Vpet requires Microsoft .NET Framework 4.8 or later. Install it from https://dotnet.microsoft.com/download/dotnet-framework/net48 and run this installer again.', mbError, MB_OK);
end;
