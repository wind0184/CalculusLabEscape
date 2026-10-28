#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static class ButtonSoundCoverageAuthoring
    {
        public static void Audit()
        {
            string folder=LearningApi.Argument("--button-audio-evidence");Directory.CreateDirectory(folder);
            var lines=new List<string>{"Scene,Button,Path,Audio Component,Audio Calls,Audio First,Enabled Call"};int failures=0,total=0;
            foreach(string name in new[]{"MainMenu"}.Concat(Chapter4Game.Scenes)){
                var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+name+".unity");
                foreach(var button in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Button>(true))){
                    total++;var audio=button.GetComponent<ButtonAudio>();var calls=new List<int>();
                    for(int i=0;i<button.onClick.GetPersistentEventCount();i++)if(button.onClick.GetPersistentTarget(i)==audio&&button.onClick.GetPersistentMethodName(i)=="Play")calls.Add(i);
                    bool enabled=calls.Count==1&&button.onClick.GetPersistentListenerState(calls[0])!=UnityEventCallState.Off;
                    bool first=calls.Count==1&&calls[0]==0;
                    if(audio==null||!first||!enabled)failures++;
                    string path=button.name;for(var parent=button.transform.parent;parent!=null;parent=parent.parent)path=parent.name+"/"+path;
                    lines.Add(string.Join(",",new[]{name,button.name,path,audio!=null?"yes":"no",calls.Count.ToString(),first.ToString(),enabled.ToString()}.Select(v=>"\""+v.Replace("\"","\"\"")+"\"")));
                }
            }
            File.WriteAllLines(Path.Combine(folder,"saved-button-audit.csv"),lines);
            File.WriteAllText(Path.Combine(folder,"saved-button-audit.txt"),$"Buttons: {total}; invalid sound bindings: {failures}\n");
            if(failures>0)throw new Exception("Button sound binding audit failed: "+failures);
            Debug.Log("BUTTON_SOUND_BINDING_AUDIT_PASS / "+total);
        }
    }
}
#endif
