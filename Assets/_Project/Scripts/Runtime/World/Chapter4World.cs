using System.Collections.Generic;
using UnityEngine;

namespace CalculusLabEscape
{
    public sealed partial class Chapter4World : MonoBehaviour
    {
        public LabPlayerController player;
        public Transform station, calf, mother, water, exit;
        [field:SerializeField] public RenderTexture Preview {get;private set;}
        public bool CaseOpened {get;private set;}
        [SerializeField] Transform caseLid, waypoint;
        [SerializeField] List<Renderer> portalSections=new List<Renderer>();
        public void OpenCase(){if(!CaseOpened&&CaseAudio!=null)CaseAudio.Play("case");CaseOpened=true;}
        public List<Renderer> indicators = new List<Renderer>();
        public List<Transform> bridge = new List<Transform>();
        public List<Transform> devices = new List<Transform>();
        public int topic;
        [SerializeField] Material dark, cyan, amber, stone, leaf, stable;
        [SerializeField] bool authored;
        bool ownsPreview;
        public bool IsAuthored=>authored;
        public float CompletionDuration=>topic==1&&TryGetComponent<BridgeRescueSequence>(out var rescue)?rescue.Duration:topic==2&&TryGetComponent<CaseShutdownSequence>(out var shutdown)?shutdown.Duration:4;
        float glitch;
        int restoredCount;
        [Header("Saved scene management")]
        public GameManager gameManager;
        public Chapter4Game Game
        {
            get
            {
                if(gameManager!=null&&gameManager.Session!=null)return gameManager;
                // Existing Editor verification fixtures can temporarily attach the
                // rules component without starting the saved scene manager.
                var fixture=GetComponent<Chapter4Game>();
                return fixture!=null&&fixture.Session!=null?fixture:null;
            }
        }
        public static void Build(int topic)
        {
            var world=FindFirstObjectByType<Chapter4World>();
            if(world==null||!world.authored||world.topic!=topic||world.gameManager==null)
                throw new System.InvalidOperationException("Open a saved level scene with its Game Manager. Map generation is an Edit Mode operation.");
            world.gameManager.InitializeMission();
        }
        Material Mat(string name,Color c,bool glow=false)
        {
            var m = new Material(Shader.Find("Standard")){name=name,color=c};
            m.SetFloat("_Glossiness",.05f);
            if(glow) {m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*.65f);} return m;
        }
        GameObject Shape(string name,Vector3 p,Vector3 s,Material m,PrimitiveType type=PrimitiveType.Cube,bool solid=true)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(transform);go.transform.position=p;go.transform.localScale=s;
            go.GetComponent<Renderer>().sharedMaterial=m;if(!solid){if(Application.isPlaying)Destroy(go.GetComponent<Collider>());else DestroyImmediate(go.GetComponent<Collider>());}return go;
        }
        TextMesh Sign(string text,Vector3 p,float size=.16f)
        {
            var go=new GameObject(text);go.transform.SetParent(transform);go.transform.position=p;
            var t=go.AddComponent<TextMesh>();t.text=text;t.characterSize=size;t.fontSize=48;t.anchor=TextAnchor.MiddleCenter;t.color=topic==2?new Color(.8f,.95f,.93f):new Color(.13f,.15f,.16f);return t;
        }
        void Create()
        {
            if(Application.isPlaying)throw new System.InvalidOperationException("Map generation is only allowed before Play.");
            dark=Mat("Dark outlines",new Color(.18f,.2f,.21f));cyan=Mat("Panel ivory",new Color(.82f,.83f,.79f));
            amber=Mat("Unrestored grey",new Color(.37f,.39f,.38f));stone=Mat("Stone paper",new Color(.64f,.67f,.65f));
            leaf=Mat("Forest grey green",new Color(.42f,.47f,.42f));stable=Mat("Restored ivory",new Color(.94f,.95f,.88f));
            var light=new GameObject("Key light").AddComponent<Light>();light.transform.SetParent(transform);light.type=LightType.Directional;light.intensity=1.35f;light.transform.rotation=Quaternion.Euler(48,-30,0);
            RenderSettings.ambientLight=topic==1?new Color(.57f,.59f,.56f):new Color(.52f,.55f,.56f);
            RenderSettings.fog=true;RenderSettings.fogColor=topic==1?new Color(.75f,.79f,.75f):new Color(.71f,.75f,.75f);RenderSettings.fogDensity=.018f;
            if(topic>0)ConfigureMissionArt(light);
            if(topic==1) Forest(); else Laboratory();
            var p=new GameObject("Player");p.transform.SetParent(transform);p.transform.position=new Vector3(0,.1f,-5);
            var controller=p.AddComponent<CharacterController>();controller.height=1.8f;controller.radius=.3f;controller.center=Vector3.up*.9f;
            player=p.AddComponent<LabPlayerController>();
            var cam=new GameObject("Player Camera");cam.tag="MainCamera";cam.transform.SetParent(p.transform,false);cam.transform.localPosition=Vector3.up*1.6f;
            var camera=cam.AddComponent<Camera>();camera.fieldOfView=65;camera.backgroundColor=RenderSettings.fogColor;camera.clearFlags=CameraClearFlags.SolidColor;cam.AddComponent<AudioListener>();
            var preview=new GameObject("Storyboard view").AddComponent<Camera>();preview.transform.SetParent(transform);preview.transform.position=topic==1?new Vector3(7,4,5):new Vector3(5,3,-3);preview.transform.LookAt(topic==1?new Vector3(0,.8f,10):new Vector3(0,1.6f,1));preview.fieldOfView=48;
            Preview=new RenderTexture(topic>0&&!ProductionCanvas.LegacyQA?646:1200,300,16){name="Chapter4 scene preview"};preview.targetTexture=Preview;
            ownsPreview=true;
            if(topic==2){casePreview=preview;preview.cullingMask&=~(1<<2);FocusCase(-1,true);}
            waypoint=Shape("Active device marker",station.position+Vector3.up*2.7f,Vector3.one*.22f,stable,PrimitiveType.Sphere,false).transform;
            if(topic==2)waypoint.gameObject.layer=2;
            if(topic>0){camera.farClipPlane=90;preview.farClipPlane=90;}
        }
        void Laboratory()
        {
            Shape("Floor",new Vector3(0,-.3f,7),new Vector3(22,.6f,28),stone);
            Shape("Left wall",new Vector3(-11,4,7),new Vector3(.5f,8,28),stone);
            Shape("Right wall",new Vector3(11,4,7),new Vector3(.5f,8,28),stone);
            Shape("Rear wall",new Vector3(0,4,21),new Vector3(22,8,.5f),cyan);
            Shape("Entry wall",new Vector3(0,4,-7),new Vector3(22,8,.5f),cyan);
            for(int i=0;i<7;i++)
            {
                Shape("Floor circuit",new Vector3(0,.012f,-4+i*4),new Vector3(18,.02f,.025f),topic==2?edgeMetal:dark,solid:false);
                Shape("Wall rib L",new Vector3(-10.65f,3,-4+i*4),new Vector3(.14f,6,.14f),dark);
                Shape("Wall rib R",new Vector3(10.65f,3,-4+i*4),new Vector3(.14f,6,.14f),dark);
            }
            for(int x=-8;x<=8;x+=2)Shape("Floor grid",new Vector3(x,.015f,6),new Vector3(.025f,.02f,24),topic==2?edgeMetal:dark,solid:false);
            for(int side=-1;side<=1;side+=2)
            {
                Shape("Side workbench",new Vector3(side*8,1,7),new Vector3(3,2,7),stone);
                for(int j=0;j<3;j++){Shape("Wall equipment",new Vector3(side*10.5f,3,3+j*5),new Vector3(.5f,1.5f,2),dark);Shape("Equipment face",new Vector3(side*10.2f,3,3+j*5),new Vector3(.2f,1.25f,1.75f),cyan);}
            }
            station=Shape(topic==0?"Examination console":"Containment case",new Vector3(0,.65f,1),new Vector3(3.8f,1.3f,1.8f),stone).transform;
            Shape("Console screen",new Vector3(0,1.4f,1),new Vector3(3.4f,.18f,1.5f),dark);
            var consoleSign=Sign(topic==0?"EXAMINATION CONSOLE":"03 / CONTAINMENT CASE",topic==0?new Vector3(0,2.35f,1):new Vector3(0,1.06f,.068f),topic==0?.09f:.035f);
            var roomSign=Sign(topic==0?"01  /  THE PORTAL EXAMINATION":"03  /  CONTAINMENT CONTROL",new Vector3(0,6,20),.11f);
            // Keep large world signs out of the close-up camera; small labels on the device remain visible.
            if(topic==2){consoleSign.gameObject.layer=2;roomSign.gameObject.layer=2;}
            int count=topic==0?5:4;
            for(int i=0;i<count;i++) indicators.Add(Shape("Status indicator "+(i+1),new Vector3((i-(count-1)/2f)*1.1f,3.7f,topic==0?13:8),Vector3.one*.5f,amber,PrimitiveType.Sphere,false).GetComponent<Renderer>());
            if(topic==0)
            {
                for(int i=0;i<40;i++)
                {
                    float a=i*Mathf.PI*2/40;var go=Shape("Portal ring",new Vector3(Mathf.Cos(a)*3,3.3f+Mathf.Sin(a)*3,14),new Vector3(.24f,.52f,.4f),i%2==0?cyan:stone,solid:false);
                    go.transform.rotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg);portalSections.Add(go.GetComponent<Renderer>());
                }
                exit=Shape("Portal field",new Vector3(0,3.3f,14.15f),new Vector3(5,5,.15f),dark,PrimitiveType.Sphere,false).transform;
                Shape("Portal platform",new Vector3(0,.12f,13),new Vector3(7,.24f,3),stone);
                Shape("Activation lever",new Vector3(3.7f,1.1f,12),new Vector3(.25f,1.3f,.25f),amber);
            }
            else
            {
                Shape("Case tray",new Vector3(0,1.43f,1),new Vector3(4.8f,.22f,2.5f),dark);
                caseLid=new GameObject("Hinged case lid").transform;caseLid.SetParent(transform);caseLid.position=new Vector3(0,1.62f,2.15f);
                var lid=Shape("Case lid",new Vector3(0,1.67f,1.03f),new Vector3(4.8f,.14f,2.3f),stone).transform;lid.SetParent(caseLid,true);
                var manualPanel=Shape("Manual inside lid",new Vector3(0,1.54f,1.03f),new Vector3(3.7f,.035f,1.7f),cyan).transform;manualPanel.SetParent(caseLid,true);
                for(int i=0;i<4;i++)
                {
                    float x=(i-1.5f)*1.1f;
                    var d=Shape("Case module "+i,new Vector3(x,1.66f,1.13f),new Vector3(.95f,.25f,1.45f),cyan).transform;devices.Add(d);
                    indicators[i].transform.position=new Vector3(x,1.91f,.65f);indicators[i].transform.localScale=Vector3.one*.15f;
                }
                CreateCaseFeedback();
                Shape("Case carry handle",new Vector3(0,1.5f,-.42f),new Vector3(1.25f,.12f,.15f),dark);
                DressLaboratory();
            }
        }

        void Forest()
        {
            Shape("Near riverbank",new Vector3(0,-.5f,-1),new Vector3(28,1,16),leaf);
            Shape("Far riverbank",new Vector3(0,-.5f,18),new Vector3(28,1,12),leaf);
            water=Shape("Rising river",new Vector3(0,-.6f,9.5f),new Vector3(28,.2f,5),riverMaterial,solid:false).transform;
            // Invisible bank edge keeps the trainee safe; the calf crosses in the completion animation.
            Shape("River safety boundary",new Vector3(0,.8f,6.7f),new Vector3(28,2,.2f),leaf).GetComponent<Renderer>().enabled=false;
            Shape("Left boundary",new Vector3(-14,1,-1),new Vector3(.2f,3,17),leaf).GetComponent<Renderer>().enabled=false;
            Shape("Right boundary",new Vector3(14,1,-1),new Vector3(.2f,3,17),leaf).GetComponent<Renderer>().enabled=false;
            Shape("Back boundary",new Vector3(0,1,-9),new Vector3(28,3,.2f),leaf).GetComponent<Renderer>().enabled=false;
            for(int side=-1;side<=1;side+=2)
            {
                for(int z=6;z<=13;z+=2)Shape("Bridge post",new Vector3(side*1.7f,.65f,z),new Vector3(.14f,1.7f,.14f),dark);
                Shape("Bridge upper handrail",new Vector3(side*1.7f,1.28f,9.5f),new Vector3(.1f,.1f,7),timber);
                Shape("Bridge lower handrail",new Vector3(side*1.7f,.8f,9.5f),new Vector3(.075f,.075f,7),timber);
            }
            for(int i=0;i<9;i++) bridge.Add(Shape("Bridge section "+i,new Vector3(0,-1.1f,6.9f+i*.65f),new Vector3(3,.22f,.55f),stone).transform);
            for(int i=0;i<14;i++)
            {
                float x=(i%2==0?-1:1)*(8+i%3*2);float z=-4+i*1.8f;
                MakePine(new Vector3(x,0,z),.8f+(i%3)*.15f,i);
            }
            for(int i=0;i<3;i++)
            {
                var p=new[]{new Vector3(-5,.7f,1),new Vector3(4,.7f,2),new Vector3(3,.7f,5)}[i];devices.Add(Shape("Repair device "+i,p,new Vector3(2,1.4f,1.5f),dark).transform);
                repairLights.Add(Shape("Repair glow",p+Vector3.up*.77f,new Vector3(1.5f,.1f,1),cyan).GetComponent<Renderer>());
                var label=Sign("",p+Vector3.up*1.4f,.035f);label.alignment=TextAlignment.Center;repairSigns.Add(label);
            }
            Shape("Power Core dial",devices[0].position+Vector3.up*.91f,new Vector3(.7f,.075f,.7f),dark,PrimitiveType.Cylinder);
            for(int i=0;i<3;i++)converterLocks.Add(Shape("Converter lock "+i,devices[1].position+new Vector3((i-1)*.52f,.84f,0),new Vector3(.34f,.28f,.35f),dark).GetComponent<Renderer>());
            station=devices[0];calf=Cow("Calf",new Vector3(-2.2f,0,5),.55f);mother=Cow("Mother",new Vector3(1.5f,0,15),1);
            InitialiseBridgeFeedback();
            DressForest();
        }
        Transform Cow(string name,Vector3 p,float scale)
        {
            var root=new GameObject(name).transform;root.SetParent(transform);root.position=p;
            var cream=Mat(name+" cream",new Color(.83f,.76f,.59f));
            var parts=new List<GameObject>{Shape(name+" body",p+Vector3.up*1.05f*scale,new Vector3(1.1f,.8f,1.7f)*scale,cream,PrimitiveType.Sphere),
                Shape(name+" head",p+new Vector3(0,1.4f,-.95f)*scale,Vector3.one*.7f*scale,cream)};
            parts.Add(Shape(name+" muzzle",p+new Vector3(0,1.24f,-1.35f)*scale,new Vector3(.58f,.32f,.3f)*scale,cream));
            for(int side=-1;side<=1;side+=2)
            {
                parts.Add(Shape(name+" ear",p+new Vector3(side*.48f,1.65f,-.93f)*scale,new Vector3(.35f,.14f,.3f)*scale,cream));
                parts.Add(Shape(name+" eye",p+new Vector3(side*.23f,1.52f,-1.32f)*scale,Vector3.one*.085f*scale,dark,PrimitiveType.Sphere));
            }
            for(int i=0;i<4;i++) parts.Add(Shape(name+" leg",p+new Vector3(i%2==0?-.35f:.35f,.4f,i<2?-.5f:.5f)*scale,new Vector3(.2f,.8f,.2f)*scale,cream));
            foreach(var part in parts) part.transform.SetParent(root,true);DressCow(root,p,scale);return root;
        }
        public void Restore(int count,bool animate=false)
        {
            bool changed=count!=restoredCount;
            restoredCount=count;
            for(int i=0;i<indicators.Count;i++) indicators[i].sharedMaterial=i<count?stable:amber;
            for(int i=0;i<portalSections.Count;i++)portalSections[i].sharedMaterial=i<count*8?stable:stone;
            if(topic==1)
            {
                if(changed&&animate){bridgeLift=1;bridgeSoundPending=bridgeInstallSoundDelay;}
                if(!animate)bridgeSoundPending=-1;
                for(int i=0;i<bridge.Count;i++)
                {
                    int sections=count>=4?9:count*2;
                    if(!animate)bridge[i].position=new Vector3(0,i<sections?.08f:-1.1f,6.9f+i*.65f);
                    bridge[i].GetComponent<Renderer>().sharedMaterial=timber;
                    if(i<bridgeMarkers.Count)bridgeMarkers[i].enabled=i<sections;
                }
            }
        }
        public void SetCaseModules(bool dial,bool wires,bool sequence,bool gauge)
        {
            bool[] complete={dial,wires,sequence,gauge};
            for(int i=0;i<4&&i<indicators.Count;i++)indicators[i].sharedMaterial=complete[i]?caseComplete:(!dial&&i>0?amber:caseActive);
        }
        public void Glitch()
        {
            if(topic==2)return; // Module retry feedback must not enlarge or disturb completed lamps.
            glitch=.6f;
            if(topic==1)
            {
                if(water!=null)water.position=new Vector3(0,Mathf.Min(-.05f,water.position.y+.035f),9.5f);
                int installed=restoredCount>=4?9:restoredCount*2;
                for(int i=installed;i<bridge.Count;i++)bridge[i].GetComponent<Renderer>().sharedMaterial=repairWarning;
            }
        }
        void Update()
        {
            if(waypoint!=null&&station!=null)waypoint.position=station.position+Vector3.up*(2.5f+Mathf.Sin(Time.time*2)*.1f);
            if(topic==1)UpdateBridgeFeedback();
            if(topic==2)UpdateCaseFeedback();
            if(glitch>0){glitch-=Time.deltaTime;foreach(var r in indicators) r.transform.localScale=Vector3.one*(.5f+Mathf.Sin(Time.time*40)*.08f);if(glitch<=0&&topic==1)Restore(restoredCount,true);}
            else foreach(var r in indicators) r.transform.localScale=Vector3.one*(topic==2?.15f:.5f);
        }
        void OnDestroy()
        {
            if(ownsPreview&&Preview!=null)ReleaseGenerated(Preview);
            foreach(var mesh in artMeshes)ReleaseGenerated(mesh);
        }
        static void ReleaseGenerated(Object value)
        {
            if(value==null)return;
#if UNITY_EDITOR
            if(UnityEditor.EditorUtility.IsPersistent(value))return;
#endif
            if(value is RenderTexture texture)texture.Release();
            if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);
        }
        public void ResetSavedWorldForPlay()
        {
            // Scene geometry/materials stay authored; only the live camera target is transient.
            var preview=transform.Find("Storyboard view").GetComponent<Camera>();
            Preview=new RenderTexture(Preview){name="Chapter4 live scene preview"};ownsPreview=true;preview.targetTexture=Preview;
            if(topic==1){SetBridgeStage(0);Restore(0);GetComponent<BridgeRescueSequence>()?.ResetPose();}
            if(topic==2){GetComponent<CaseShutdownSequence>()?.ResetPose();ResetCaseFeedback();caseLid.localRotation=Quaternion.identity;masterCover.localRotation=Quaternion.identity;FocusCase(-1,true);}
        }
