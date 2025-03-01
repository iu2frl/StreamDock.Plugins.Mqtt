using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BarRaider.SdTools.Payloads;
using BarRaider.SdTools;
using Coordinates;
using MQTTnet.Client;
using MQTTnet;

namespace Common
{
    public class StreamDock
    {
        #region Private Methods
        public static Bitmap? UpdateKeyImage(string value)
        {
            Bitmap bmp;
            try
            {
                bmp = new Bitmap(ImageHelper.GetImage(Color.Black));

                bmp = new Bitmap(ImageHelper.SetImageText(bmp, value, new SolidBrush(Color.White), 72, 72));
                return bmp;

            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, ex.Message);
                Logger.Instance.LogMessage(TracingLevel.ERROR, ex.StackTrace);
            }

            return null;
        }
        #endregion
    }

    #region Custom classes
    public class MQTT_Client
    {
        private IMqttClient mqttClient;
        private readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);
        private int retryAttempts = 0;

        // Event that external classes can subscribe to
        public event Action<string, string> OnMessageReceived;

        // Get the subscriber from outside
        public IMqttClient Client
        { get => mqttClient; }

        public bool ConnectToBroker(string address, int port, string user, string password, bool authUserPass)
        {
            // Ignore if already connected
            if (mqttClient != null || ClientConnected)
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"MQTT Client is already connected, forcing a reconnection");
                DisconnectFromBroker().Wait();
            }

            // Cleanup parameters
            address = address.ToLower().Trim();
            user = user.Trim();
            password = password.Trim();

            // Connection details
            string receiversSetTopic = "receivers/get/#";

            // Create a MQTT client factory
            var factory = new MqttFactory();

            // Create a MQTT client instance
            mqttClient = factory.CreateMqttClient();

            // Create MQTT client options
            MqttClientOptionsBuilder options = new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString())
                .WithCleanSession(true)
                .WithCleanStart(true)
                .WithKeepAlivePeriod(new TimeSpan(0, 1, 0));

            if (authUserPass)
                options = options.WithCredentials(user, password);

            // TCP Server or Websocket
            options = options.WithWebSocketServer(webSocketOptions => webSocketOptions.WithUri($"ws://{address}:{port}/mqtt"));

            try
            {
                // Connect to MQTT broker
                mqttClient.ConnectAsync(options.Build()).Wait();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Failed to connect to MQTT broker: {ex.Message}");
                ClientConnected = false;
                return false;
            }

            if (mqttClient.IsConnected)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, "Connected to MQTT broker successfully");

                try
                {
                    // Subscribe to a topic
                    mqttClient.SubscribeAsync(receiversSetTopic).Wait();
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Cannot subscribe: {ex.Message}");
                }

                // Callback function when a message is received
                mqttClient.ApplicationMessageReceivedAsync += e =>
                {
                    OnMessageReceived?.Invoke(e.ApplicationMessage.Topic, Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment));
                    return Task.CompletedTask;
                };

                ClientConnected = true;
            }
            else
            {
                mqttClient?.Dispose();
                ClientConnected = false;
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Failed to connect to MQTT broker");
                return false;
            }

            return true;
        }

        public async Task DisconnectFromBroker()
        {
            if (mqttClient != null)
            {
                // Unsubscribe and disconnect
                try
                {
                    await mqttClient.DisconnectAsync();
                    mqttClient?.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Cannot dispose MQTT object: {ex.Message}");
                }
                ClientConnected = false;
                Logger.Instance.LogMessage(TracingLevel.INFO, "Disconnected from broker successfully");
            }
        }

        public async Task PublishMessageAsync(MQTT_Config mqttConfig, string topic, string payload)
        {
            if (!ConnectToBroker(mqttConfig.Host, mqttConfig.Port, mqttConfig.User, mqttConfig.Password, mqttConfig.UseAuthentication))
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, "Cannot connect to MQTT broker");
                return;
            }

            try
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel(0)
                    .WithRetainFlag(false)
                    .Build();

                await mqttClient.PublishAsync(message);

                Logger.Instance.LogMessage(TracingLevel.INFO, $"Published message: Topic={topic}, Payload={payload}");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Cannot send message: {ex.Message}");
            }

            await DisconnectFromBroker();
        }

        #region Public properties
        public static bool ClientConnected { get; private set; } = false;
        #endregion
    }

    public class MQTT_Config
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 1883;
        public string User { get; set; } = "mqtt";
        public string Password { get; set; } = "madeinitaly";
        public bool UseAuthentication { get; set; } = true;
    }

    public class BaseKeypadItem : KeypadBase
    {
        #region StreamDock events
        public BaseKeypadItem(ISDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType().Name}: Constructor called");
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
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType().Name}: OnTick called");
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType().Name}: Destructor called");
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType().Name}: ReceivedSettings called");
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType().Name}: ReceivedGlobalSettings called");
        }
        #endregion
    }

    public class BaseDialItem : EncoderBase
    {
        public BaseDialItem(ISDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType().Name}: Constructor called");
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
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType().Name}: Destructor called");
        }

        public override void OnTick()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{GetType().Name}: OnTick called");
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

    public class MqttElement
    {
        // Action parameters
        private string mqttPayload1 = "";
        private string mqttTopic1 = "";
        private string mqttPayload2 = "";
        private string mqttTopic2 = "";

        // MQTT Connnection parameters
        private MQTT_Config mqttConfig = new MQTT_Config();

        // MQTT Client
        private MQTT_Client mqttClient = new MQTT_Client();

        public void LoadConfig(dynamic settings)
        {
            if (settings.mqttSettings != null)
            {
                mqttConfig = settings.mqttSettings.ToObject<MQTT_Config>();
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "LoadSettings: Received empty MQTT settings");
            }
        }

        public void SaveConfig(dynamic settings)
        {

        }

        public void SendMqttMessage(bool sub = false)
        {
            if (!sub)
                mqttClient.PublishMessageAsync(mqttConfig, mqttTopic1, mqttPayload1).Wait();
            else
                mqttClient.PublishMessageAsync(mqttConfig, mqttTopic2, mqttPayload2).Wait();
        }
    }
    #endregion
}