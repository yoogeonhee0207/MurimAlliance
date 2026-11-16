using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MurimAlliance.Hunt
{
    /// <summary>
    /// 오른쪽 아래 모서리의 공격 버튼. 누를 때마다 맹주가 한 대 때리고(공격 간격보다 빠르지는 않음),
    /// 누르고 있으면 간격마다 계속 때린다. 누르고 있는 동안 버튼이 살짝 어두워진다.
    /// 자동사냥이 켜져 있을 때는 자동사냥이 공격을 맡으므로 버튼은 영향이 없다.
    /// </summary>
    public class AttackButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Image background;

        static readonly Color Normal = new Color(0.55f, 0.16f, 0.14f, 0.9f);
        static readonly Color Pressed = new Color(0.38f, 0.1f, 0.09f, 0.95f);

        LeaderBrain brain;

        public void Bind(LeaderBrain leaderBrain) => brain = leaderBrain;

        void Awake()
        {
            if (background != null) background.color = Normal;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (brain != null)
            {
                brain.RequestAttack();
                brain.AttackHeld = true;
            }
            if (background != null) background.color = Pressed;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (brain != null) brain.AttackHeld = false;
            if (background != null) background.color = Normal;
        }
    }
}
