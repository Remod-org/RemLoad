#region License (GPL v2)
/*
    DESCRIPTION
    Copyright (c) 2023 RFC1920 <desolationoutpostpve@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License v2.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/
#endregion License Information (GPL v2)
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using HarmonyLib;
using System.IO;
using System.Net;
using Oxide.Core.Plugins;
//Reference: 0Harmony

/*
 * What is this?
 *  RemLoad is a Harmony mod that patches into the CSharpPluginLoader to detect and intercept files when called
 *  with http(s) in the directory name.  This is just a proof of concept, but is usable despite some hard-coded values for the remote site and plugin name.
 *  
 * What it does:
 * 1. Provides for a basic means of retrieving plugins remotely.
 * 2. Allows for basic authentication at the remote site.
 * 3. Deletes the remotely-loaded plugin(s) once this plugin itself is unloaded.
 * 
 * What it does not do:
 * 1. Secure the downloaded file(s).  This makes it perhaps acceptable in the traditional sense of paid plugins, but does not offer a true subscription model.
 *
 * See the one command here, rload.  This will show you how to call a manual load of your remote plugin.  The plugin will be downloaded, assuming auth is present and works,
 * into the standard plugins folder.
 * 
 * If you can make this better, please do.  But, let me know what you've done so it can be improved for everyone.  By nature of the licensing, sharing of the code
 * is required.
 */
namespace Oxide.Plugins
{
    [Info("RemLoad", "RFC1920", "1.0.2")]
    [Description("A remote plugin loader for Rust Oxide")]
    internal class RemLoad : RustPlugin
    {
        private ConfigData configData;
        public static RemLoad Instance;

        private List<string> loadedPlugins = new List<string>();
        private const string permUse = "remload.use";

        #region Message
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private void Message(IPlayer player, string key, params object[] args) => player.Message(Lang(key, player.Id, args));
        private void LMessage(IPlayer player, string key, params object[] args) => player.Reply(Lang(key, player.Id, args));
        #endregion

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["notauthorized"] = "You don't have permission to do that !!"
            }, this);
        }

        private void OnServerInitialized()
        {
            permission.RegisterPermission(permUse, this);
            LoadConfigVariables();

            AddCovalenceCommand("rload", "RemoteLoad");
            Instance = this;
        }

        [AutoPatch]
        [HarmonyPatch(typeof(CSharpPluginLoader), nameof(CSharpPluginLoader.Load), new Type[] { typeof(string), typeof(string) })]
        public static class CSharpPluginLoaderPatch
        {
            [HarmonyPrefix]
            private static void Prefix(CSharpPluginLoader __instance, ref string directory, ref string name)
            {
                if (directory.StartsWith("http"))
                {
                    Interface.GetMod().LogDebug($"Trying to load remote plugin from {directory}{name}");
                    HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(directory + name);
                    httpRequest.Method = WebRequestMethods.Http.Get;
                    string encoded = (string)Interface.GetMod().CallHook("RemLoadGetAuthString", name);
                    if (!string.IsNullOrEmpty(encoded))
                    {
                        httpRequest.Headers.Add("Authorization", "Basic " + encoded);
                    }
                    HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                    Stream httpResponseStream = httpResponse.GetResponseStream();
                    const int bufferSize = 1024;
                    byte[] buffer = new byte[bufferSize];

                    FileStream fileStream = File.OpenWrite(Path.Combine(Interface.GetMod().PluginDirectory, name));
                    int bytesRead;
                    while ((bytesRead = httpResponseStream.Read(buffer, 0, bufferSize)) != 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                    }
                    fileStream.Close();
                }
            }
        }

        [Command("rload")]
        private void RemoteLoad(IPlayer player, string command, string[] args)
        {
            string url = sites[configData.Options.Provider];
            const string plug = "Tides.cs";

            CompilablePlugin cp = CSharpPluginLoader.GetCompilablePlugin(url, plug);
            cp.Loader.Load(url, plug);
        }

        private readonly Dictionary<string, string> sites = new Dictionary<string, string>()
        {
            { "remod", "https://code.remod.org/private/" },
            { "yours", "https://yoursitehere/private/" }
        };

        private string RemLoadGetAuthString(string pluginName)
        {
            loadedPlugins.Add(pluginName);

            //DoLog($"Returning auth string for {pluginName}: '{auth}'");
            return Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(configData.Options.Username + ":" + configData.Options.Key));
        }

        private void DoLog(string message)
        {
            if (configData.debug) Interface.GetMod().LogInfo(message);
        }

        private void Unload()
        {
            foreach (string pluginName in loadedPlugins)
            {
                File.Delete(Path.Combine(Interface.GetMod().PluginDirectory, pluginName));
            }
        }

        private void DestroyAll<T>() where T : MonoBehaviour
        {
            foreach (T type in UnityEngine.Object.FindObjectsOfType<T>())
            {
                UnityEngine.Object.Destroy(type);
            }
        }

        private class ConfigData
        {
            public Options Options;
            public bool debug;
            public VersionNumber Version;
        }

        public class Options
        {
            public string Provider;
            public string Username;
            public string Key;
        }

        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();

            configData.Version = Version;
            SaveConfig(configData);
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            ConfigData config = new ConfigData
            {
                Options = new Options()
                {
                    Provider = "remod",
                    Username = "remload",
                    Key = "FAKEKEY"
                }
            };

            SaveConfig(config);
        }

        private void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }
    }
}
