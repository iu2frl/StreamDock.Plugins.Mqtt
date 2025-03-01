#region Using directives
using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BarRaider.SdTools;
using BarRaider.SdTools.Payloads;
using BarRaider.SdTools.Wrappers;
using Common;
using Coordinates;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using NLog.Config;
#endregion

#region Streamdeck Commands
namespace MqttButton
{
    // Name: Send MQTT Message
    // Tooltip: Sends a message to the MQTT broker when button is pressed
    // Controllers: Keypad
    [PluginActionId("it.iu2frl.streamdock.mqtt.mqttbutton")]
    public class MqttButton : Common.BaseKeypadItem
    {
        #region Button Configuration 
        private MqttElement mqttElement = new MqttElement();

        public MqttButton(ISDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "MqttButton constructor called");
        }
        #endregion

        public override void KeyPressed(KeyPayload payload)
        {
            mqttElement.SendMqttMessage();
            Logger.Instance.LogMessage(TracingLevel.INFO, "KeyPressed called");
        }

        private async void Connection_OnTitleParametersDidChange(object sender, SDEventReceivedEventArgs<BarRaider.SdTools.Events.TitleParametersDidChange> e)
        {
            await Connection.SetImageAsync(Common.StreamDock.UpdateKeyImage($"RX 1"));
        }
    }
}

namespace MqttKnob
{
    // Name: Send MQTT Message
    // Tooltip: Sends a message to the MQTT broker when knob is rotated
    // Controllers: Knob
    [PluginActionId("it.iu2frl.streamdock.mqtt.mqttknob")]
    public class MqttKnob : Common.BaseDialItem
    {
        private MqttElement mqttElement = new MqttElement();

        public MqttKnob(ISDConnection connection, InitialPayload payload) : base(connection, payload)
        { 
            Logger.Instance.LogMessage(TracingLevel.INFO, "MqttKnob constructor called");
        }
        public override void DialRotate(DialRotatePayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, $"{GetType().Name}: DialRotate called with ticks {payload.Ticks}");
            
            // Implement logic based on rotation direction
            if (payload.Ticks > 0)
            {
                // Clockwise rotation
                mqttElement.SendMqttMessage(false);
                Logger.Instance.LogMessage(TracingLevel.INFO, "Clockwise rotation");
            }
            else
            {
                // Counter-clockwise rotation
                mqttElement.SendMqttMessage(true);
                Logger.Instance.LogMessage(TracingLevel.INFO, "Counter-clockwise rotation");
            }
        }
    }
}
#endregion

namespace StreamDock.Plugins.PluginAction
{
    // Placeholder
}
