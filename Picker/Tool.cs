using ColossalFramework;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnifiedUI.Helpers;
using UnityEngine;

namespace Picker
{
    public partial class PickerTool : ToolBase
    {
        public static PickerTool instance;

        internal UIPickerButton m_button;
        public UIComponent UUIButton;

        public static SavedBool doSetFRTMode = new SavedBool("doSetFRT", Picker.settingsFileName, true, true);
        public static SavedBool openMenu = new SavedBool("openMenu", Picker.settingsFileName, true, true);
        public static SavedBool openMenuNetworks = new SavedBool("openMenuNetworks", Picker.settingsFileName, false, true);
        public static SavedBool useUUI = new SavedBool("useUUI", Picker.settingsFileName, false, true);

        public InstanceID hoveredId = InstanceID.Empty;

        public bool hasSteppedOver = false;
        public List<InstanceID> objectBuffer = new List<InstanceID>();

        public static FindItManager FindIt = null;

        protected override void Awake()
        {
            m_toolController = FindObjectOfType<ToolController>();
            enabled = false;

            m_button = UIView.GetAView().AddUIComponent(typeof(UIPickerButton)) as UIPickerButton;
            if (useUUI)
            {
                EnableUUI();
            }

            PropLayer.Initialise();
        }

        private Dictionary<string, UIComponent> _componentCache = new Dictionary<string, UIComponent>();

        private Color hoverColor = new Color32(0, 181, 255, 255);

        private T FindComponentCached<T>(string name) where T : UIComponent
        {
            if (!_componentCache.TryGetValue(name, out UIComponent component) || component == null)
            {
                component = UIView.Find<UIButton>(name);
                _componentCache[name] = component;
            }

            return component as T;
        }

        internal RaycastOutput GetRaycast(RaycastInput input)
        {
            RayCast(input, out RaycastOutput output);
            return output;
        }

        protected override void OnToolUpdate()
        {
            base.OnToolUpdate();

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastInput input = new RaycastInput(ray, Camera.main.farClipPlane)
            {
                m_ignoreTerrain = false,
                m_ignoreSegmentFlags = NetSegment.Flags.None,
                m_ignoreNodeFlags = NetNode.Flags.All,
                m_ignorePropFlags = PropInstance.Flags.None,
                m_ignoreTreeFlags = TreeInstance.Flags.None,
                m_ignoreBuildingFlags = Building.Flags.None,

                m_ignoreCitizenFlags = CitizenInstance.Flags.All,
                m_ignoreVehicleFlags = Vehicle.Flags.Created,
                m_ignoreDisasterFlags = DisasterData.Flags.All,
                m_ignoreTransportFlags = TransportLine.Flags.All,
                m_ignoreParkedVehicleFlags = VehicleParked.Flags.All,
                m_ignoreParkFlags = DistrictPark.Flags.All
            };

            objectBuffer = PropLayer.Manager.GetObjectBuffer(input);

            //RayCast(input, out RaycastOutput output);
            
            if (objectBuffer.Count > 0)
            {
                hoveredId = objectBuffer[0];
            }
            else
            {
                hoveredId = InstanceID.Empty;
            }

            // A prefab has been selected. Find it in the UI and enable it.
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            {
                PrefabInfo info = hoveredId.Info();
                if (info == null)
                {
                    enabled = false;
                    ToolsModifierControl.SetTool<DefaultTool>();
                    return;
                }
                Activate(info);
            }

            // Escape key or RMB hit = disable the tool
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                enabled = false;
                ToolsModifierControl.SetTool<DefaultTool>();
            }
        }

        internal void Activate(PrefabInfo info)
        {
            if (openMenu)
            {
                if (openMenuNetworks)
                {
                    if (info is NetInfo)
                    {
                        ActivateCall(true, DefaultPrefab(info));
                    }
                    else
                    {
                        ActivateCall(false, info);
                    }
                }
                else
                {
                    ActivateCall(true, DefaultPrefab(info));
                }
                return;
            }
            else
            {
                ActivateCall(false, info);
            }
        }

        internal void ActivateCall(bool open, PrefabInfo info)
        {
            if (Event.current.control == open)
            {
                GetToolFromPrefab(DefaultPrefab(info));
            }
            else
            {
                ShowInPanelResolveGrowables(DefaultPrefab(info));
            }
        }

        private void GetToolFromPrefab(PrefabInfo info)
        {
            if (info is BuildingInfo)
            {
                Singleton<ToolManager>.instance.m_properties.CurrentTool = FindObjectOfType<BuildingTool>();
                ((BuildingTool)Singleton<ToolManager>.instance.m_properties.CurrentTool).m_prefab = (BuildingInfo)info;
                return;
            }

            if (info is PropInfo)
            {
                Singleton<ToolManager>.instance.m_properties.CurrentTool = FindObjectOfType<PropTool>();
                ((PropTool)Singleton<ToolManager>.instance.m_properties.CurrentTool).m_prefab = (PropInfo)info;
                return;
            }

            if (info is TreeInfo)
            {
                Singleton<ToolManager>.instance.m_properties.CurrentTool = FindObjectOfType<TreeTool>();
                ((TreeTool)Singleton<ToolManager>.instance.m_properties.CurrentTool).m_prefab = (TreeInfo)info;
                return;
            }

            if (info is NetInfo)
            {
                Singleton<ToolManager>.instance.m_properties.CurrentTool = FindObjectOfType<NetTool>();
                ((NetTool)Singleton<ToolManager>.instance.m_properties.CurrentTool).m_prefab = (NetInfo)info;
                return;
            }

            throw new Exception("Invalid tool choice");
        }

        private void ShowInPanelResolveGrowables(PrefabInfo pInfo)
        {
            //Debug.Log($"Hovered: {pInfo.name} ({hoveredId.Type}) <{hoveredId.GetType()}>\nB:{hoveredId.Building}, P/D:{hoveredId.Prop}, PO:{hoveredId.NetLane}, N:{hoveredId.NetNode}, S:{hoveredId.NetSegment}");
            if (!(pInfo is BuildingInfo || pInfo is PropInfo))
            {
                ShowInPanel(pInfo);
                return;
            }

            // Try to locate in Find It!
            if (FindIt.Searchbox == null)
            {
                return;
            }
            if (FindIt.FilterDropdown == null)
            {
                return;
            }

            if (pInfo is PropInfo propInfo)
            {
                FindIt.Find((propInfo.m_isDecal ? "Decal" : "Prop"), propInfo);
                return;
            }

            BuildingInfo info = pInfo as BuildingInfo;
            if (info != null && (
                (info.m_class.m_service == ItemClass.Service.Residential) ||
                (info.m_class.m_service == ItemClass.Service.Industrial) ||
                (info.m_class.m_service == ItemClass.Service.Commercial) ||
                (info.m_class.m_service == ItemClass.Service.Office) ||
                (info.m_class.m_service == ItemClass.Service.Tourism)
                ))
            {
                // Reflect into the scroll panel, starting with the growable panel:
                FindIt.Find("Growable", info);
            }
            else
            {
                ShowInPanel(pInfo);
            }
        }

        private string GetButtonName(PrefabInfo info)
        {
            if (info.GetAI() is ExtractingFacilityAI)
            {
                BuildingInfo bi = info as BuildingInfo;

                if (bi.name.Contains("Beech")) return bi.name.Replace("Beech", "Alder");
                if (bi.name.Contains("Conifer")) return bi.name.Replace("Conifer", "Alder");
                if (bi.name.Contains("Orange")) return bi.name.Replace("Orange", "Apple");
                if (bi.name.Contains("Pear")) return bi.name.Replace("Pear", "Apple");
                if (bi.name.Contains("Green House")) return bi.name.Replace("Green House", "Apple");
            }
            return info.name;
        }

