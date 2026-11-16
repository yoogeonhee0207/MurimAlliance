using MurimAlliance.Data;
using UnityEngine;

namespace MurimAlliance.Hunt
{
    /// <summary>
    /// 사냥터의 구역 하나(예: 입구·숲길·깊은 숲). 월드 좌표 사각형으로 영역을 정한다.
    /// 몬스터 생성과 자동사냥이 "지금 어느 구역인가"를 판단할 때 쓴다.
    /// </summary>
    public class HuntZone : MonoBehaviour
    {
        [Tooltip("구역 이름 (화면 표시용)")]
        public string zoneName = "구역";

        [Tooltip("구역 영역 (월드 좌표, 왼쪽 아래 x·y + 폭·높이)")]
        public Rect area = new Rect(0, 0, 15, 25);

        [Header("몬스터 생성")]
        [Tooltip("이 구역에 나오는 몬스터 (무작위로 고름)")] public UnitData[] monsters;
        [Tooltip("이 구역에 동시에 있을 수 있는 최대 마릿수")] public int maxMonsters = 6;
        [Tooltip("켜면 선공: 가까이 온 적을 먼저 공격")] public bool aggressive;

        [Tooltip("씬 화면에서 영역을 표시할 색")]
        public Color gizmoColor = new Color(1f, 0.8f, 0.2f, 1f);

        public bool Contains(Vector2 p) => area.Contains(p);

        /// <summary>구역 안의 무작위 위치 (가장자리에서 margin만큼 안쪽).</summary>
        public Vector2 RandomPoint(float margin = 1f)
        {
            return new Vector2(
                Random.Range(area.xMin + margin, area.xMax - margin),
                Random.Range(area.yMin + margin, area.yMax - margin));
        }

        void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Vector3 c = area.center;
            Gizmos.DrawWireCube(c, new Vector3(area.width, area.height, 0));
#if UNITY_EDITOR
            UnityEditor.Handles.Label(new Vector3(area.xMin + 0.3f, area.yMax - 0.3f, 0), zoneName);
#endif
        }
    }
}
