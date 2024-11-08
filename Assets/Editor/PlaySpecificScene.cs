using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PlaySpecificScene : EditorWindow
{
    [SerializeField]
    SceneAsset playScene = null;

    [MenuItem("Window/PlaySpecificScene")]
    public static void ShowWindow()
    {
        GetWindow(typeof(PlaySpecificScene));
    }

    void OnGUI()
    {
        titleContent.text = "PlaySpecificScene";
        EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
        playScene = (SceneAsset)EditorGUILayout.ObjectField(
                    new GUIContent("Scene"),
                    playScene,
                    typeof(SceneAsset),
                    false);

        EditorGUILayout.HelpBox("���� �������� ������ ���� �����ִ� ���� ���� �մϴ�",
            MessageType.Info);

        if (GUILayout.Button("Play"))
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

                if (playScene != null)
                {
                    string scene = AssetDatabase.GetAssetPath(playScene);
                    EditorSceneManager.OpenScene(scene);
                }

                EditorApplication.isPlaying = true;
            }
        }
        EditorGUI.EndDisabledGroup();
    }

    void OnInspectorUpdate()
    {
        Repaint();
    }
}