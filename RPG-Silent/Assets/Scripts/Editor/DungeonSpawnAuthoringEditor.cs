using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DungeonSpawnAuthoring))]
public class DungeonSpawnAuthoringEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var authoring = (DungeonSpawnAuthoring)target;

        DrawDefaultInspector();

        EditorGUILayout.Space();

        DungeonDatabase.Entry entry = ResolveEntry(authoring);
        if (entry != null)
        {
            EditorGUILayout.HelpBox(
                $"当前副本：[{entry.Id}] {entry.DisplayName}\n" +
                $"表中出生点：{entry.SpawnPosition}  朝向：{entry.SpawnEulerAngles}",
                MessageType.Info);
        }
        else if (authoring.database != null)
        {
            EditorGUILayout.HelpBox($"副本表里找不到 ID = {authoring.dungeonId} 的副本。", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("请先指定 Database。", MessageType.Warning);
        }

        using (new EditorGUI.DisabledScope(entry == null))
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("读取表格出生点 → 此物体"))
            {
                Undo.RecordObject(authoring.transform, "Load Spawn From Table");
                authoring.transform.position    = entry.SpawnPosition;
                authoring.transform.eulerAngles  = entry.SpawnEulerAngles;
            }

            if (GUILayout.Button("保存此物体位置 → 表格"))
                WriteToTable(authoring, entry);

            EditorGUILayout.EndHorizontal();
        }
    }

    private void OnSceneGUI()
    {
        var authoring = (DungeonSpawnAuthoring)target;
        DungeonDatabase.Entry entry = ResolveEntry(authoring);
        if (entry == null) return;

        Transform t = authoring.transform;

        EditorGUI.BeginChangeCheck();
        Vector3    newPos = Handles.PositionHandle(t.position, t.rotation);
        Quaternion newRot = Handles.RotationHandle(t.rotation, t.position);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(t, "Move Dungeon Spawn");
            t.position = newPos;
            t.rotation = newRot;
            WriteToTable(authoring, entry);
        }

        Handles.Label(t.position + Vector3.up * 0.6f, $"出生点：[{entry.Id}] {entry.DisplayName}");
    }

    private static DungeonDatabase.Entry ResolveEntry(DungeonSpawnAuthoring authoring)
    {
        if (authoring.database == null) return null;
        authoring.database.TryGetById(authoring.dungeonId, out DungeonDatabase.Entry entry);
        return entry;
    }

    private static void WriteToTable(DungeonSpawnAuthoring authoring, DungeonDatabase.Entry entry)
    {
        Undo.RecordObject(authoring.database, "Write Dungeon Spawn");
        entry.SpawnPosition    = authoring.transform.position;
        entry.SpawnEulerAngles = authoring.transform.eulerAngles;
        EditorUtility.SetDirty(authoring.database);
    }
}
