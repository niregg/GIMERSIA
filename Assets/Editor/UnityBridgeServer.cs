#if UNITY_EDITOR
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Antigravity.Cockpit
{
    /// <summary>
    /// Marshals HTTP REST requests onto Unity's main editor thread via EditorApplication.update
    /// and cleanly releases the TCP listener across AppDomain script reloads.
    /// </summary>
    [InitializeOnLoad]
    public class UnityBridgeServer : EditorWindow
    {
        private const string PrefsKeyAutoStart = "Antigravity_Bridge_AutoStart";
        private const string PrefsKeyPort = "Antigravity_Bridge_Port";
        private static int port = 8080;
        private static HttpListener listener;
        private static Thread listenerThread;
        private static volatile bool isRunning = false;
        private static readonly ConcurrentQueue<ActionContext> actionQueue = new ConcurrentQueue<ActionContext>();

        private class ActionContext
        {
            public HttpListenerContext Context;
            public string Path;
            public string Method;
            public string Body;
        }

        static UnityBridgeServer()
        {
            port = EditorPrefs.GetInt(PrefsKeyPort, 8080);

            EditorApplication.update -= MainThreadUpdate;
            EditorApplication.update += MainThreadUpdate;

            AssemblyReloadEvents.beforeAssemblyReload -= StopServer;
            AssemblyReloadEvents.beforeAssemblyReload += StopServer;

            EditorApplication.quitting -= StopServer;
            EditorApplication.quitting += StopServer;

            if (EditorPrefs.GetBool(PrefsKeyAutoStart, true))
            {
                EditorApplication.delayCall += () => StartServer(port);
            }
        }

        [MenuItem("Window/Antigravity Cockpit Bridge")]
        public static void ShowWindow()
        {
            GetWindow<UnityBridgeServer>("Cockpit Bridge");
        }

        private void OnGUI()
        {
            GUILayout.Label("Antigravity Unity Cockpit Bridge", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            port = EditorGUILayout.IntField("Port", port);
            bool autoStart = EditorGUILayout.Toggle("Auto-start with Editor", EditorPrefs.GetBool(PrefsKeyAutoStart, true));
            if (GUI.changed)
            {
                EditorPrefs.SetInt(PrefsKeyPort, port);
                EditorPrefs.SetBool(PrefsKeyAutoStart, autoStart);
            }

            EditorGUILayout.Space();
            if (isRunning)
            {
                EditorGUILayout.HelpBox($"Active: http://127.0.0.1:{port}/", MessageType.Info);
                if (GUILayout.Button("Stop Bridge Server"))
                {
                    StopServer();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Bridge Inactive.", MessageType.Warning);
                if (GUILayout.Button("Start Bridge Server"))
                {
                    StartServer(port);
                }
            }
        }

        public static void StartServer(int targetPort)
        {
            if (isRunning) return;

            try
            {
                port = targetPort;
                listener = new HttpListener();
                listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                listener.Start();
                isRunning = true;

                listenerThread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "AntigravityUnityBridgeListener"
                };
                listenerThread.Start();
                Debug.Log($"[Antigravity Bridge] Listening on http://127.0.0.1:{port}/");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Antigravity Bridge] Listener bind failed on port {targetPort}: {ex.Message}");
                isRunning = false;
            }
        }

        public static void StopServer()
        {
            if (!isRunning && listener == null) return;
            isRunning = false;

            if (listener != null)
            {
                try
                {
                    listener.Stop();
                    listener.Close();
                }
                catch (ObjectDisposedException)
                {
                    // Listener already disposed during domain teardown.
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Antigravity Bridge] Listener shutdown warning: {ex.Message}");
                }
                finally
                {
                    listener = null;
                }
            }
        }

        private static void ListenLoop()
        {
            while (isRunning && listener != null && listener.IsListening)
            {
                try
                {
                    var context = listener.GetContext();
                    string path = context.Request.Url.AbsolutePath.TrimEnd('/');
                    string method = context.Request.HttpMethod;
                    string body = "";

                    if (context.Request.HasEntityBody)
                    {
                        using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                        {
                            body = reader.ReadToEnd();
                        }
                    }

                    actionQueue.Enqueue(new ActionContext
                    {
                        Context = context,
                        Path = path,
                        Method = method,
                        Body = body
                    });
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (isRunning)
                    {
                        Debug.LogWarning($"[Antigravity Bridge] Listener loop error: {ex.Message}");
                    }
                }
            }
        }

        private static void MainThreadUpdate()
        {
            while (actionQueue.TryDequeue(out var req))
            {
                ProcessRequestOnMainThread(req);
            }
        }

        private static void ProcessRequestOnMainThread(ActionContext req)
        {
            string responseJson;
            int statusCode = 200;

            try
            {
                if (req.Path == "/ping" || req.Path == "")
                {
                    responseJson = JsonUtility.ToJson(new PingResponse
                    {
                        status = "success",
                        unityVersion = Application.unityVersion,
                        productName = Application.productName,
                        activeScene = EditorSceneManager.GetActiveScene().name,
                        isPlaying = EditorApplication.isPlaying
                    });
                }
                else if (req.Path == "/refresh")
                {
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    responseJson = "{\"status\":\"success\",\"message\":\"AssetDatabase refreshed\"}";
                }
                else if (req.Path == "/scene/hierarchy")
                {
                    var roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
                    var list = new List<string>(roots.Length);
                    foreach (var go in roots)
                    {
                        list.Add(go.name.Replace("\"", "\\\""));
                    }
                    string jsonArray = list.Count > 0 ? "[\"" + string.Join("\",\"", list) + "\"]" : "[]";
                    responseJson = $"{{\"status\":\"success\",\"scene\":\"{EditorSceneManager.GetActiveScene().name}\",\"root_objects\":{jsonArray}}}";
                }
                else if (req.Path == "/playmode")
                {
                    var payload = JsonUtility.FromJson<PlaymodePayload>(req.Body);
                    EditorApplication.isPlaying = payload.state;
                    responseJson = $"{{\"status\":\"success\",\"isPlaying\":{EditorApplication.isPlaying.ToString().ToLowerInvariant()}}}";
                }
                else if (req.Path == "/menu_item")
                {
                    var payload = JsonUtility.FromJson<MenuPayload>(req.Body);
                    bool ok = EditorApplication.ExecuteMenuItem(payload.menuItem);
                    responseJson = $"{{\"status\":\"{(ok ? "success" : "failed")}\",\"menuItem\":\"{payload.menuItem}\"}}";
                }
                else if (req.Path == "/create_primitive")
                {
                    var payload = JsonUtility.FromJson<CreatePrimitivePayload>(req.Body);
                    if (!Enum.TryParse(payload.primitiveType, true, out PrimitiveType primType))
                    {
                        primType = PrimitiveType.Cube;
                    }

                    GameObject go = GameObject.CreatePrimitive(primType);
                    if (!string.IsNullOrEmpty(payload.name)) go.name = payload.name;
                    go.transform.position = new Vector3(payload.posX, payload.posY, payload.posZ);
                    Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
                    Selection.activeGameObject = go;

                    responseJson = $"{{\"status\":\"success\",\"name\":\"{go.name}\",\"instanceId\":{go.GetInstanceID()}}}";
                }
                else
                {
                    statusCode = 404;
                    responseJson = $"{{\"status\":\"error\",\"error\":\"Unknown endpoint: {req.Path}\"}}";
                }
            }
            catch (Exception ex)
            {
                statusCode = 500;
                string safeMsg = ex.Message.Replace("\\", "\\\\").Replace("\"", "\\\"");
                responseJson = $"{{\"status\":\"error\",\"error\":\"{safeMsg}\"}}";
            }

            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(responseJson);
                req.Context.Response.ContentType = "application/json";
                req.Context.Response.StatusCode = statusCode;
                req.Context.Response.ContentLength64 = buffer.Length;
                req.Context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                req.Context.Response.OutputStream.Close();
            }
            catch (Exception writeEx)
            {
                Debug.LogWarning($"[Antigravity Bridge] Response stream write failed: {writeEx.Message}");
            }
        }

        [Serializable]
        private class PingResponse
        {
            public string status;
            public string unityVersion;
            public string productName;
            public string activeScene;
            public bool isPlaying;
        }

        [Serializable]
        private class PlaymodePayload
        {
            public bool state;
        }

        [Serializable]
        private class MenuPayload
        {
            public string menuItem;
        }

        [Serializable]
        private class CreatePrimitivePayload
        {
            public string primitiveType;
            public string name;
            public float posX;
            public float posY;
            public float posZ;
        }
    }
}
#endif
