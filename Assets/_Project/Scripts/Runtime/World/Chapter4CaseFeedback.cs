using UnityEngine;

namespace CalculusLabEscape
{
    public sealed partial class Chapter4World
    {
        [field:SerializeField] public MissionAudio CaseAudio {get;private set;}
        public bool MasterCoverOpen {get;private set;}
        public bool DeviceDeactivated {get;private set;}
        [SerializeField] Camera casePreview;
        [SerializeField] Material caseActive,caseComplete,caseWarning;
        [SerializeField] Transform exponentNeedle,divisorNeedle,pressureNeedle,releaseHandle,masterCover;
        [SerializeField] Renderer[] caseWires=new Renderer[3],caseCards=new Renderer[4],powerBars=new Renderer[4];
        [SerializeField] Renderer constantSocket;
        readonly bool[] wireAccepted=new bool[3],cardAccepted=new bool[4];
        bool dialAccepted,constantAccepted,gaugeAccepted;
        int caseFocus=-1,retryModule=-1,draftExponent=4,draftDivisor=1,draftRelease;
        float caseRetry;
        bool shutdownStarted;

        Transform Pivot(string name,Vector3 position)
        {var t=new GameObject(name).transform;t.SetParent(transform);t.position=position;return t;}
        Transform CaseNeedle(string name,Vector3 position,float size)
        {
            Shape(name+" bezel",position-Vector3.up*.02f,new Vector3(size*1.12f,.05f,size*1.12f),edgeMetal,PrimitiveType.Cylinder,false);
            Shape(name+" face",position,new Vector3(size,.045f,size),instrumentFace,PrimitiveType.Cylinder,false);
            for(int i=0;i<9;i++)
            {
                float angle=i*40*Mathf.Deg2Rad;
                var tick=Shape(name+" tick",position+new Vector3(Mathf.Sin(angle)*size*.40f,.052f,Mathf.Cos(angle)*size*.40f),new Vector3(.018f,.007f,.047f),dark,solid:false);tick.transform.rotation=Quaternion.Euler(0,i*40,0);
            }
            var pivot=Pivot(name,position+Vector3.up*.065f);
            var pointer=Shape(name+" pointer",pivot.position+Vector3.forward*size*.18f,new Vector3(.035f,.025f,size*.42f),caseActive,solid:false).transform;pointer.SetParent(pivot,true);
            return pivot;
        }
        void CreateCaseFeedback()
        {
            caseActive=Mat("Case active cyan",new Color(.09f,.55f,.65f),true);
            caseComplete=Mat("Case accepted green",new Color(.13f,.5f,.26f));
            caseWarning=Mat("Case retry amber",new Color(.8f,.37f,.06f),true);
            CaseAudio=gameObject.AddComponent<MissionAudio>();
            exponentNeedle=CaseNeedle("Exponent needle",new Vector3(-1.65f,1.85f,1.02f),.48f);
            divisorNeedle=CaseNeedle("Divisor needle",new Vector3(-1.65f,1.85f,1.59f),.48f);
            for(int i=0;i<3;i++)
            {
                float z=.96f+i*.27f;
                caseWires[i]=Shape("Accepted wire "+i,new Vector3(-.55f,1.84f,z),new Vector3(.62f,.045f,.055f),amber,solid:false).GetComponent<Renderer>();
                for(int side=-1;side<=1;side+=2)Shape("Wire socket",new Vector3(-.55f+side*.34f,1.84f,z),Vector3.one*.10f,dark,PrimitiveType.Sphere,false);
            }
            constantSocket=Shape("Common constant socket",new Vector3(-.55f,1.82f,1.75f),new Vector3(.4f,.045f,.15f),amber,solid:false).GetComponent<Renderer>();
            for(int i=0;i<4;i++)
            {
                caseCards[i]=Shape("Procedure card "+i,new Vector3(.55f,1.82f,.94f+i*.24f),new Vector3(.67f,.06f,.17f),amber,solid:false).GetComponent<Renderer>();
                var number=Sign((i+1).ToString(),caseCards[i].transform.position+Vector3.up*.034f,.012f);number.color=new Color(.8f,.95f,.93f);number.transform.rotation=Quaternion.Euler(90,0,0);number.transform.SetParent(caseCards[i].transform,true);
            }
            pressureNeedle=CaseNeedle("Pressure needle",new Vector3(1.65f,1.85f,1.4f),.69f);
            releaseHandle=Pivot("Pressure release handle",new Vector3(1.65f,1.85f,.88f));
            var handle=Shape("Release grip",releaseHandle.position+Vector3.up*.16f,new Vector3(.3f,.07f,.07f),caseActive,solid:false).transform;handle.SetParent(releaseHandle,true);
            var stem=Shape("Release stem",releaseHandle.position+Vector3.up*.08f,new Vector3(.045f,.16f,.045f),dark,solid:false).transform;stem.SetParent(releaseHandle,true);
            for(int i=0;i<4;i++)
            {
                float x=(i-1.5f)*1.1f;
                var label=Sign(new[]{"DIAL","WIRES","STEPS","GAUGE"}[i],new Vector3(x,1.805f,.48f),.014f);label.color=new Color(.8f,.95f,.93f);label.transform.rotation=Quaternion.Euler(90,0,0);
                powerBars[i]=Shape("Device power bar "+i,new Vector3(x,1.58f,.02f),new Vector3(.68f,.04f,.09f),caseActive,solid:false).GetComponent<Renderer>();
            }
            Shape("Master switch housing",new Vector3(0,1.61f,-.13f),new Vector3(.94f,.12f,.42f),dark,solid:false);
            exit=Pivot("Master switch lever",new Vector3(0,1.7f,-.14f));
            var lever=Shape("Master lever grip",exit.position+Vector3.up*.09f,new Vector3(.38f,.15f,.1f),caseWarning,solid:false).transform;lever.SetParent(exit,true);
            masterCover=Pivot("Master switch cover hinge",new Vector3(0,1.85f,.1f));
            var cover=Shape("Master switch cover",new Vector3(0,1.85f,-.14f),new Vector3(.95f,.06f,.5f),stone,solid:false).transform;cover.SetParent(masterCover,true);
            SetCaseModules(false,false,false,false);
        }
        public void FocusCase(int module,bool immediate=false)
        {
            if(topic!=2||casePreview==null)return;
            caseFocus=Mathf.Clamp(module,-1,3);
            casePreview.orthographic=true;
            if(immediate)PositionCasePreview(1);
        }
        void PositionCasePreview(float blend)
        {
            var target=caseFocus<0?new Vector3(0,1.75f,1):devices[caseFocus].position+Vector3.up*.18f;
            var position=target+new Vector3(0,4,-4.2f);
            casePreview.transform.position=Vector3.Lerp(casePreview.transform.position,position,blend);
            casePreview.transform.rotation=Quaternion.Slerp(casePreview.transform.rotation,Quaternion.LookRotation(target-position),blend);
            casePreview.orthographicSize=Mathf.Lerp(casePreview.orthographicSize,caseFocus<0?1.4f:.72f,blend);
        }
        public void SetCaseDraft(int exponent,int divisor,int release)
        {draftExponent=exponent;draftDivisor=divisor;draftRelease=release;}
        void ResetCaseFeedback()
        {
            dialAccepted=constantAccepted=gaugeAccepted=false;
            for(int i=0;i<3;i++){wireAccepted[i]=false;caseWires[i].sharedMaterial=amber;}
            for(int i=0;i<4;i++){cardAccepted[i]=false;caseCards[i].sharedMaterial=amber;var p=caseCards[i].transform.position;p.y=1.82f;caseCards[i].transform.position=p;}
            constantSocket.sharedMaterial=amber;SetCaseModules(false,false,false,false);
        }
        public void SyncCaseFeedback(Chapter4Game game,bool snap=false)
        {
            if(topic!=2)return;
            dialAccepted=game.MissionDone("I_DIAL");constantAccepted=game.MissionDone("I_C");gaugeAccepted=game.MissionDone("I_GAUGE");
            for(int i=0;i<3;i++){wireAccepted[i]=game.MissionDone("I_W"+i);caseWires[i].sharedMaterial=wireAccepted[i]?caseComplete:amber;}
            for(int i=0;i<4;i++){cardAccepted[i]=game.MissionDone("I_SEQ_"+i);caseCards[i].sharedMaterial=cardAccepted[i]?caseComplete:amber;}
            constantSocket.sharedMaterial=constantAccepted?caseComplete:amber;
            SetCaseModules(game.CaseModuleComplete(0),game.CaseModuleComplete(1),game.CaseModuleComplete(2),game.CaseModuleComplete(3));
            if(snap){CaseOpened=true;caseLid.localRotation=Quaternion.Euler(105,0,0);PositionCaseParts(game,1);}
        }
        public void CaseAttemptSound(bool correct,bool moduleComplete,int module)
        {
            CaseAudio.Play(!correct?"retry":module==3?"release":moduleComplete?"complete":module==2?"step":"connect");
            if(!correct){caseRetry=.35f;retryModule=module;}
        }
        public void LiftMasterCover()
        {
            var game=Game;
            if(topic!=2||game==null||!game.Ready||MasterCoverOpen)return;
            MasterCoverOpen=true;CaseAudio.Play("cover");
        }
        void PositionCaseParts(Chapter4Game game,float blend)
        {
            exponentNeedle.localRotation=Quaternion.Slerp(exponentNeedle.localRotation,Quaternion.Euler(0,(dialAccepted?5:draftExponent)*40,0),blend);
            divisorNeedle.localRotation=Quaternion.Slerp(divisorNeedle.localRotation,Quaternion.Euler(0,(dialAccepted?5:draftDivisor)*40,0),blend);
            float pressure=gaugeAccepted?.12f:game.PressureHeld?.65f:Mathf.Clamp01(.35f+(600-game.CaseRemaining)/900);
            pressureNeedle.localRotation=Quaternion.Slerp(pressureNeedle.localRotation,Quaternion.Euler(0,Mathf.Lerp(-115,115,pressure),0),blend);
            releaseHandle.localRotation=Quaternion.Slerp(releaseHandle.localRotation,Quaternion.Euler(gaugeAccepted?60:draftRelease*5,0,0),blend);
            for(int i=0;i<4;i++){var p=caseCards[i].transform.position;p.y=Mathf.Lerp(p.y,cardAccepted[i]?1.91f:1.82f,blend);caseCards[i].transform.position=p;}
            caseLid.localRotation=Quaternion.Slerp(caseLid.localRotation,Quaternion.Euler(CaseOpened?105:0,0,0),blend);
            masterCover.localRotation=Quaternion.Slerp(masterCover.localRotation,Quaternion.Euler(MasterCoverOpen?110:0,0,0),blend);
        }
        void UpdateCaseFeedback()
        {
            var game=Game;if(game==null)return;
            bool paused=game.State=="Paused"||game.CanvasSupportOpen||game.PresentationBusy;
            CaseAudio.SetPaused(paused);
            if(paused)return;
            PositionCasePreview(1-Mathf.Exp(-Time.deltaTime*7));
            PositionCaseParts(game,1-Mathf.Exp(-Time.deltaTime*9));
            GetComponent<CaseShutdownSequence>()?.SyncStatus(game);
            SetCaseModules(game.CaseModuleComplete(0),game.CaseModuleComplete(1),game.CaseModuleComplete(2),game.CaseModuleComplete(3));
            if(retryModule>=0&&caseRetry>0)
            {
                caseRetry-=Time.deltaTime;
                // Only the selected unfinished module signals a retry; accepted lamps stay stable.
                bool done=game.CaseModuleComplete(retryModule);
                if(!done)indicators[retryModule].sharedMaterial=caseRetry>0?caseWarning:caseActive;
            }
        }
        void AnimateDeactivation(float t)
        {
            var sequence=GetComponent<CaseShutdownSequence>();
            if(!shutdownStarted&&t>=(sequence!=null?sequence.soundStartProgress:0)){shutdownStarted=true;CaseAudio.Play("shutdown");}
            if(sequence!=null){sequence.Sample(t);DeviceDeactivated=t>=1;if(waypoint!=null)waypoint.gameObject.SetActive(false);return;}
            exit.localRotation=Quaternion.Euler(Mathf.Lerp(0,65,Mathf.Clamp01(t*3)),0,0);
            for(int i=0;i<4;i++)powerBars[i].sharedMaterial=t>(i+1)*.19f?dark:caseActive;
            DeviceDeactivated=t>=1;
            // Show the whole physical case during the final action, not the former rising cube.
            var camera=player.GetComponentInChildren<Camera>();camera.cullingMask&=~(1<<2);camera.transform.position=new Vector3(0,5.2f,-3.5f);camera.transform.LookAt(new Vector3(0,1.65f,.8f));
            if(waypoint!=null)waypoint.gameObject.SetActive(false);
        }
#if UNITY_EDITOR
        public void EditorCaseCompletion(int modules,bool cover=false)
        {
            if(topic!=2)return;
            dialAccepted=modules>0;constantAccepted=modules>1;gaugeAccepted=modules>3;
            for(int i=0;i<3;i++){wireAccepted[i]=modules>1;caseWires[i].sharedMaterial=wireAccepted[i]?caseComplete:amber;}
            for(int i=0;i<4;i++){cardAccepted[i]=modules>2;caseCards[i].sharedMaterial=cardAccepted[i]?caseComplete:amber;var p=caseCards[i].transform.position;p.y=cardAccepted[i]?1.91f:1.82f;caseCards[i].transform.position=p;}
            constantSocket.sharedMaterial=constantAccepted?caseComplete:amber;SetCaseModules(dialAccepted,constantAccepted,modules>2,gaugeAccepted);
            exponentNeedle.localRotation=Quaternion.Euler(0,(dialAccepted?5:4)*40,0);divisorNeedle.localRotation=Quaternion.Euler(0,(dialAccepted?5:1)*40,0);
            pressureNeedle.localRotation=Quaternion.Euler(0,Mathf.Lerp(-115,115,gaugeAccepted?.12f:.35f),0);releaseHandle.localRotation=Quaternion.Euler(gaugeAccepted?60:0,0,0);
            masterCover.localRotation=Quaternion.Euler(cover?110:0,0,0);
            if(waypoint!=null)waypoint.gameObject.SetActive(modules<4);
            GetComponent<CaseShutdownSequence>()?.SetReadiness(modules,cover);
        }
#endif
    }
}
