using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.IO;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using EManagersLib.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Serialization;
using UnityEngine;

namespace Picker
{
    static class PropLayer
    {
        public static IPropsWrapper Manager;

        private static bool EML = false;

        public static void Initialise()
        {
            EML = isEMLInstalled();

            if (EML)
            {
                Manager = new EPropsManager();
            }
            else
            {
                Manager = new PropsManager();
            }

            Debug.Log($"Picker EML: {EML} <{Manager.GetType()}>");
        }

        internal static bool isEMLInstalled()
        {
            foreach (PluginManager.PluginInfo pluginInfo in Singleton<PluginManager>.instance.GetPluginsInfo())
            {
                foreach (Assembly assembly in pluginInfo.GetAssemblies())
                {
                    if (assembly.GetName().Name.ToLower().Equals("emanagerslib"))
                    {
                        return pluginInfo.isEnabled;
                    }
                }
            }

            return false;
        }

        internal static string getVersionText()
        {
            if (isEMLInstalled())
            {
                return "Extended Managers Library: Found";
            }

            return "Extended Managers Library: Not Found";
        }
    }

    public interface IPropsWrapper
    {
        PropInfo GetInfo(InstanceID id);
        IProp Buffer(uint id);
        IProp Buffer(InstanceID id);
        uint GetId(InstanceID id);
        InstanceID SetProp(InstanceID id, uint i);
        InstanceID GetInstanceID(uint i);
    }

    class PropsManager : IPropsWrapper
    {
        private readonly PropInstance[] propBuffer;

        public PropsManager()
        {
            propBuffer = Singleton<PropManager>.instance.m_props.m_buffer;
        }

        public PropInfo GetInfo(InstanceID id)
        {
            return propBuffer[id.Prop].Info;
        }

        public IProp Buffer(uint id)
        {
            return new PropWrapper((ushort)id);
        }

        public IProp Buffer(InstanceID id)
        {
            return new PropWrapper(id.Prop);
        }

        public uint GetId(InstanceID id)
        {
            return id.Prop;
        }

        public InstanceID SetProp(InstanceID id, uint i)
        {
            id.Prop = (ushort)i;
            return id;
        }

        public InstanceID GetInstanceID(uint i)
        {
            InstanceID id = default;
            id.Prop = (ushort)i;
            return id;
        }
    }

    // Extended Managers Library support
    class EPropsManager : IPropsWrapper
    {
        private readonly EPropInstance[] propBuffer;

        public EPropsManager()
        {
            propBuffer = PropAPI.GetPropArray().m_buffer;
        }

        public PropInfo GetInfo(InstanceID id)
        {
            return propBuffer[id.GetProp32()].Info;
        }

        public IProp Buffer(uint id)
        {
            Debug.Log($"AAA9 {id}");
            return new EPropWrapper(id);
        }

        public IProp Buffer(InstanceID id)
        {
            Debug.Log($"AAA8 {GetId(id)}");
            return new EPropWrapper(GetId(id));
        }

        public uint GetId(InstanceID id)
        {
            return id.GetProp32();
        }

        public InstanceID SetProp(InstanceID id, uint i)
        {
            id.SetProp32(i);
            return id;
        }

        public InstanceID GetInstanceID(uint i)
        {
            InstanceID id = default;
            id.SetProp32(i);
            return id;
        }
    }
}
