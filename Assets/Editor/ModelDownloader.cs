using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class ModelDownloader : EditorWindow
{
    private string _urlModel = "http://localhost:8000/Proyectos/test.fbx";
    private string _nameModel = "model";
    private string _downloadDebug = "";

    private bool _isDownloading = false;
    private float _downloadProgress = 0f;

    private UnityWebRequest _request;

    private GameObject _modelInstanced;

    [MenuItem("Window/Model Downloader")]
    public static void ShowWindow()
    {
        GetWindow<ModelDownloader>("Model Downloader");
    }

    private void OnGUI()
    {
        GUILayout.Space(15);

        _urlModel = EditorGUILayout.TextField("Model URL:", _urlModel);
        _nameModel = EditorGUILayout.TextField("Model Name:", _nameModel);

        if (GUILayout.Button("Download Model"))
        {
            StartDownload(_urlModel);
        }

        GUILayout.BeginVertical();

        Rect progressRect = EditorGUILayout.GetControlRect(false, 25);
        EditorGUI.ProgressBar(progressRect, _downloadProgress, _downloadProgress < 1f ? "Loading" : "Complete");
        GUILayout.Label(_downloadDebug);

        GUILayout.EndVertical();

        GUILayout.Space(15);

        GUILayout.Label("Instantiation Test");

        if (GUILayout.Button("Instance Model Downloaded"))
        {
            InstanceModelDownloaded();
        }

        if (GUILayout.Button("Clear Model Instanced"))
        {
            ClearModelDownloaded();
        }
    }

    private void InstanceModelDownloaded()
    {
        Vector3 cameraPosition = Camera.main.transform.position;
        cameraPosition.y = 0f;

        GameObject modelDownloaded = Resources.Load<GameObject>(_nameModel);
        _modelInstanced = Instantiate(modelDownloaded, cameraPosition + Vector3.forward*40f, Quaternion.identity);

        foreach (Camera camera in _modelInstanced.GetComponentsInChildren<Camera>())
        {
            DestroyImmediate(camera.gameObject);
        }

        foreach (Light light in _modelInstanced.GetComponentsInChildren<Light>())
        {
            DestroyImmediate(light.gameObject);
        }
    }

    private void ClearModelDownloaded()
    {
        if (!_modelInstanced) return;

        DestroyImmediate(_modelInstanced.gameObject);
    }

    private void StartDownload(string url)
    {
        _isDownloading = true;

        _request = UnityWebRequest.Get(url);
        var operation = _request.SendWebRequest();

        operation.completed += (asyncOperation) =>
        {
            if (_request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(_request.error);
            }
            else
            {
                string fileExtension = Path.GetExtension(url);

                string path = Path.Combine(Application.dataPath, "Resources", $"{_nameModel}{fileExtension}");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, _request.downloadHandler.data);
                AssetDatabase.Refresh();

                string debug = $"Model downloaded and saved to: {path}";

                _downloadDebug = debug;
                Debug.Log(debug);
            }
        };

        EditorApplication.update += UpdateDownloadProgress;
    }

    private void UpdateDownloadProgress()
    {
        if (_isDownloading && _request != null)
        {
            _downloadProgress = _request.downloadProgress;
            Repaint();

            if (_request.isDone)
            {
                EditorApplication.update -= UpdateDownloadProgress;
            }
        }
    }
}
