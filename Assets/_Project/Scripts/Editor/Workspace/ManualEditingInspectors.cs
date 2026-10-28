#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CalculusLabEscape.Editor
{
    [CustomEditor(typeof(UIReferenceMap))]
    public sealed class UIReferenceMapInspector : UnityEditor.Editor
    {
        string filter="";
        bool show;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var entries=serializedObject.FindProperty("elements");
            EditorGUILayout.HelpBox("这里保存 UI 对象的实际引用。左边是代码使用的固定 ID，右边可以拖入替换对象。重命名已绑定的按钮不会切断引用。",MessageType.Info);
            filter=EditorGUILayout.TextField("查找 ID / 对象",filter);
            show=EditorGUILayout.Foldout(show,"已保存引用（"+entries.arraySize+"）",true);
            if(show||!string.IsNullOrEmpty(filter))
            {
                for(int i=0;i<entries.arraySize;i++)
                {
                    var row=entries.GetArrayElementAtIndex(i);
                    var id=row.FindPropertyRelative("id");var value=row.FindPropertyRelative("target");
                    string name=value.objectReferenceValue!=null?value.objectReferenceValue.name:"Missing";
                    if(!string.IsNullOrEmpty(filter)&&!(id.stringValue+" "+name).ToLowerInvariant().Contains(filter.ToLowerInvariant()))continue;
                    EditorGUILayout.PropertyField(value,new GUIContent(id.stringValue));
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(GameManager))]
    public sealed class GameManagerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("从这里认识本场景：World 是三维关卡，UI 是界面，Event System 负责 UI 输入。场景物体和界面已经保存在 Hierarchy；Play 只启动游戏规则。",MessageType.Info);
            DrawDefaultInspector();
            var controller=(GameManager)target;
            if(controller.world!=null&&GUILayout.Button("选择玩家：移动 / E 键交互"))Selection.activeGameObject=controller.world.player.gameObject;
            var ui=controller.missionUI!=null?controller.missionUI.gameObject:controller.menuOrLimitsUI!=null?controller.menuOrLimitsUI.gameObject:null;
            if(ui!=null&&GUILayout.Button("选择界面：布局 / 引用 / 按钮"))Selection.activeGameObject=ui;
            if(controller.world!=null&&GUILayout.Button("选择三维场景：设备 / 桥梁 / 环境"))Selection.activeGameObject=controller.world.gameObject;
            if(Application.isPlaying&&controller.Game!=null)EditorGUILayout.LabelField("当前状态",controller.Game.State);
        }
    }
}
#endif
