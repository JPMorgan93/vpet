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
UninstallDisplayIcon={app}\assets\reference\Vpet-Pixel.ico
UninstallDisplayName=Vpet
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
AppMutex=Local\VpetPrototype
CloseApplications=no
RestartApplications=no
DisableProgramGroupPage=yes
DisableReadyPage=yes
UsePreviousTasks=yes
InfoBeforeFile=Getting Started.txt

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "..\bin\release\Vpet.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\release\assets\reference\Base Vpet Sprite Sheet.png"; DestDir: "{app}\assets\reference"; Flags: ignoreversion
Source: "..\bin\release\assets\reference\Heads.png"; DestDir: "{app}\assets\reference"; Flags: ignoreversion
Source: "..\bin\release\assets\reference\Tails.png"; DestDir: "{app}\assets\reference"; Flags: ignoreversion
Source: "..\bin\release\assets\reference\Joystick.png"; DestDir: "{app}\assets\reference"; Flags: ignoreversion
Source: "..\bin\release\assets\reference\Easy.mp3"; DestDir: "{app}\assets\reference"; Flags: ignoreversion
Source: "..\bin\release\assets\reference\Normal.mp3"; DestDir: "{app}\assets\reference"; Flags: ignoreversion
Source: "..\bin\release\assets\reference\Hard.mp3"; DestDir: "{app}\assets\reference"; Flags: ignoreversion
Source: "..\bin\release\assets\reference\Vpet.ico"; DestDir: "{app}\assets\reference"; Flags: ignoreversion
Source: "..\bin\release\assets\reference\Vpet.ico"; DestDir: "{app}\assets\reference"; DestName: "Vpet-Pixel.ico"; Flags: ignoreversion
Source: "Getting Started.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Vpet"; Filename: "{app}\Vpet.exe"; WorkingDir: "{app}"; IconFilename: "{app}\assets\reference\Vpet-Pixel.ico"
Name: "{autodesktop}\Vpet"; Filename: "{app}\Vpet.exe"; WorkingDir: "{app}"; IconFilename: "{app}\assets\reference\Vpet-Pixel.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\Vpet.exe"; Description: "Launch Vpet"; Flags: nowait postinstall skipifsilent; Check: not ExistingInstallation
Filename: "{app}\Vpet.exe"; Parameters: "--startup"; Flags: nowait skipifsilent; Check: ExistingInstallation

[UninstallDelete]
Type: files; Name: "{app}\pending-update.txt"
Type: files; Name: "{app}\pending-update-from.txt"
Type: files; Name: "{app}\last-run-version.txt"

[Code]
var
  IsUpgrade: Boolean;
  PreviousVersion: String;

function ExistingInstallation(): Boolean;
begin
  Result := IsUpgrade;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if IsUpgrade and (CurStep = ssPostInstall) then
  begin
    SaveStringToFile(ExpandConstant('{app}\pending-update-from.txt'), PreviousVersion, False);
    SaveStringToFile(ExpandConstant('{app}\pending-update.txt'), '{#AppVersion}', False);
  end;
end;

procedure InitializeWizard();
var
  ExistingDirectory: String;
  InstalledVersion: String;
  LastRun, PendingVersion, PendingFrom: AnsiString;
begin
  IsUpgrade := RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{25F79B0F-454A-4E2F-BE0C-C13D6F067F65}_is1', 'InstallLocation', ExistingDirectory);
  if IsUpgrade then IsUpgrade := FileExists(AddBackslash(ExistingDirectory) + 'Vpet.exe');
  if IsUpgrade then
  begin
    { Capture before replacing the EXE. Older apps have no last-run file, so
      fall back to their installed version. Preserve unseen upgrade history. }
    GetVersionNumbersString(AddBackslash(ExistingDirectory) + 'Vpet.exe', InstalledVersion);
    PreviousVersion := InstalledVersion;
    if LoadStringFromFile(AddBackslash(ExistingDirectory) + 'pending-update.txt', PendingVersion) and
       LoadStringFromFile(AddBackslash(ExistingDirectory) + 'pending-update-from.txt', PendingFrom) then
      if (Trim(PendingVersion) + '.0' = InstalledVersion) and (Trim(PendingFrom) <> '') then
        PreviousVersion := Trim(PendingFrom);
    if LoadStringFromFile(AddBackslash(ExistingDirectory) + 'last-run-version.txt', LastRun) then
      if Trim(LastRun) <> '' then PreviousVersion := Trim(LastRun);
    WizardForm.Caption := 'Updating Vpet';
    WizardForm.FinishedHeadingLabel.Caption := 'Vpet update complete';
    WizardForm.FinishedLabel.Caption := 'Vpet {#AppVersion} is up to date. Your settings, artwork, and shortcut choices have been kept.';
  end;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  { Inno always shows installation progress and completion. Older updaters
    that launch interactively skip all optional setup pages on an upgrade. }
  Result := IsUpgrade and (PageID <> wpFinished);
end;

procedure CurPageChanged(PageID: Integer);
begin
  if IsUpgrade and (PageID = wpReady) then
    { Inno can retain Ready when every preceding page is skipped. }
    PostMessage(WizardForm.NextButton.Handle, $00F5, 0, 0);
  if IsUpgrade and (PageID = wpFinished) then
  begin
    WizardForm.FinishedHeadingLabel.Caption := 'Vpet update complete';
    WizardForm.FinishedLabel.Caption := 'Vpet {#AppVersion} is up to date. Your settings, artwork, and shortcut choices have been kept.';
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Command: String;
begin
  if CurUninstallStep = usUninstall then
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'Vpet', Command) then
      if CompareText(Command, '"' + ExpandConstant('{app}\Vpet.exe') + '" --startup') = 0 then
        RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'Vpet');
end;

function InitializeSetup(): Boolean;
var
  Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release);
  if Result then Result := Release >= 528040;
  if not Result then
    MsgBox('Vpet requires Microsoft .NET Framework 4.8 or later. Install it from https://dotnet.microsoft.com/download/dotnet-framework/net48 and run this installer again.', mbError, MB_OK);
end;
