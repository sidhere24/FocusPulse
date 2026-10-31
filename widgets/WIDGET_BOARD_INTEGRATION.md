# Windows 11 Native Widget Board Integration (Win + W)

This guide documents how to package and host **FocusPulse** inside the official **Windows 11 Widget Board** (accessible via `Win + W` or the taskbar widgets icon).

---

## 1. Architecture Overview

Windows 11 Widgets Board uses **Adaptive Cards 1.5** templates rendered by the Windows OS shell (`widgets.exe` / Windows Web Experience Pack).

The widget provider lifecycle:
```
┌─────────────────────────────────┐
│ Windows 11 Widget Board (Win+W) │
└──────────────┬──────────────────┘
               │ 1. Request Widget Template & Data
               ▼
┌─────────────────────────────────┐
│ FocusPulse Widget Provider      │ ◄── Implements IWidgetProvider
└──────────────┬──────────────────┘
               │ 2. Queries FocusPulse Activity Engine
               ▼
┌─────────────────────────────────┐
│ FocusPulse Native Engine        │
│ (%LOCALAPPDATA%\FocusPulse)     │
└─────────────────────────────────┘
```

---

## 2. Windows App SDK Widget Provider Interface

To register as a native Windows 11 Widget provider, an application implements the `IWidgetProvider` COM interface provided by the Windows App SDK:

```csharp
using Microsoft.Windows.Widgets.Providers;

namespace FocusPulse
{
    public class FocusPulseWidgetProvider : IWidgetProvider
    {
        public void CreateWidget(WidgetContext widgetContext)
        {
            UpdateWidget(widgetContext.Id);
        }

        public void Activate(WidgetContext widgetContext)
        {
            UpdateWidget(widgetContext.Id);
        }

        public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
        {
            if (actionInvokedArgs.Verb == "refreshStats")
            {
                UpdateWidget(actionInvokedArgs.WidgetContext.Id);
            }
        }

        private void UpdateWidget(string widgetId)
        {
            string cardTemplate = File.ReadAllText("adaptive-card-widget.json");
            string cardData = GetLiveJsonData();

            WidgetUpdateRequestOptions options = new WidgetUpdateRequestOptions(widgetId)
            {
                Template = cardTemplate,
                Data = cardData
            };

            WidgetManager.GetDefault().UpdateWidget(options);
        }
    }
}
```

---

## 3. Package Manifest Registration (`Package.appxmanifest`)

To make Windows 11 detect your widget, the MSIX / AppX manifest declares the widget extension:

```xml
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:uap3="http://schemas.microsoft.com/appx/manifest/uap/windows10/3"
  xmlns:com="http://schemas.microsoft.com/appx/manifest/com/windows10">

  <Applications>
    <Application Id="App" Executable="FocusPulse-x64.exe" EntryPoint="Windows.FullTrustApplication">
      <Extensions>
        <!-- Windows 11 Widget Provider Extension -->
        <uap3:Extension Category="windows.appExtension">
          <uap3:AppExtension Name="com.microsoft.windows.widgets"
                             Id="FocusPulseWidget"
                             DisplayName="FocusPulse Summary"
                             Description="Real-time flow score and active focus monitoring">
            <uap3:Properties>
              <WidgetProvider>
                <ProviderIcons>
                  <Icon Path="Assets\WidgetIcon.png" />
                </ProviderIcons>
                <Activation>
                  <CreateInstance ClassId="YOUR-GUID-HERE" />
                </Activation>
                <Definitions>
                  <Definition Id="FocusPulse_Summary"
                              DisplayName="FocusPulse Summary"
                              Description="Shows focused time and top applications"
                              AllowMultiple="false">
                    <Capabilities>
                      <Capability>
                        <Size>medium</Size>
                      </Capability>
                      <Capability>
                        <Size>large</Size>
                      </Capability>
                    </Capabilities>
                    <ThemeResources>
                      <Icons>
                        <Icon Path="Assets\WidgetIcon.png" />
                      </Icons>
                      <Screenshots>
                        <Screenshot Path="Assets\WidgetPreview.png" />
                      </Screenshots>
                    </ThemeResources>
                  </Definition>
                </Definitions>
              </WidgetProvider>
            </uap3:Properties>
          </uap3:AppExtension>
        </uap3:Extension>
      </Extensions>
    </Application>
  </Applications>
</Package>
```

---

## 4. Testing Adaptive Card Rendering

You can preview and test `adaptive-card-widget.json` with `widget-sample-data.json` at:
👉 **[Adaptive Cards Designer](https://adaptivecards.io/designer/)**

1. Set **Target version** to `1.5`.
2. Paste the contents of `adaptive-card-widget.json` into the **Card Payload Editor**.
3. Paste the contents of `widget-sample-data.json` into the **Sample Data Editor**.
4. The designer renders the complete glassmorphic card preview immediately.
