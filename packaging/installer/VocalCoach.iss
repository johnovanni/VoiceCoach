; Inno Setup script for the Vocal Coach installer (VocalCoach-Setup.exe).
; Built by .github/workflows/release.yml:  iscc /DAppVersion=1.2.3 /DPublishDir=<publish folder> /DOutputDir=<out> VocalCoach.iss
; Installs to C:\Program Files\Qapow\VocalCoach by default (every QAPOW program goes under C:\Program Files\Qapow),
; and the user can pick another folder. The app keeps its settings and data in the user's profile, not here.

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\..\publish"
#endif
#ifndef OutputDir
  #define OutputDir "..\..\out"
#endif

#define AppName "Vocal Coach"
#define AppFolder "VocalCoach"
#define AppExe "VocalCoach.App.exe"

[Setup]
; AppId identifies this program to Windows across versions. Never change it.
AppId={{635A824B-13F8-5897-8587-658C2B122D7D}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=John Apolloni
AppPublisherURL=https://qapow.com/p/vocal-coach/
AppSupportURL=https://github.com/johnovanni/VoiceCoach
VersionInfoVersion={#AppVersion}
DefaultDirName={autopf}\Qapow\{#AppFolder}
DisableDirPage=no
DefaultGroupName=Qapow
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes
CloseApplications=yes
OutputDir={#OutputDir}
OutputBaseFilename=VocalCoach-Setup

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Qapow\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent
