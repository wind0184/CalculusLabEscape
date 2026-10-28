using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace CalculusLabEscape
{
    public static class MathNotation
    {
        // Only recognise authored power notation, not arbitrary expressions or input answers.
        static readonly Regex Tokens=new Regex(@"(?<![A-Za-z])x\^(?:\((?<power>[+-]?(?:\d+(?:\.\d+)?(?:/\d+)?|n|□)(?:[+-]\d+)?)\)|(?<power>[+-]?(?:\d+(?:\.\d+)?|n|□))(?![\w/]|\.\d))|sqrt\(x(?:\^(?<rootPower>\d+))?\)",RegexOptions.CultureInvariant);
        static void Literal(StringBuilder output,string text,int start,int count)
        {
            for(int i=start;i<start+count;i++)
                // Keep saved learner text visible; never interpret its TMP tags.
                if(text[i]=='<')output.Append("<noparse><</noparse>");else output.Append(text[i]);
        }
        public static string Format(string source)
        {
            if(string.IsNullOrEmpty(source))return source??"";
            var output=new StringBuilder(source.Length+32);int cursor=0;
            foreach(Match match in Tokens.Matches(source))
            {
                Literal(output,source,cursor,match.Index-cursor);
                if(match.Value=="sqrt(x)")output.Append('\u221a').Append('x');
                else if(match.Groups["rootPower"].Success)output.Append("√(x<sup><size=140%>").Append(match.Groups["rootPower"].Value).Append("</size></sup>)");
                else output.Append("x<sup><size=140%>").Append(match.Groups["power"].Value).Append("</size></sup>");
                cursor=match.Index+match.Length;
            }
            Literal(output,source,cursor,source.Length-cursor);return output.ToString();
        }
    }
    [DisallowMultipleComponent,RequireComponent(typeof(TMP_Text))]
    public sealed class MathText:MonoBehaviour
    {
        [SerializeField,TextArea] string source="";
        TMP_Text label;
        string rendered;
        public string Source=>source;
        void Awake(){SetSource(source);}
        public void SetSource(string value)
        {
            if(label==null)label=GetComponent<TMP_Text>();value=value??"";
            if(value==source&&rendered!=null&&label.text==rendered)return;
            source=value;rendered=MathNotation.Format(value);label.richText=true;label.text=rendered;
        }
        public static void Set(TMP_Text label,string value)
        {
            var math=label.GetComponent<MathText>();if(math!=null)math.SetSource(value);else label.text=value;
        }
    }
}
