﻿using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Logging;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Buttplug;
using AetherSenseRedux.Triggers;

namespace AetherSenseRedux
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "AetherSense Redux";

        private const string commandName = "/as";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }

        private ButtplugClient Buttplug;

        private List<Device> DevicePool;

        private List<ChatTrigger> ChatTriggerPool;

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;

            Buttplug = new ButtplugClient("AetherSense Redux");
            Buttplug.DeviceAdded += OnDeviceAdded;
            Buttplug.DeviceRemoved += OnDeviceRemoved;
            Buttplug.ScanningFinished += OnScanComplete;

            this.DevicePool = new List<Device>();
            this.ChatTriggerPool = new List<ChatTrigger>();

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            // you might normally want to embed resources and load them from the manifest stream
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            PluginUi = new PluginUI(Configuration);

            CommandManager.AddHandler(commandName, new CommandInfo(OnShowUI)
            {
                HelpMessage = "Opens the Aether Sense Redux configuration window"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        public void Dispose()
        {
            foreach (Device device in DevicePool)
            {
                try
                {
                    device.Stop();
                }
                catch (Exception)
                {
                    PluginLog.Error("Could not stop device {0}, device disconnected?", device.ClientDevice.Name);
                }
                this.DevicePool.Remove(device);
            }
            PluginUi.Dispose();
            CommandManager.RemoveHandler(commandName);
        }

        private void OnDeviceAdded(object? sender, DeviceAddedEventArgs e)
        {
            PluginLog.Information("Device {0} added", e.Device.Name);
            this.DevicePool.Add(new Device(e.Device));
        }

        private void OnDeviceRemoved(object? sender, DeviceRemovedEventArgs e)
        {
            PluginLog.Information("Device {0} removed", e.Device.Name);
            foreach (Device device in this.DevicePool)
            {
                if (device.ClientDevice == e.Device)
                {
                    try
                    {
                        device.Stop();
                    }
                    catch (Exception)
                    {
                        PluginLog.Error("Could not stop device {0}, device disconnected?", device.Name);
                    }
                    this.DevicePool.Remove(device);
                }
            }
        }

        private void OnScanComplete(object? sender, EventArgs e)
        {
            Task.Run(DoScan);
        }

        private void OnChatReceived(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            ChatMessage chatMessage = new ChatMessage(type, senderId, ref sender, ref message, ref isHandled);
            foreach (ChatTrigger t in ChatTriggerPool)
            {
                if (t.Enabled)
                {
                    t.Queue(chatMessage);
                }
            }
        }

        private async Task MainLoop()
        {
            //register OnChatReceived handler

            //attempt to connect

            while (Configuration.Enabled)
            {
                await Task.Delay(10);
            }
        }

        private async Task DoScan()
        {
            await Task.Delay(1000);
            try
            {
                await Buttplug.StartScanningAsync();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Asynchronous scanning failed.");
            }
        }

        private void OnShowUI(string command, string args)
        {
            // in response to the slash command, just display our main ui
            PluginUi.Visible = true;
        }

        private void DrawUI()
        {
            PluginUi.Draw();
        }

        private void DrawConfigUI()
        {
            PluginUi.SettingsVisible = true;
        }
    }
}
