﻿using ColossalFramework.UI;
using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Picker
{
    public class IngameLoader : LoadingExtensionBase
    {
        private LoadMode loadMode;

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            loadMode = mode;
            InstallMod();
        }

        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();
            UninstallMod();
        }

        public void InstallMod()
        {
            if (PickerTool.instance == null)
            {
                ToolController toolController = ToolsModifierControl.toolController;
                PickerTool.instance = toolController.gameObject.AddComponent<PickerTool>();
                PickerTool.instance.enabled = false;
            }
            else
            {
                Debug.Log($"Picker: InstallMod with existing instance!");
            }
        }

        //public override void OnReleased()
        //{
        //    base.OnReleased();
        //    GameObject.Destroy(PickerTool.instance);
        //}

        public static void UninstallMod()
        {
            if (ToolsModifierControl.toolController.CurrentTool is PickerTool)
                ToolsModifierControl.SetTool<DefaultTool>();

            GameObject.Destroy(PickerTool.instance);

            if (PickerTool.instance != null)
            {
                PickerTool.instance.enabled = false;
            }
        }

        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            if (LoadingManager.instance.m_loadingComplete)
            {
                if (PickerTool.instance == null)
                {
                    ToolController toolController = ToolsModifierControl.toolController;
                    PickerTool.instance = toolController.gameObject.AddComponent<PickerTool>();
                    PickerTool.instance.enabled = false;
                }
            }
        }

        internal static bool InGame() => SceneManager.GetActiveScene().name == "Game";

        public void OnEnabled()
        {
            if (InGame())
            {
                // basic ingame hot reload
                OnLevelLoaded(loadMode);
            }
        }

        public void OnDisabled()
        {
            if (InGame())
            {
                // basic in game hot unload
                UninstallMod();
            }
        }
    }

    //public class IngameKeybindingResolver : ThreadingExtensionBase
    //{
    //    public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
    //    {

    //        if (!UIView.HasModalInput() && !UIView.HasInputFocus() && Picker.toggleTool.IsPressed(Event.current))
    //        {
    //        //    SimulateClick();
    //        //}

    //        //if (Input.GetKeyDown(KeyCode.N))
    //        //{
    //            Debug.Log("N key");
    //            PickerTool.instance.enabled = !PickerTool.instance.enabled;
    //            ToolsModifierControl.SetTool<PickerTool>();
    //        }
    //    }
    //}
}