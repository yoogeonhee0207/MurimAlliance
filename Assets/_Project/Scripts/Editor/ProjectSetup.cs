using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace MurimAlliance.EditorTools
{
    /// <summary>
    /// 천하무림맹 프로젝트 기본 설정을 한 번에 적용하는 에디터 메뉴.
    /// 메뉴: Tools > 천하무림맹 > Player Settings 적용
    /// </summary>
    public static class ProjectSetup
    {
        const string CompanyName = "yoo";
        const string ProductName = "천하무림맹";
        const string Version = "0.1.0";
        const string PackageName = "com.yoo.murimalliance";

        [MenuItem("Tools/천하무림맹/Player Settings 적용")]
        public static void ApplyPlayerSettings()
        {
            // 기본 정보
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);

            // 가로 화면만 허용 (뒤집으면 따라 회전)
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // 구글 플레이 요구사항: IL2CPP + ARM64
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // 에디터에서 Unity 창이 뒤에 있어도 Play가 멈추지 않게 (안드로이드에서는 무시되는 설정)
            PlayerSettings.runInBackground = true;

            AssetDatabase.SaveAssets();
            Debug.Log($"[ProjectSetup] Player Settings 적용 완료: {ProductName} / {PackageName} / v{Version}");
        }
    }
}
