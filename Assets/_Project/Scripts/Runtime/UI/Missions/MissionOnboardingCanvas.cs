using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CalculusLabEscape
{
    public sealed partial class MissionCanvas
    {
        public MissionOnboarding onboarding;
        string introductionKey;
        Vector3 guideStart;
        bool guideStarted;
        MissionGuideZone[] guideZones;
        ExplorationHUD explorationHUD;

        void SetupOnboarding()
        {
            explorationHUD=GetComponent<ExplorationHUD>();
            guideZones=Game.World.GetComponentsInChildren<MissionGuideZone>(true);
            
        }

        string CurrentIntroduction()
        {
            if(topic==1)return Game.Ready?"bridge-rescue":Game.BridgeExtension!=null?(Game.BridgeExtension.choiceAnswer?"bridge-choices":"bridge-numbers"):Game.Stage==0?"bridge-power":Game.Stage==7||Game.Stage==10?"bridge-choices":"bridge-numbers";
            return caseOverview?Game.Ready?"case-shutdown":"case-overview":new[]{"case-dial","case-wires","case-sequence","case-pressure"}[Game.Module];
        }

        void OpenIntroduction(string key)
        {
            var card=onboarding.Find(key);if(card==null)return;
            EndDrag();introductionKey=key;modal="Introduction";Game.CanvasOpenSupport(true);
            Text("IntroductionTitle",card.title);Text("IntroductionBody",card.instructions);
            Show("IntroductionModal",true);Show("WorldGuide",false);
        }

        void FinishIntroduction(bool skip)
        {
            if(introductionKey==null)return;
            if(!Game.Session.guideSeen.Contains(introductionKey))Game.Session.guideSeen.Add(introductionKey);
            if(skip)Game.Session.guideSkipped=true;
            introductionKey=null;CloseModal();Game.CanvasSaveGuide();
        }

        void RefreshOnboarding()
        {
            if(onboarding==null)return;
            if(sceneController!=null&&sceneController.robotDialogue!=null&&sceneController.robotDialogue.IsOpen){Show("WorldGuide",false);return;}
            if(Game.Session.guideSeen==null)Game.Session.guideSeen=new List<string>();
            bool explore=Game.State=="Explore";
            if(explore&&!guideStarted){guideStart=Game.World.player.transform.position;guideStarted=true;}
            if(explore&&!Game.Session.guideMoved)
            {
                var delta=Game.World.player.transform.position-guideStart;delta.y=0;
                if(delta.sqrMagnitude>=.64f){Game.Session.guideMoved=true;Game.CanvasSaveGuide();}
            }
            if(explore&&Keyboard.current!=null&&Keyboard.current.gKey.wasPressedThisFrame)
            {Game.Session.guideSkipped=!Game.Session.guideSkipped;Game.CanvasSaveGuide();}
            Text("GuidePreferenceLabel","First-use guides: "+(Game.Session.guideSkipped?"OFF / click to enable":"ON / click to hide"));
            bool show=explore&&!Game.Session.guideSkipped;
            bool robotNearby=explore&&sceneController!=null&&sceneController.robotDialogue!=null&&sceneController.robotDialogue.CanTalk;
            Show("WorldGuide",show);
            if(show)
            {
                var zone=guideZones.FirstOrDefault(z=>z.target==Game.World.station);
                bool near=Game.CanvasNearStation&&(zone==null||zone.Contains(Game.World.player.transform.position));
                string device=zone!=null?zone.deviceName:topic==1?Chapter4World.RepairDeviceNames[Game.Stage==0?0:Game.Stage<4?1:2]:"Containment case";
                Text("WorldGuideTitle",robotNearby?"TALK / LABORATORY AI":near?"READY TO INSPECT / "+device:!Game.Session.guideMoved?"GET MOVING":"FIND / "+device);
                Text("WorldGuideBody",robotNearby?"Press E to talk to your laboratory guide.":near?"Press E to inspect. Your mouse becomes available for the device controls.":!Game.Session.guideMoved?"WASD or arrow keys move. Mouse looks around. Walk towards the cyan device marker.":"Follow the cyan marker and the direction prompt below. Move closer until [E] appears.");
            }
            if(explorationHUD!=null)explorationHUD.Present(Game.Session.guideMoved,Game.CanvasNearStation||robotNearby,topic==1?Game.BridgeParts:Game.ModulesComplete,robotNearby);
            if(Game.State!="Puzzle"||modal!=null||Game.Session.guideSkipped||Game.CanvasAccepted)return;
            string key=CurrentIntroduction();
            if(!Game.Session.guideSeen.Contains(key))OpenIntroduction(key);
        }
    }
}
