using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.Plugins;
using EManagersLib.API;
using System;
using System.Collections.Generic;
using System.Reflection;
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
            Debug.Log($"Picker EML: {EML}");

            if (EML)
            {
                Manager = new EPropsManager();
            }
            else
            {
                Manager = new PropsManager();
            }
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
        Vector3 GetPosition(InstanceID id);
        float GetAngle(InstanceID id);
        List<InstanceID> GetObjectBuffer(ToolBase.RaycastInput input);
    }

    class PropsManager : IPropsWrapper
    {
        public PropInfo GetInfo(InstanceID id)
        {
            return PropManager.instance.m_props.m_buffer[id.Prop].Info;
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

        public Vector3 GetPosition(InstanceID id)
        {
            return PropManager.instance.m_props.m_buffer[id.Prop].Position;
        }

        public float GetAngle(InstanceID id)
        {
            return PropManager.instance.m_props.m_buffer[id.Prop].Angle;
        }

        public List<InstanceID> GetObjectBuffer(ToolBase.RaycastInput input)
        {
            List<InstanceID> objectBuffer = new List<InstanceID>();
            Vector3 mouseCurrentPosition = Vector3.zero;

            ToolBase.RaycastOutput output = PickerTool.instance.GetRaycast(input);

            mouseCurrentPosition = output.m_hitPos;

            if (output.m_netSegment != 0) objectBuffer.Add(new InstanceID() { NetSegment = (ushort)output.m_netSegment });
            if (output.m_treeInstance != 0) objectBuffer.Add(new InstanceID() { Tree = output.m_treeInstance });
            if (output.m_propInstance != 0) objectBuffer.Add(new InstanceID() { Prop = output.m_propInstance });
            if (output.m_building != 0) objectBuffer.Add(new InstanceID() { Building = (ushort)output.m_building });
            objectBuffer.Sort((a, b) => (a.Position() - mouseCurrentPosition).sqrMagnitude.CompareTo((b.Position() - mouseCurrentPosition).sqrMagnitude));

            return objectBuffer;
        }
    }

    // Extended Managers Library support
    class EPropsManager : IPropsWrapper
    {
        public EPropsManager()
        {
            PropAPI.Initialize();
        }

        public PropInfo GetInfo(InstanceID id)
        {
            return PropAPI.Wrapper.GetInfo(id.GetProp32());
        }

        public IProp Buffer(uint id)
        {
            return new EPropWrapper(id);
        }

        public IProp Buffer(InstanceID id)
        {
            return new EPropWrapper(id.GetProp32());
        }

        public uint GetId(InstanceID id)
        {
            return id.GetProp32();
        }

        public Vector3 GetPosition(InstanceID id)
        {
            return PropAPI.Wrapper.GetPosition(id);
        }

        public float GetAngle(InstanceID id)
        {
            return PropAPI.Wrapper.GetAngle(id);
        }

        public List<InstanceID> GetObjectBuffer(ToolBase.RaycastInput input)
        {
            List<InstanceID> objectBuffer = new List<InstanceID>();
            Vector3 mouseCurrentPosition = Vector3.zero;

            PropAPI.RayCast(input, out EToolBase.RaycastOutput output);

            mouseCurrentPosition = output.m_hitPos;

            if (output.m_netSegment != 0) objectBuffer.Add(new InstanceID() { NetSegment = (ushort)output.m_netSegment });
            if (output.m_treeInstance != 0) objectBuffer.Add(new InstanceID() { Tree = output.m_treeInstance });
            /* EInstanceID and InstanceID can be used interchangeably whether EML exists or not */
            if (output.m_propInstance != 0) objectBuffer.Add(new EInstanceID() { Prop = output.m_propInstance });
            if (output.m_building != 0) objectBuffer.Add(new InstanceID() { Building = (ushort)output.m_building });
            objectBuffer.Sort((a, b) => (a.Position() - mouseCurrentPosition).sqrMagnitude.CompareTo((b.Position() - mouseCurrentPosition).sqrMagnitude));

            return objectBuffer;
        }
    }
}
