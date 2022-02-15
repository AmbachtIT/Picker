using ColossalFramework;
using ColossalFramework.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Picker
{
    public partial class PickerTool
    {
        internal bool ReflectIntoNS2()
        {
            Assembly ass = GetNSAssembly();
            Type tPipette = ass.GetType("NetworkSkins.Tool.PipetteTool")
                ?? throw new Exception("NS2 failed: tPipette is null");
            object ns2 = ass.GetType("NetworkSkins.GUI.NetworkSkinPanelController").GetField("Instance").GetValue(null) ?? throw new Exception("NS2 failed: ns2 is null");
            object pipette = ns2.GetType().GetProperty("Tool").GetValue(ns2, null) ?? throw new Exception("NS2 failed: pipette is null");

            MethodInfo apply = tPipette.GetMethod("ApplyTool", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new Exception("NS2 failed: apply is null");
            FieldInfo segmentId = tPipette.GetField("HoveredSegmentId", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new Exception("NS2 failed: segmentId is null");

            //Debug.Log($"NS2: {ns2},{apply}\n{pipette} <{pipette.GetType()}>\nlmb:{segmentId} <{segmentId.GetType()}>");

            segmentId.SetValue(pipette, hoveredId.NetSegment);
            apply.Invoke(pipette, null);

            return true;
        }

        internal static bool isNS2Installed()
        {
            return (GetNSAssembly() != null);
        }

        internal static Assembly GetNSAssembly()
        {
            foreach (PluginManager.PluginInfo pluginInfo in Singleton<PluginManager>.instance.GetPluginsInfo())
            {
                if (pluginInfo.userModInstance?.GetType().Name.ToLower() == "networkskinsmod" && pluginInfo.isEnabled)
                {
                    // Network Skins 1 - unsupported - uses CimTools
                    if (pluginInfo.GetAssemblies().Any(mod => mod.GetName().Name.ToLower() == "cimtools"))
                    {
                        break;
                    }

                    foreach (Assembly assembly in pluginInfo.GetAssemblies())
                    {
                        if (assembly.GetName().Name.ToLower() == "networkskins")
                        {
                            return assembly;
                        }
                    }
                }
            }

            return null;
        }
    }
}