        private void ShowInPanel(PrefabInfo info)
        {
            UIButton button = FindComponentCached<UIButton>(GetButtonName(info));
            //Debug.Log($"Button for {info.name}:{button?.name} <{(button == null ? "null" : button.GetType().ToString())}>");
            if (button != null)
            {
                // NS2 integration will go here when its menu filter bug is fixed

                UIView.Find("TSCloseButton").SimulateClick();
                UITabstrip subMenuTabstrip = null;
                UIScrollablePanel scrollablePanel = null;
                UIPanel filterPanel;
                UIComponent current = button, parent = button.parent;

                int subMenuTabstripIndex = -1, menuTabstripIndex = -1;
                while (parent != null)
                {
                    if (current.name == "ScrollablePanel")
                    {
                        subMenuTabstripIndex = parent.zOrder;
                        scrollablePanel = current as UIScrollablePanel;
                    }
                    if (current.name == "GTSContainer")
                    {
                        menuTabstripIndex = parent.zOrder;
                        subMenuTabstrip = parent.Find<UITabstrip>("GroupToolstrip");
                    }
                    current = parent;
                    parent = parent.parent;
                }

                UITabstrip menuTabstrip = current.Find<UITabstrip>("MainToolstrip");
                if (scrollablePanel == null || subMenuTabstrip == null || menuTabstrip == null || menuTabstripIndex == -1 || subMenuTabstripIndex == -1)
                {
                    Debug.Log($"UI Panel not found");
                    return;
                }

                menuTabstrip.selectedIndex = menuTabstripIndex;
                menuTabstrip.ShowTab(menuTabstrip.tabs[menuTabstripIndex].name);
                subMenuTabstrip.selectedIndex = subMenuTabstripIndex;
                subMenuTabstrip.ShowTab(subMenuTabstrip.tabs[subMenuTabstripIndex].name);

                filterPanel = scrollablePanel.parent.Find<UIPanel>("FilterPanel");
                if (filterPanel != null)
                {
                    foreach (UIMultiStateButton c in filterPanel.GetComponentsInChildren<UIMultiStateButton>())
                    {
                        if (c.isVisible && c.activeStateIndex == 1)
                        {
                            c.activeStateIndex = 0;
                        }
                    }
                }

                StartCoroutine(ShowInPanelProcess(scrollablePanel, button));
            }
            else
            {
                Debug.Log($"Button not found, falling back to FindIt/All");
                FindIt.Find("All", info);
            }
        }

        private IEnumerator<object> ShowInPanelProcess(UIScrollablePanel scrollablePanel, UIButton button)
        {
            yield return new WaitForSeconds(0.10f);

            if (hoveredId.NetSegment > 0 && isNS2Installed())
            {
                try
                {
                    ReflectIntoNS2();
                    yield break;
                }
                catch (Exception e)
                {
                    Debug.Log($"NS2 failed:\n{e}");
                }
            }

            button.SimulateClick();
            scrollablePanel.ScrollIntoView(button);
        }

        public PrefabInfo DefaultPrefab(PrefabInfo resolve)
        {
            if (!(resolve is NetInfo)) return resolve;

            NetInfo info = resolve as NetInfo;
            for (uint i = 0; i < PrefabCollection<NetInfo>.LoadedCount(); i++)
            {
                NetInfo prefab = PrefabCollection<NetInfo>.GetLoaded(i);
                if ((AssetEditorRoadUtils.TryGetBridge(prefab) != null && AssetEditorRoadUtils.TryGetBridge(prefab).name == info.name) ||
                   (AssetEditorRoadUtils.TryGetElevated(prefab) != null && AssetEditorRoadUtils.TryGetElevated(prefab).name == info.name) ||
                   (AssetEditorRoadUtils.TryGetSlope(prefab) != null && AssetEditorRoadUtils.TryGetSlope(prefab).name == info.name) ||
                   (AssetEditorRoadUtils.TryGetTunnel(prefab) != null && AssetEditorRoadUtils.TryGetTunnel(prefab).name == info.name))
                {
                    if (AssetEditorRoadUtils.TryGetBridge(prefab) != null && AssetEditorRoadUtils.TryGetBridge(prefab).name == info.name)
                    {
                        FRTSet("BridgeMode");
                    }
                    else if (AssetEditorRoadUtils.TryGetElevated(prefab) != null && AssetEditorRoadUtils.TryGetElevated(prefab).name == info.name)
                    {
                        FRTSet("ElevatedMode");
                    }
                    else if (AssetEditorRoadUtils.TryGetTunnel(prefab) != null && AssetEditorRoadUtils.TryGetTunnel(prefab).name == info.name)
                    {
                        FRTSet("TunnelMode");
                    }
                    else
                    {
                        FRTSet("GroundMode");
                    }

                    return prefab;
                }
                else if (prefab == info)
                {
                    FRTSet("GroundMode");
                }
            }
            return info;
        }