#if UNITY_EDITOR
        public static Chapter4World CreateAuthored(int topic)
        {
            var root=new GameObject("Level World - "+Chapter4Game.Scenes[topic]);
            var world=root.AddComponent<Chapter4World>();world.topic=topic;world.Create();world.authored=true;return world;
        }
        public void EditorOpenCase(bool open)
        {if(topic==2&&caseLid!=null)caseLid.localRotation=Quaternion.Euler(open?105:0,0,0);}
        public void EditorRescueState(bool active)
        {
            if(topic!=1)return;
            SetBridgeStage(active?11:0);Restore(active?4:0);
            foreach(var sign in repairSigns)sign.gameObject.SetActive(!active);
            if(waypoint!=null)waypoint.gameObject.SetActive(!active);
        }
#endif
        public void Activate(float t)
        {
            if(topic==1)
            {
                var rescue=GetComponent<BridgeRescueSequence>();
                if(rescue!=null){rescue.Sample(t);if(water!=null)water.position=new Vector3(0,-.6f,9.5f);return;}
                var start=new Vector3(-2.2f,0,5);var approach=new Vector3(0,0,6.45f);var end=new Vector3(.3f,0,14);
                // Align with the entrance before crossing so the calf does not pass through the handrail.
                calf.position=t<.27f?Vector3.Lerp(start,approach,t/.27f):Vector3.Lerp(approach,end,(t-.27f)/.73f);
                calf.rotation=Quaternion.LookRotation(-(t<.27f?approach-start:end-approach),Vector3.up);
                if(water!=null)water.position=new Vector3(0,-.6f,9.5f);
                var camera=player.GetComponentInChildren<Camera>();
                camera.transform.position=new Vector3(7,5,6);
                camera.transform.LookAt(new Vector3(0,.7f,11));
            }
            else if(topic==2)AnimateDeactivation(t);
            else if(exit!=null)exit.GetComponent<Renderer>().sharedMaterial=stable;
        }
    }
}
