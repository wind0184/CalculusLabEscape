#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CalculusLabEscape.Editor
{
    public static partial class ProductionUIBuilder
    {
        static void MakeAccountPages(Transform pages,Transform home)
        {
            Button(home,"HomeLogout","Sign out",1000,677,222,32);
            var login=Page(pages,"Login");
            Text(login,"LoginKicker","SECURE ACCESS / LEARNING LAB",55,127,650,30,16,Accent,true);
            Text(login,"LoginTitle","Enter the lab.\nBuild your understanding.",55,187,644,134,42,Ink,true);
            Text(login,"LoginIntro","Learn through exploration, evidence and feedback.\n\nStudents: complete missions and review your progress.\nTeachers: inspect attempts and identify learning gaps.",57,347,601,156,21,Muted);
            Diagram(login,"LoginPortal",129,501,365,171,true);
            var card=Panel(login,"LoginCard",752,113,473,558);
            Text(card.transform,"LoginFormTitle","Account access",28,23,415,43,29,Ink,true);
            Text(card.transform,"UsernameLabel","USERNAME",29,88,410,25,15,Muted,true);
            Input(card.transform,"LoginUsername","Letters, numbers, _ . -",29,121,415,44).characterLimit=32;
            Text(card.transform,"PasswordLabel","PASSWORD / 12-128 CHARACTERS",29,185,415,25,15,Muted,true);
            var password=Input(card.transform,"LoginPassword","Your password",29,217,415,44);password.characterLimit=128;password.contentType=TMP_InputField.ContentType.Password;
            Button(card.transform,"LoginSubmit","Sign in",29,284,415,48,true);
            Button(card.transform,"RegisterStudent","Create student account",29,345,415,43);
            Button(card.transform,"SetupTeacher","Set up first teacher",29,401,415,43);
            Text(card.transform,"LoginStatus","Connecting...",29,460,415,85,16,Muted);
            Button(login,"LoginExit","Exit",56,675,132,32);

            var teacher=Page(pages,"Teacher");
            Text(teacher,"TeacherTitle","Learning evidence",49,113,1000,53,37,Ink,true);
            Text(teacher,"TeacherIntro","TEACHER WORKSPACE / FILTER SESSIONS AND REVIEW ATTEMPTS",51,177,1170,30,16,Accent,true);
            var filters=Panel(teacher,"TeacherFilters",49,232,1180,177);
            Text(filters.transform,"StudentFilterLabel","STUDENT",22,18,320,26,15,Muted,true);
            Input(filters.transform,"FilterStudent","Search username",22,51,340,43);
            Text(filters.transform,"TopicFilterLabel","TOPIC",391,18,320,26,15,Muted,true);
            Dropdown(filters.transform,"FilterTopic",new[]{"All topics","Limits","Differentiation","Integration"},391,51,352);
            Text(filters.transform,"StatusFilterLabel","STATUS",772,18,360,26,15,Muted,true);
            Dropdown(filters.transform,"FilterStatus",new[]{"All statuses","In progress","Completed","Abandoned"},772,51,384);
            Dropdown(filters.transform,"FilterStep",new[]{"All questions","L1 / Numerical","L2 / Graph","L3 / One-sided","L4 / Point value","L5 / Conclusion"},22,116,340);
            Dropdown(filters.transform,"FilterError",new[]{"All error categories","Numerical approach","Graph limit","One-sided mismatch","Limit vs. function value","Unequal sides / DNE"},391,116,352);
            Button(filters.transform,"TeacherSearch","Apply filters",772,116,384,43,true);
            Text(teacher,"TeacherSummary","Loading...",53,438,620,112,25);
            Button(teacher,"TeacherView","View sessions",834,439,391,48,true);
            Button(teacher,"TeacherExport","Export results as CSV",834,503,391,43);
            Text(teacher,"TeacherStatus","",53,564,1174,59,16,Muted);
            Button(teacher,"TeacherStudents","Student accounts",52,648,283,43);
            Button(teacher,"TeacherLogout","Sign out",981,648,244,43);
            var students=Page(pages,"Students");Text(students,"StudentsTitle","Student accounts",52,114,1160,55,37,Ink,true);
            Text(students,"StudentsIntro","ACCOUNT ID / USERNAME",54,189,1140,28,16,Accent,true);
            var scroll=Panel(students,"StudentsScroll",53,235,1170,372);scroll.gameObject.AddComponent<RectMask2D>();var sr=scroll.gameObject.AddComponent<ScrollRect>();sr.horizontal=false;sr.movementType=ScrollRect.MovementType.Clamped;sr.viewport=scroll.rectTransform;
            var content=Rect(scroll.transform,"StudentsContent",18,12,1128,344);sr.content=content;
            var list=Text(content,"StudentsList","",0,0,1128,344,23);list.overflowMode=TextOverflowModes.Overflow;
            var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.childControlHeight=true;layout.childControlWidth=true;layout.childForceExpandHeight=false;content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            Button(students,"StudentsBack","Back to teacher workspace",53,648,388,43);
        }
        static TMP_Dropdown Dropdown(Transform parent,string name,string[] options,float x,float y,float width)
        {
            var image=Panel(parent,name,x,y,width,43,Paper);var dropdown=image.gameObject.AddComponent<TMP_Dropdown>();dropdown.targetGraphic=image;
            dropdown.captionText=Text(image.transform,name+"Caption",options[0],12,6,width-48,31,18);
            Text(image.transform,name+"Arrow","v",width-29,7,22,28,18,Accent);
            var template=Panel(image.transform,name+"Template",0,45,width,210,White).rectTransform;
            var scroll=template.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var viewport=Rect(template,name+"Viewport",0,0,width,210);viewport.gameObject.AddComponent<RectMask2D>();scroll.viewport=viewport;
            var content=Rect(viewport,name+"Content",0,0,width,38);scroll.content=content;
            var item=Panel(content,name+"Item",0,0,width,38,Paper);var toggle=item.gameObject.AddComponent<Toggle>();toggle.targetGraphic=item;
            toggle.graphic=Panel(item.transform,name+"Selected",3,4,3,30,Accent);
            var label=Text(item.transform,name+"Option","",12,5,width-23,30,17);dropdown.itemText=label;
            dropdown.template=template;dropdown.options=new List<TMP_Dropdown.OptionData>();foreach(var option in options)dropdown.options.Add(new TMP_Dropdown.OptionData(option));
            template.gameObject.SetActive(false);dropdown.RefreshShownValue();return dropdown;
        }
    }
}
#endif
