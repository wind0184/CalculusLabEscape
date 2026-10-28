namespace CalculusLabEscape
{
    public sealed partial class MissionCanvas
    {
        PracticePanel practicePanel;
        void SetupPractice()
        {
            practicePanel=new PracticePanel(Text,Show,Enabled,CloseModal,"Back to mission results");
            
        }
    }
}
