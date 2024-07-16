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

    private GameObject _modelDownloaded;
    private GameObject _modelInstanced;

    private Texture2D _previewTexture;

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
        EditorGUI.ProgressBar(
            progressRect, 
            _downloadProgress, 
            _downloadProgress < 1f ? 
                $"Loading {_downloadProgress*100}%" : 
                "Complete"
            );

        GUILayout.Label("Download Debug Info:");
        GUILayout.TextArea(_downloadDebug, GUILayout.Height(100));

        GUILayout.Space(10);

        if (GUILayout.Button("Load Model Downloaded"))
        {
            LoadModelFromResources();
        }

        if(_modelDownloaded)
        {
            _previewTexture = AssetPreview.GetAssetPreview(_modelDownloaded);
            if (_previewTexture == null)
            {
                AssetPreview.SetPreviewTextureCacheSize(1);
                _previewTexture = AssetPreview.GetAssetPreview(_modelDownloaded);
            }

            GUILayout.Label("Preview:");
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(_previewTexture, GUILayout.Width(128), GUILayout.Height(128));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

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

    private void LoadModelFromResources()
    {
        _modelDownloaded = Resources.Load<GameObject>(_nameModel);
    }

    private void InstanceModelDownloaded()
    {
        Vector3 cameraPosition = Camera.main.transform.position;
        cameraPosition.y = 0f;

        _modelInstanced  = Instantiate(_modelDownloaded, cameraPosition + Vector3.forward*40f, Quaternion.identity);

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

                LoadModelFromResources();
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
