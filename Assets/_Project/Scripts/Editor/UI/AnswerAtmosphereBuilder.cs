#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CalculusLabEscape.Editor
{
    public static partial class ProductionUIBuilder
    {
        const string RoomImage="Assets/_Project/Art/UIAtmosphere/SimulationLabRoom-v1.png";
        [MenuItem("Calculus Lab/UI/Enrich answer atmosphere")]
        public static void EnrichAnswerAtmosphere()
        {
            AssetDatabase.ImportAsset(RoomImage,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(RoomImage);
            importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;importer.sRGBTexture=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
            importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.alphaSource=TextureImporterAlphaSource.None;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(RoomImage);if(texture==null||texture.width<1000)throw new Exception("Missing simulation room artwork.");
            foreach(string name in new[]{"LimitsUI","DifferentiationUI","IntegrationUI"})
            {
                string path=PrefabsPath+"/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int buttons=root.GetComponentsInChildren<Button>(true).Length,labels=root.GetComponentsInChildren<TMP_Text>(true).Length;
                    var art=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="SimulationAnswerArt");
                    int topic=name=="LimitsUI"?0:name=="DifferentiationUI"?1:2;
                    var sky=art.Find("SimulationAnswerSky").GetComponent<SimulationAnswerBackdrop>();
                    var room=art.Find("SimulationRoomImage");
                    if(room==null){room=Rect(art,"SimulationRoomImage",0,0,1280,720);room.gameObject.AddComponent<RawImage>();room.SetAsFirstSibling();}
                    var image=room.GetComponent<RawImage>();image.texture=texture;image.color=Color.white;image.raycastTarget=false;
                    sky.color=new Color(1,1,1,.18f);sky.roomDetails=true;
                    var main=art.Find("AnswerGlassSurface").GetComponent<SimulationAnswerBackdrop>();main.color=new Color(.055f,.115f,.175f,.78f);main.consoleDetails=true;main.subject=topic;
                    var side=art.Find("DeviceGlassSurface").GetComponent<SimulationAnswerBackdrop>();side.color=new Color(.05f,.12f,.17f,.88f);side.consoleDetails=topic!=0;side.previewFrame=topic!=0;side.subject=topic;
                    main.rim=side.rim=Color.Lerp(new Color(.18f,.33f,.43f,.8f),new Color(sky.lightTint.r,sky.lightTint.g,sky.lightTint.b,.8f),.22f);
                    var header=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Header");header.GetComponent<Image>().color=new Color(.025f,.065f,.105f,.92f);
                    var badge=header.Find("MissionEmblem");if(badge==null)badge=Rect(header,"MissionEmblem",686,23,42,33);
                    var emblem=badge.GetComponent<SimulationAnswerBackdrop>();if(emblem==null)emblem=badge.gameObject.AddComponent<SimulationAnswerBackdrop>();
                    emblem.emblem=true;emblem.subject=topic;emblem.lightTint=sky.lightTint;emblem.raycastTarget=false;
                    if(root.GetComponentsInChildren<Button>(true).Length!=buttons||root.GetComponentsInChildren<TMP_Text>(true).Length!=labels)throw new Exception("Atmosphere changed controls or question labels.");
                    if(art.GetComponentsInChildren<Graphic>(true).Any(g=>g.raycastTarget))throw new Exception("Atmosphere intercepts input.");
                    if(art.GetComponentsInChildren<RawImage>(true).Length!=1||room.GetSiblingIndex()!=0)throw new Exception("Background texture layer is incomplete.");
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();Debug.Log("ANSWER_ATMOSPHERE_PREFABS_PASS");
        }
        public static void EnrichAnswerAtmosphereAndBuild(){EnrichAnswerAtmosphere();EnrichAnswerAtmosphere();BuildDelivery();Debug.Log("ANSWER_ATMOSPHERE_DELIVERY_PASS");}
        public static void CaptureAnswerAtmosphere()=>SceneAuthoring.ValidateAndCaptureTo(Path.GetFullPath("../../qa/answer-atmosphere/editor"));
    }
}
#endif
