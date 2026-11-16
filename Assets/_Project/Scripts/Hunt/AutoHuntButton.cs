using UnityEngine;
using UnityEngine.UI;

namespace MurimAlliance.Hunt
{
    /// <summary>
    /// 오른쪽 아래 자동사냥 켜기·끄기 버튼. 누르면 맹주의 자동사냥을 바꾸고,
    /// 켜져 있으면 밝은 금색, 꺼져 있으면 어두운 색으로 표시한다.
    /// 맹주는 실행 중에 만들어지므로 생성기가 Bind로 연결한다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class AutoHuntButton : MonoBehaviour
    {
        public Image background;
        public Text label;

        static readonly Color OnColor = new Color(0.85f, 0.68f, 0.25f, 0.95f);
        static readonly Color OffColor = new Color(0.12f, 0.12f, 0.14f, 0.75f);

        LeaderBrain brain;

        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() => { if (brain != null) brain.ToggleAutoHunt(); });
            Refresh(false);
        }

        public void Bind(LeaderBrain leaderBrain)
        {
            if (brain != null) brain.AutoHuntChanged -= Refresh;
            brain = leaderBrain;
            brain.AutoHuntChanged += Refresh;
            Refresh(brain.autoHunt);
        }

        void OnDestroy()
        {
            if (brain != null) brain.AutoHuntChanged -= Refresh;
        }

        void Refresh(bool on)
        {
            if (background != null) background.color = on ? OnColor : OffColor;
            if (label != null)
            {
                label.text = on ? "자동\nON" : "자동\nOFF";
                label.color = on ? new Color(0.15f, 0.1f, 0.05f) : new Color(0.85f, 0.85f, 0.8f);
            }
        }
    }
}
