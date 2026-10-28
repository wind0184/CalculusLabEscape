using UnityEngine;

namespace CalculusLabEscape
{
    public abstract class Chapter4UI : MonoBehaviour
    {
        protected GUIStyle title,heading,body,small,button,field;
        GUIStyle panelStyle,sliderStyle,thumbStyle;
        protected readonly Color cyan=new Color(.16f,.18f,.19f),muted=new Color(.36f,.38f,.39f),green=new Color(.12f,.36f,.22f);
        protected readonly Color paper=new Color(.96f,.96f,.94f),ink=new Color(.16f,.18f,.19f);
        protected void BeginUI()
        {
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2),Quaternion.identity,Vector3.one*scale);
            if(title!=null)return;
            title=Style(30,ink,true);heading=Style(19,ink,true);body=Style(18,ink);small=Style(14,muted);
            button=new GUIStyle(GUI.skin.button){fontSize=15,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(12,12,5,5),border=new RectOffset(2,2,2,2)};
            button.normal.background=Surface(paper);button.hover.background=Surface(new Color(.83f,.86f,.86f));button.active.background=Surface(new Color(.72f,.77f,.77f));button.focused.background=button.hover.background;
            button.normal.textColor=button.hover.textColor=button.active.textColor=button.focused.textColor=ink;
            field=new GUIStyle(GUI.skin.textField){fontSize=21,padding=new RectOffset(10,10,8,5),border=new RectOffset(2,2,2,2)};
            field.normal.background=Surface(Color.white);field.focused.background=Surface(new Color(.91f,.95f,.94f));field.hover.background=field.normal.background;
            field.normal.textColor=field.focused.textColor=field.hover.textColor=ink;
            panelStyle=new GUIStyle{border=new RectOffset(2,2,2,2)};panelStyle.normal.background=Surface(paper);
            sliderStyle=new GUIStyle(GUI.skin.horizontalSlider){fixedHeight=6,border=new RectOffset(2,2,2,2)};sliderStyle.normal.background=Surface(new Color(.8f,.81f,.8f));
            thumbStyle=new GUIStyle(GUI.skin.horizontalSliderThumb){fixedWidth=18,fixedHeight=18,border=new RectOffset(2,2,2,2),overflow=new RectOffset(0,0,6,6)};
            thumbStyle.normal.background=thumbStyle.hover.background=thumbStyle.active.background=Surface(Color.white);
        }
        GUIStyle Style(int size,Color c,bool bold=false){var s=new GUIStyle(GUI.skin.label){fontSize=size,wordWrap=true,fontStyle=bold?FontStyle.Bold:FontStyle.Normal};s.normal.textColor=c;return s;}
        Texture2D Surface(Color fill){var t=new Texture2D(8,8){filterMode=FilterMode.Point,hideFlags=HideFlags.HideAndDontSave};for(int y=0;y<8;y++)for(int x=0;x<8;x++)t.SetPixel(x,y,x==0||y==0||x==7||y==7?ink:fill);t.Apply();return t;}
        protected void Box(float x,float y,float w,float h,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);GUI.color=old;}
        protected void Panel(float x,float y,float w,float h)=>GUI.Box(new Rect(x,y,w,h),GUIContent.none,panelStyle);
        protected void Label(float x,float y,float w,float h,string text,GUIStyle style=null)=>GUI.Label(new Rect(x,y,w,h),text,style??body);
        protected void Center(float x,float y,float w,float h,string text,GUIStyle style=null)=>GUI.Label(new Rect(x,y,w,h),text,new GUIStyle(style??body){alignment=TextAnchor.MiddleCenter});
        protected bool Button(float x,float y,float w,float h,string text,bool enabled=true)
        {
            bool old=GUI.enabled;GUI.enabled=old&&enabled;int size=button.fontSize;
            while(button.fontSize>11&&button.CalcSize(new GUIContent(text)).x>w-12)button.fontSize--;
            bool hit=GUI.Button(new Rect(x,y,w,h),text,button);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(GUI.enabled&&Event.current.type==EventType.Repaint&&Chapter4Playtest.Active&&Chapter4Playtest.Click==text){hit=true;Chapter4Playtest.Click=null;}
#endif
            button.fontSize=size;GUI.enabled=old;return hit;
        }
        protected string Field(float x,float y,float w,string value)=>GUI.TextField(new Rect(x,y,w,42),value,32,field);
        protected float Slider(Rect r,float v,float min,float max)=>GUI.HorizontalSlider(r,v,min,max,sliderStyle,thumbStyle);
        protected void Line(Vector2 a,Vector2 b,Color c,float width=2)
        {
            var matrix=GUI.matrix;var color=GUI.color;
            try
            {
                // Compose in the same 1280x720 local coordinates as the rest of the UI.
                // Rotating a screen-space pivot after scaling displaces ring segments at other resolutions.
                GUI.matrix=matrix*Matrix4x4.TRS(new Vector3(a.x,a.y,0),
                    Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg),Vector3.one);
                GUI.color=c;
                GUI.DrawTexture(new Rect(0,-width/2,Vector2.Distance(a,b),width),Texture2D.whiteTexture);
            }
            finally{GUI.matrix=matrix;GUI.color=color;}
        }
        protected void Ring(float x,float y,float r,Color color,int segments=64){for(int i=0;i<segments;i++){float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;Line(new Vector2(x+Mathf.Cos(a)*r,y+Mathf.Sin(a)*r),new Vector2(x+Mathf.Cos(b)*r,y+Mathf.Sin(b)*r),color);}}
        protected void Point(float x,float y,Color color,bool open=false){Box(x-5,y-5,10,10,color);if(open)Box(x-3,y-3,6,6,paper);}
        protected void HeartRow(float x,float y,int remaining)
        {
            for(int i=0;i<3;i++){float px=x+i*31;Color c=i<remaining?ink:new Color(.72f,.72f,.72f);Vector2[] pts={new Vector2(px,y+7),new Vector2(px+5,y),new Vector2(px+11,y+4),new Vector2(px+17,y),new Vector2(px+23,y+7),new Vector2(px+11,y+21),new Vector2(px,y+7)};for(int j=1;j<pts.Length;j++)Line(pts[j-1],pts[j],c,2);}
        }
        protected void PortalSketch(float x,float y,float r,int progress)
        {
            Ring(x,y,r,ink);Ring(x,y,r-7,muted);
            for(int i=0;i<5;i++){float a=(i*72-90)*Mathf.Deg2Rad;float px=x+Mathf.Cos(a)*(r+16),py=y+Mathf.Sin(a)*(r+16);Ring(px,py,6,ink,16);if(i<progress)Box(px-3,py-3,6,6,ink);}
            for(int i=0;i<progress;i++){float a=(i*72-90)*Mathf.Deg2Rad,b=((i+1)*72-90)*Mathf.Deg2Rad;for(int j=0;j<10;j++){float aa=Mathf.Lerp(a,b,j/10f),bb=Mathf.Lerp(a,b,(j+1)/10f);Line(new Vector2(x+Mathf.Cos(aa)*(r-13),y+Mathf.Sin(aa)*(r-13)),new Vector2(x+Mathf.Cos(bb)*(r-13),y+Mathf.Sin(bb)*(r-13)),ink,5);}}
        }
        protected void BridgeSketch(float x,float y,float w,int parts)
        {
            for(int i=0;i<11;i++){if(i>3&&i<7&&parts<3)continue;float px=x+i*w/11;Line(new Vector2(px,y+45),new Vector2(px+w/11-3,y+20),ink,3);}
            for(int i=0;i<4;i++){float px=x+i*w/3;Line(new Vector2(px,y),new Vector2(px,y+75),ink,4);Ring(px,y,5,ink,12);}
            Line(new Vector2(x,y+5),new Vector2(x+w,y+5),ink,2);Line(new Vector2(x,y+15),new Vector2(x+w,y+15),muted,1);
            for(int i=0;i<6;i++)Line(new Vector2(x+i*w/6,y+91),new Vector2(x+i*w/6+25,y+87),muted,1);
        }
        protected void CaseSketch(float x,float y,float w,int complete)
        {
            Panel(x,y,w,120);Panel(x+8,y+8,w-16,35);Center(x+12,y+12,w-24,24,"INTEGRATION MANUAL",small);
            for(int i=0;i<4;i++){Panel(x+10+i*(w-20)/4,y+52,(w-24)/4,40);Center(x+10+i*(w-20)/4,y+55,(w-24)/4,33,new[]{"n+1","wires","1 2 3","F(b)"}[i],small);}
            Center(x+15,y+95,w-30,21,"MODULES "+complete+"/4   |   MASTER SWITCH",small);
        }
        protected void ConsoleFrame(string caption)
        {
            Box(0,0,1280,720,new Color(.87f,.88f,.86f));Panel(24,20,1232,672);Panel(88,37,1104,614);
            Center(120,51,1040,45,"CALCULUS LAB ESCAPE",title);Center(160,103,960,33,caption,heading);
            for(int i=0;i<3;i++){Ring(52,200+i*30,6,ink,16);Box(1231,188+i*20,9,9,ink);}
            Panel(36,91,40,60);Line(new Vector2(40,132),new Vector2(49,111),ink);Line(new Vector2(49,111),new Vector2(58,133),ink);Line(new Vector2(58,133),new Vector2(71,106),ink);
            Ring(1225,119,20,ink);Line(new Vector2(1225,119),new Vector2(1231,104),ink);Line(new Vector2(1225,119),new Vector2(1216,117),ink);
            for(int y=472;y<536;y+=9)Line(new Vector2(38,y),new Vector2(72,y),ink,2);
            Panel(1215,443,23,115);Box(1222,456,8,87,ink);Box(1213,493,27,9,paper);
            for(int i=0;i<9;i++)Panel(150+i*56,664,42,10);Ring(950,670,12,ink);Ring(991,670,12,ink);
        }
    }
}
