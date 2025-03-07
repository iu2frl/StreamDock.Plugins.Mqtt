# StreamDock.Plugins.Mqtt

![Ajazz AKP03 deck](./Images/5dc4c3f18ad285ed4e9a8ba0fd3b52e8.webp)

This is a fork from the original project which I used to reverse engineer how the StreamDock AKP03 plugins work.

I am working to get it up and running to send command via MQTT and WebSocket.

## Deprecation notice

Even if this code is a good starting point, I will concentrate my efforts on the Olliter version of this plugin: [https://github.com/iu2frl/StreamDock.Plugins.Olliter](https://github.com/iu2frl/StreamDock.Plugins.Olliter)

## Mqtt plugin

### Features

- Work in progress

### How to use it

1. Clone the repo
1. Compile the source code using Visual Studio (`.NET SDK` is required)
1. Copy the `it.iu2frl.streamdock.mqtt` folder to `%AppData%\HotSpot\StreamDock\plugins\` and restart the Ajazz software.
1. (optional) Create a new scene.
1. Drag and drop the different actions to the buttons/knobs.
1. Configure the action if needed

## How this works

Base plugin structure:

```text
- MainPluginFolder.sdPlugin/
-- bin/
-- images/
-- manifest.json
```

- Every pugin has a folder which has some speficic naming convention, for example `it.iu2frl.streamdock.mqtt.sdPlugin`. The `sdPlugin` at the end is always there, I'm guessing it is required.
- The `bin` folder contains the binaries that are executed when the plugin is loaded or the keys are pressed (the .NET build output in our case)
- The `images` folder contains the images used in the plugin. These are the images shown when dropping buttons to the keys.
- The `manifest.json` file contains the plugin metadata (see below).

### Manifest file

The manifest file is used by the Ajazz app to kwnow how the plugin works, for example:

```json
{
    "SDKVersion": 2,
    "Author": "DevAny",
    "CodePath": "bin/release/StreamDock.Plugins.Payload",
    "Description": "StreamDock plugin to send MQTT commands via WS.",
    "Name": "IU2FRL MQTT over WS plugin",
    "Icon": "images/IU2FRL",
    "URL": "https://www.iu2frl.it/",
    "Version": "1.0",
    "Software": {
        "MinimumVersion": "2.9"
    },
    "OS": [
        {
            "Platform": "windows",
            "MinimumVersion": "10"
        }
    ],
    "Category": "Mqtt",
    "CategoryIcon": "images/Mqtt",
    "Actions": [
        {
            "PropertyInspectorPath": "property-inspector.html",
            "Name": "Send MQTT Message",
            "Tooltip": "Sends a message to the MQTT broker when button is pressed",
            "Controllers": [
                "Keypad"
            ],
            "UUID": "it.iu2frl.streamdock.mqtt.mqttbutton",
            "Icon": "images/Mqtt",
            "SupportedInMultiActions": true
        },
        {
            "PropertyInspectorPath": "property-inspector.html",
            "Name": "Send MQTT Message",
            "Tooltip": "Sends a message to the MQTT broker when knob is rotated",
            "Controllers": [
                "Knob"
            ],
            "UUID": "it.iu2frl.streamdock.mqtt.mqttknob",
            "Icon": "images/Mqtt",
            "SupportedInMultiActions": true
        },
        {
            "PropertyInspectorPath": "property-inspector.html",
            "Name": "Keypad Debug",
            "Tooltip": "This function only prints to the log",
            "Controllers": [
                "Keypad"
            ],
            "UUID": "it.iu2frl.streamdock.keypaddebug",
            "Icon": "images/Mqtt",
            "SupportedInMultiActions": true
        },
        {
            "PropertyInspectorPath": "property-inspector.html",
            "Name": "Dial Debug",
            "Tooltip": "This function only prints to the log",
            "Controllers": [
                "Knob"
            ],
            "UUID": "it.iu2frl.streamdock.dialdebug",
            "Icon": "images/Mqtt",
            "SupportedInMultiActions": true
        }
    ]
}
```

As you can see, there's an `Actions` array which contains a mapping to the C# methods (see below.)

#### Manifest documentation

The full documentation can be found here: [https://staging-docs.elgato.com/streamdeck/sdk/references/manifest/](https://staging-docs.elgato.com/streamdeck/sdk/references/manifest/)

#### Actions Controllers

I've found two controllers so far:

- `Keypad`: which tells the plugin this action can be used on keys
- `Knob`: which tells the plugin this action can be applied to knobs

Then, for some reason I've also found:

- `Information`: unknown (not documented in the plugins doc)

#### Automatic generation

The script called `build_manifest.py` will read the plugin source code and build the manifest file automatically.

The plugin only builds the actions list, other customization should be done manually.

### C# code

#### Keypad

The base method implementation for an action (like a key press) is derived from the `KeyPadBase` from the StreamDeck package. I preferred to wrap this initializer to a custom class so I can create shorter code when defining actions.

```csharp
namespace Common
{
    public class BaseKeyItem : KeypadBase
    {
        public BaseKeyItem(ISDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType().Name}: Something to do as soon as the element is initialized");
        }

        public override void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType().Name}: KeyPressed called");
        }

        public override void KeyReleased(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType().Name}: KeyReleased called");
        }

        public override void OnTick()
        {
            // Not sure when this is called
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType().Name}: Something to do when the object is removed");
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType().Name}: ReceivedSettings called");
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType().Name}: ReceivedGlobalSettings called");
        }
    }
}
```

Once we creat that wrapper, for each action we need to define a NameSpace similar to this one:

*Please note*: The comments above the method name are used by the Python script (called `build_manifest.py`) to build the manifest file automatically at every build event of the .NET solution. They are not normally needed.

```csharp
namespace ToggleMox1
{
    // Name: MOX Receiver 1
    // Tooltip: Toggle receive/transmit RX 1
    // Controllers: Keypad
    [PluginActionId("it.iu2frl.streamdock.mqtt.togglemox1")]
    public class ToggleRx1(ISDConnection connection, InitialPayload payload) : Common.BaseKeyItem(connection, payload)
    {
        public override void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "KeyPressed called");
        }
    }
}
```

#### Knobs

The base method implementation for an action (like a dial rotation) is derived from the `EncoderBase` from the StreamDeck package. I preferred to wrap this initializer to a custom class so I can create shorter code when defining actions.

```csharp
public class BaseDialItem : EncoderBase
{
    public BaseDialMqttItem(ISDConnection connection, InitialPayload payload) : base(connection, payload)
    {
        Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType().Name}: Something to do as soon as the element is initialized");
    }

    public override void DialDown(DialPayload payload)
    {
        Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType().Name}: DialDown called");
    }

    public override void DialRotate(DialRotatePayload payload)
    {
        Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType().Name}: DialRotate called");
    }

    public override void DialUp(DialPayload payload)
    {
        Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType().Name}: DialUp called");
    }

    public override void Dispose()
    {
        Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType().Name}: Dispose called");
    }

    public override void OnTick()
    {
        // Not sure when this is called
    }

    public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
    {
        Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType().Name}: ReceivedGlobalSettings called");
    }

    public override void ReceivedSettings(ReceivedSettingsPayload payload)
    {
        Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType().Name}: ReceivedSettings called");
    }

    public override void TouchPress(TouchpadPressPayload payload)
    {
        Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType().Name}: TouchPress called");
    }
}
```

Once we creat that wrapper, for each action we need to define a NameSpace similar to this one:

*Please note*: The comments above the method name are used by the Python script (called `build_manifest.py`) to build the manifest file automatically at every build event of the .NET solution. They are not normally needed.

```csharp
namespace TuneRx1
{
    // Name: Tune Receiver 1
    // Tooltip: Frequency knob for RX 1
    // Controllers: Knob
    [PluginActionId("it.iu2frl.streamdock.mqtt.tunerx1")]
    public class TuneRx1(ISDConnection connection, InitialPayload payload) : Common.BaseDialMqttItem(connection, payload)
    {
        public override void DialUp(DialPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Dial was rotated");
        }
    }
}
```
