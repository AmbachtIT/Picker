using EManagersLib.API;
using UnityEngine;

namespace Picker
{
    public static class Db
    {
        public static bool on = false;

        public static void l(object m)
        {
            if (on) Debug.Log(m);
        }

        public static void w(object m)
        {
            if (on) Debug.LogWarning(m);
        }

        public static void e(object m)
        {
            if (on) Debug.LogWarning(m);
        }

        // Extensions
        /* Structures in C# return by value, and these structures are large, hence return by ref is more efficient */
        public static ref NetSegment S(this ushort s)
        {
            //Debug.Log(s);
            return ref NetManager.instance.m_segments.m_buffer[s];
        }

        public static ref NetNode N(this ushort n)
        {
            //Debug.Log(n);
            return ref NetManager.instance.m_nodes.m_buffer[n];
        }

        public static ref Building B(this ushort b)
        {
            //Debug.Log(b);
            ref Building building = ref BuildingManager.instance.m_buildings.m_buffer[b];
            while (building.m_parentBuilding > 0)
            {
                building = BuildingManager.instance.m_buildings.m_buffer[building.m_parentBuilding];
            }
            return ref building;
        }

        public static ref TreeInstance T(this uint t)
        {
            //Debug.Log(t);
            return ref TreeManager.instance.m_trees.m_buffer[t];
        }

        public static Vector3 Position(this InstanceID id)
        {
            if (id.NetSegment != 0) return id.NetSegment.S().m_middlePosition;
            if (id.NetNode != 0 && id.NetNode < 32768) return id.NetNode.N().m_position;
            if (PropAPI.GetPropID(id) != 0) return PropAPI.Wrapper.GetPosition(id);
            if (id.Building != 0) return id.Building.B().m_position;
            if (id.Tree != 0) return id.Tree.T().Position;

            return Vector3.zero;
        }

        public static PrefabInfo Info(this InstanceID id)
        {
            if (id.NetSegment != 0) return id.NetSegment.S().Info;
            if (id.NetNode != 0 && id.NetNode < 32768) return id.NetNode.N().Info;
            if (PropAPI.GetPropID(id) != 0) return PropAPI.Wrapper.GetInfo(id);
            if (id.Building != 0) return id.Building.B().Info;
            if (id.Tree != 0) return id.Tree.T().Info;

            return null;
        }
    }
}