        private void FRTSet(string buttonName)
        {
            if (doSetFRTMode == Event.current.shift)
                return;

            UIButton button = FindComponentCached<UIButton>("FRT_" + buttonName);
            if (button is UIComponent)
            {
                SimulateClick(button);
            }
        }

        // From MoreShortcuts by Boogieman Sam
        private static void SimulateClick(UIComponent component)
        {
            Camera camera = component.GetCamera();
            Vector3 vector = camera.WorldToScreenPoint(component.center);
            Ray ray = camera.ScreenPointToRay(vector);
            UIMouseEventParameter p = new UIMouseEventParameter(component, UIMouseButton.Left, 1, ray, vector, Vector2.zero, 0f);

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            if (component.isEnabled)
            {
                component.GetType().GetMethod("OnMouseDown", flags).Invoke(component, new object[] { p });
                component.GetType().GetMethod("OnClick", flags).Invoke(component, new object[] { p });
                component.GetType().GetMethod("OnMouseUp", flags).Invoke(component, new object[] { p });
            }
            else
            {
                component.GetType().GetMethod("OnDisabledClick", flags).Invoke(component, new object[] { p });
            }
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            base.RenderOverlay(cameraInfo);
            if (!enabled) return;

            if (hoveredId.NetSegment != 0)
            {
                /* avoid creating local copies, and get reference is faster for large structures */
                //NetSegment hoveredSegment = hoveredId.NetSegment.S();
                NetTool.RenderOverlay(cameraInfo, ref hoveredId.NetSegment.S(), hoverColor, hoverColor);
            }
            else if (hoveredId.Building != 0)
            {
                Building[] buildings = BuildingManager.instance.m_buildings.m_buffer;
                ref Building hoveredBuilding = ref hoveredId.Building.B();
                ushort subBuildingID = hoveredBuilding.m_subBuilding;
                BuildingTool.RenderOverlay(cameraInfo, ref hoveredBuilding, hoverColor, hoverColor);

                while (subBuildingID > 0)
                {
                    BuildingTool.RenderOverlay(cameraInfo, ref buildings[subBuildingID], hoverColor, hoverColor);
                    subBuildingID = buildings[subBuildingID].m_subBuilding;
                }

                //ref Building hoveredBuilding2 = ref hoveredId.Building.B();
                //BuildingTool.RenderOverlay(cameraInfo, ref hoveredBuilding2, hoverColor, hoverColor);

                //while (hoveredBuilding2.m_subBuilding > 0)
                //{
                //    hoveredBuilding2 = ref BuildingManager.instance.m_buildings.m_buffer[hoveredBuilding2.m_subBuilding];
                //    BuildingTool.RenderOverlay(cameraInfo, ref hoveredBuilding2, hoverColor, hoverColor);
                //}
            }
            else if (hoveredId.Tree != 0)
            {
                ref TreeInstance hoveredTree = ref hoveredId.Tree.T();
                TreeTool.RenderOverlay(cameraInfo, hoveredTree.Info, hoveredTree.Position, hoveredTree.Info.m_minScale, hoverColor);
            }
            else if (PropLayer.Manager.GetId(hoveredId) != 0)
            {
                PropInfo prefab = PropLayer.Manager.GetInfo(hoveredId);
                PropTool.RenderOverlay(cameraInfo, prefab, PropLayer.Manager.GetPosition(hoveredId), prefab.m_minScale, PropLayer.Manager.GetAngle(hoveredId), hoverColor);
            }
        }

        internal void EnableUUI()
        {
            if (!useUUI)
            {
                return;
            }

            try
            {
                var hotkeys = new UUIHotKeys { ActivationKey = OptionsKeymapping.toggleTool };
                Texture2D texture = ResourceLoader.loadTextureFromAssembly("Picker.Icons.Picker.png");

                UUIButton = UUIHelpers.RegisterToolButton(
                    name: "PickerTool",
                    groupName: null,
                    tooltip: "Picker",
                    tool: this,
                    icon: texture,
                    hotkeys: hotkeys
                    );
                m_button.Hide();
            }
            catch (Exception e)
            {
                Debug.Log(e);
                UIView.ForwardException(e);
            }
        }

        internal void DisableUUI()
        {
            m_button?.Show();
            UUIButton?.Destroy();
            UUIButton = null;
        }
    }
}