using System.Collections.Generic;
using UnityEngine;

namespace MurimAlliance.Hunt
{
    /// <summary>
    /// 사냥터 맵 하나의 정보: 전체 경계, 구역 목록, 맹주가 처음 서는 위치.
    /// 카메라가 맵 밖을 보여 주지 않게 막거나, 위치로 구역을 찾을 때 쓴다.
    /// </summary>
    public class HuntMap : MonoBehaviour
    {
        public static HuntMap Current { get; private set; }

        [Tooltip("맵 전체 경계 (월드 좌표)")]
        public Rect bounds = new Rect(0, 0, 45, 25);

        [Tooltip("맹주가 처음 서는 위치 (입구)")]
        public Transform spawnPoint;

        [Tooltip("구역 목록 (왼쪽부터)")]
        public List<HuntZone> zones = new List<HuntZone>();

        void Awake() => Current = this;
        void OnDestroy() { if (Current == this) Current = null; }

        /// <summary>위치가 속한 구역. 어느 구역에도 없으면 null.</summary>
        public HuntZone ZoneAt(Vector2 p)
        {
            foreach (var z in zones)
                if (z != null && z.Contains(p)) return z;
            return null;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(bounds.center, new Vector3(bounds.width, bounds.height, 0));
        }
    }
}
