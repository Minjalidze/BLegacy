using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Facepunch.Cursor;
using UnityEngine;

namespace BClient.UserReferences;

internal class FPSBooster : MonoBehaviour
{
    [DllImport("kernel32")]
    private static extern int WritePrivateProfileString(string section, string key, string value, string filePath);
    [DllImport("kernel32")]
    private static extern int GetPrivateProfileString(string section, string key, string @default, StringBuilder retVal, int size, string filePath);
    
    public static ItemDataBlock GetItemDataBlock(string item)
    {
        return DatablockDictionary.GetByName(item);
    }
    
    private void Start()
    {
        try
        {
            render.frames = -1;
        
            ((BulletWeaponDataBlock)GetItemDataBlock("M4")).aimSway = 0;
            ((BulletWeaponDataBlock)GetItemDataBlock("P250")).aimSway = 0;
            ((BulletWeaponDataBlock)GetItemDataBlock("Bolt Action Rifle")).bulletRange = 250f;
            ((BulletWeaponDataBlock)GetItemDataBlock("Bolt Action Rifle")).aimSway = 0;
            ((BulletWeaponDataBlock)GetItemDataBlock("9mm Pistol")).aimSway = 0;
            ((BulletWeaponDataBlock)GetItemDataBlock("MP5A4")).aimSway = 0;
            ((BulletWeaponDataBlock)GetItemDataBlock("Shotgun")).aimSway = 0;
            ((BulletWeaponDataBlock)GetItemDataBlock("Revolver")).aimSway = 0;
        
            _masterTextureLimit = 2;
            _basemapDistance = 5000f;
            _heightmapPixelError = 250f;
            _treeBillboardDistance = 0f;
            _treeCrossFadeLength = 250f;
            _treeDistance = 550f;
            _treeMaximumFullLODCount = 5;
            _detailObjectDensity = 0f;
            _detailObjectDistance = 0f;
            _shadowCascades = 0;
            _shadowDistance = 0f;

            gfx.all = false;
            gfx.ssaa = false;
            gfx.bloom = false;
            gfx.grain = false;
            gfx.ssao = false;
            gfx.tonemap = false;
            gfx.shafts = false;
            gfx.damage = true;
        
            if (File.Exists("BCore\\TerrorBlade.ini"))
            {
                _treeDistance = float.Parse(GetConfigSectionInfo("Dota2Gavno", "TreeDistance"));
                _treeCrossFadeLength = float.Parse(GetConfigSectionInfo("Dota2Gavno", "TreeCrossFadeLength"));
                _basemapDistance = float.Parse(GetConfigSectionInfo("Dota2Gavno", "BasemapDistance"));
                _detailObjectDensity = float.Parse(GetConfigSectionInfo("Dota2Gavno", "DetailObjectDensity"));
                _detailObjectDistance = float.Parse(GetConfigSectionInfo("Dota2Gavno", "DetailObjectDistance"));
                _masterTextureLimit = int.Parse(GetConfigSectionInfo("Dota2Gavno", "MasterTextureLimit"));
                _treeBillboardDistance = float.Parse(GetConfigSectionInfo("Dota2Gavno", "TreeBillboardDistance"));
                _treeMaximumFullLODCount = int.Parse(GetConfigSectionInfo("Dota2Gavno", "TreeMaximumFullLODCount"));
                _shadowCascades = int.Parse(GetConfigSectionInfo("Dota2Gavno", "ShadowCascades"));
                _shadowDistance = float.Parse(GetConfigSectionInfo("Dota2Gavno", "ShadowDistance"));
            }
            else
            {
                Directory.CreateDirectory("BCore");
                File.Create("BCore\\TerrorBlade.ini");
                WritePrivateProfileString("Dota2Gavno", "TreeDistance", _treeDistance.ToString(), "BCore\\TerrorBlade.ini");
                WritePrivateProfileString("Dota2Gavno", "TreeCrossFadeLength", _treeCrossFadeLength.ToString(), "BCore\\TerrorBlade.ini");
                WritePrivateProfileString("Dota2Gavno", "BasemapDistance", _basemapDistance.ToString(), "BCore\\TerrorBlade.ini");
                WritePrivateProfileString("Dota2Gavno", "DetailObjectDensity", _detailObjectDensity.ToString(), "BCore\\TerrorBlade.ini");
                WritePrivateProfileString("Dota2Gavno", "DetailObjectDistance", _detailObjectDistance.ToString(), "BCore\\TerrorBlade.ini");
                WritePrivateProfileString("Dota2Gavno", "MasterTextureLimit", _masterTextureLimit.ToString(), "BCore\\TerrorBlade.ini");
                WritePrivateProfileString("Dota2Gavno", "TreeBillboardDistance", _treeBillboardDistance.ToString(), "BCore\\TerrorBlade.ini");
                WritePrivateProfileString("Dota2Gavno", "TreeMaximumFullLODCount", _treeMaximumFullLODCount.ToString(), "BCore\\TerrorBlade.ini");
                WritePrivateProfileString("Dota2Gavno", "ShadowCascades", _shadowCascades.ToString(), "BCore\\TerrorBlade.ini");
                WritePrivateProfileString("Dota2Gavno", "ShadowDistance", _shadowDistance.ToString(), "BCore\\TerrorBlade.ini");
            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
            throw;
        }
    }

    private void Update()
    {
        if (!NetCull.isClientRunning || PlayerClient.GetLocalPlayer() == null) return;
        
        if (Input.GetKeyDown(KeyCode.F5)) _isVisible = !_isVisible;
            
        _cursorNode ??= LockCursorManager.CreateCursorUnlockNode(false, "FPSUnlocker");
        _cursorNode.On = _isVisible;
        
        Terrain.activeTerrain.treeDistance = _treeDistance;
        Terrain.activeTerrain.treeBillboardDistance = _treeBillboardDistance;
        Terrain.activeTerrain.treeCrossFadeLength = _treeCrossFadeLength;
        Terrain.activeTerrain.treeMaximumFullLODCount = _treeMaximumFullLODCount;
        Terrain.activeTerrain.basemapDistance = _basemapDistance;
        Terrain.activeTerrain.detailObjectDensity = _detailObjectDensity;
        Terrain.activeTerrain.detailObjectDistance = _detailObjectDistance;
        QualitySettings.masterTextureLimit = _masterTextureLimit;
        QualitySettings.shadowCascades = _shadowCascades;
        QualitySettings.shadowDistance = _shadowDistance;
    }

    private void OnGUI()
    {
        if (!_isVisible) return;
        
        _buttonSkin = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
        _textSkin = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
        
        GUILayout.BeginArea(new Rect(10f, 80f, 350.0f, 850f));
        {
            GUILayout.BeginVertical("Box");
            {
                GUILayout.Label($"Общее качество = {_masterTextureLimit}", _textSkin);
                _masterTextureLimit = (int)GUILayout.HorizontalSlider(_masterTextureLimit, .0f, 10.0f);
                
                GUILayout.Label($"Деревья = {_treeDistance}", _textSkin);
                _treeDistance = GUILayout.HorizontalSlider(_treeDistance, 150.0f, 1500.0f);
                
                GUILayout.Label($"Качество деревьев = {_treeBillboardDistance}", _textSkin);
                _treeBillboardDistance = GUILayout.HorizontalSlider(_treeBillboardDistance, .0f, 300.0f);
                
                GUILayout.Label($"Тление деревьев = {_treeCrossFadeLength}", _textSkin);
                _treeCrossFadeLength = GUILayout.HorizontalSlider(_treeCrossFadeLength, .0f, 300.0f);
                
                GUILayout.Label($"Прорисовка объектов = {_detailObjectDistance}", _textSkin);
                _detailObjectDistance = GUILayout.HorizontalSlider(_detailObjectDistance, .0f, 500.0f);
                
                GUILayout.Label($"Плотность объектов = {_detailObjectDensity}", _textSkin);
                _detailObjectDensity = GUILayout.HorizontalSlider(_detailObjectDensity, .0f, 500.0f);
                
                GUILayout.Label($"Количество теней = {_shadowDistance}", _textSkin);
                _shadowDistance = GUILayout.HorizontalSlider(_shadowDistance, .0f, 15.0f);
                
                GUILayout.Label($"Каскады теней = {_shadowCascades}", _textSkin);
                _shadowCascades = (int)GUILayout.HorizontalSlider(_shadowCascades, .0f, 1.0f);
                
                GUILayout.Label($"Прорисовка земли = {_basemapDistance}", _textSkin);
                _basemapDistance = GUILayout.HorizontalSlider(_basemapDistance, .0f, 10000.0f);
                
                GUILayout.BeginHorizontal("Box");
                {
                    
                    if (GUILayout.Button("MINECRAFT!!!", _buttonSkin))
                    {
                        Terrain.activeTerrain.detailObjectDistance = 0f;
                        Terrain.activeTerrain.detailObjectDensity = 0f;
                        Texture.anisotropicFiltering = AnisotropicFiltering.Disable;
                        if (RenderTexture.active)
                        {
                            RenderTexture.active.antiAliasing = 0;
                            RenderTexture.active.anisoLevel = 0;
                        }
                        RenderSettings.fog = false;
                        QualitySettings.antiAliasing = 0;
                        QualitySettings.shadowCascades = 0;
                        QualitySettings.shadowDistance = 0f;
                        
                    }
                    if (GUILayout.Button("Сохранить", _buttonSkin))
                    {
                        WritePrivateProfileString("Dota2Gavno", "TreeDistance", _treeDistance.ToString(), "BCore\\TerrorBlade.ini");
                        WritePrivateProfileString("Dota2Gavno", "TreeCrossFadeLength", _treeCrossFadeLength.ToString(), "BCore\\TerrorBlade.ini");
                        WritePrivateProfileString("Dota2Gavno", "BasemapDistance", _basemapDistance.ToString(), "BCore\\TerrorBlade.ini");
                        WritePrivateProfileString("Dota2Gavno", "DetailObjectDensity", _detailObjectDensity.ToString(), "BCore\\TerrorBlade.ini");
                        WritePrivateProfileString("Dota2Gavno", "DetailObjectDistance", _detailObjectDistance.ToString(), "BCore\\TerrorBlade.ini");
                        WritePrivateProfileString("Dota2Gavno", "MasterTextureLimit", _masterTextureLimit.ToString(), "BCore\\TerrorBlade.ini");
                        WritePrivateProfileString("Dota2Gavno", "TreeBillboardDistance", _treeBillboardDistance.ToString(), "BCore\\TerrorBlade.ini");
                        WritePrivateProfileString("Dota2Gavno", "TreeMaximumFullLODCount", _treeMaximumFullLODCount.ToString(), "BCore\\TerrorBlade.ini");
                        WritePrivateProfileString("Dota2Gavno", "ShadowCascades", _shadowCascades.ToString(), "BCore\\TerrorBlade.ini");
                        WritePrivateProfileString("Dota2Gavno", "ShadowDistance", _shadowDistance.ToString(), "BCore\\TerrorBlade.ini");
                        
                    
                        if (!Directory.Exists("cfg"))
                        {
                            Directory.CreateDirectory("cfg");
                        }
                        var path = config.ConfigName();
                        var contents = ConsoleSystem.SaveToConfigString();
                        File.WriteAllText(path, contents);
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndArea();
    }

    private static string GetConfigSectionInfo(string section, string line)
    {
        var stringBuilder = new StringBuilder(255);
        GetPrivateProfileString(section, line, "", stringBuilder, 255, "BCore\\TerrorBlade.ini");
        return stringBuilder.ToString();
    }

    private GUIStyle _buttonSkin;
    private GUIStyle _textSkin;
    
    private bool _isVisible;
    private UnlockCursorNode _cursorNode;
    
    private static float _treeDistance;
    private static float _treeBillboardDistance;
    private static float _treeCrossFadeLength;
    private static int _treeMaximumFullLODCount;
    private static float _heightmapPixelError;
    private static float _basemapDistance;
    private static float _detailObjectDensity;
    private static float _detailObjectDistance;
    private static int _masterTextureLimit;
    private static int _shadowCascades;
    private static float _shadowDistance;
}