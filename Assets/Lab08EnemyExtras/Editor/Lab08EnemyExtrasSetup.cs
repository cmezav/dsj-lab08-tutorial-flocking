using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class Lab08EnemyExtrasSetup
{
    [MenuItem("Lab08/Agregar panel P y marca de destino")]
    static void Setup()
    {
        if(EditorApplication.isPlaying){Debug.LogWarning("Sal de Play primero.");return;}
        if(Object.FindAnyObjectByType<PlayerController>()==null){Debug.LogError("Abre SampleScene del tutorial de enemigos.");return;}
        var panel=Object.FindAnyObjectByType<Lab08EnemyPanel>();
        if(panel==null){
            var go=new GameObject("PanelFlocking");Undo.RegisterCreatedObjectUndo(go,"Crear panel");
            panel=Undo.AddComponent<Lab08EnemyPanel>(go);
        }
        EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
        Selection.activeGameObject=panel.gameObject;
        Debug.Log("Listo. Ctrl+S. En Play: clic en suelo marca destino; P abre pausa con pesos editables.");
    }
}
