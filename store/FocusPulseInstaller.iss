; Script generated for FocusPulse Microsoft Store Win32 Submission
#define MyAppName "FocusPulse"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "FocusPulse Studio"
#define MyAppURL "https://github.com/FocusPulse/FocusPulse"
#define MyAppExeName "FocusPulse.exe"

[Setup]
AppId={{8B315F9D-5B1E-4E6D-A2DC-9B057C952044}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\bin
OutputBaseFilename=FocusPulse-Setup-x64
SetupIconFile=..\bin\FocusPulse.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\bin\FocusPulse-x64.exe"; DestDir: "{app}"; DestName: "FocusPulse.exe"; Flags: ignoreversion
Source: "..\bin\FocusPulse.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\FocusPulse.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\FocusPulse.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
