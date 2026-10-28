namespace CalculusLabEscape
{
    public sealed partial class MissionCanvas
    {
        public void OnPracticeClose(){practicePanel.Close();Refresh();}
        public void OnPracticeOption(int index){practicePanel.Choose(index);Refresh();}
        public void OnPracticeSubmit(){practicePanel.Submit();Refresh();}
        public void OnPracticeNext(){practicePanel.Next();Refresh();}
        public void OnPracticeRepeat(){practicePanel.Repeat();Refresh();}
    }
}
