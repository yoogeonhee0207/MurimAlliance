using System.Collections.Generic;
using MurimAlliance.Data;
using UnityEditor;
using UnityEngine;

namespace MurimAlliance.EditorTools
{
    /// <summary>
    /// 유닛 데이터 Inspector를 한글 항목 이름으로 보여 준다.
    /// 각 칸은 Unity 기본 그리기(PropertyField)를 그대로 써서 구역 제목, 슬라이더, 최소값 제한이 유지된다.
    /// 마우스를 올리면 설명과 코드 변수명이 함께 보인다.
    /// </summary>
    [CustomEditor(typeof(UnitData))]
    public class UnitDataEditor : Editor
    {
        static readonly Dictionary<string, string> Labels = new Dictionary<string, string>
        {
            { "displayName", "이름" },
            { "faction", "문파" },
            { "role", "역할" },
            { "personality", "성격 유형" },
            { "grade", "등급" },
            { "sprite", "그림" },
            { "shadowSize", "그림자 크기" },
            { "isBoss", "보스" },
            { "maxHp", "최대 체력" },
            { "attack", "공격력" },
            { "defense", "방어력" },
            { "attackInterval", "공격 간격(초)" },
            { "range", "사거리(칸)" },
            { "moveSpeed", "이동 속도(칸/초)" },
            { "critChance", "치명타 확률(0~1)" },
            { "critMultiplier", "치명타 배율" },
            { "attackType", "공격 방식" },
            { "areaRadius", "범위 반경(칸)" },
            { "poisonDamagePerSecond", "중독 피해(초당)" },
            { "poisonDuration", "중독 시간(초)" },
            { "healAmount", "회복량" },
            { "shieldAmount", "보호막량" },
            { "counterChance", "반격 확률(0~1)" },
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var prop = serializedObject.GetIterator();
            bool enterChildren = true;
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.name == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(prop);
                    continue;
                }

                var label = Labels.TryGetValue(prop.name, out var kor) ? kor : prop.displayName;
                var tip = string.IsNullOrEmpty(prop.tooltip) ? prop.name : $"{prop.tooltip}\n({prop.name})";
                EditorGUILayout.PropertyField(prop, new GUIContent(label, tip), true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
