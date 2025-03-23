// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Linq;
using QuestNav.Utils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace QuestNav.Passthrough
{
    public class PassthroughCameraManager : MonoBehaviour
    {
        [SerializeField] public PassthroughCameraEye Eye = PassthroughCameraEye.Left;
        [FormerlySerializedAs("RequestedResolution")]
        [SerializeField, Tooltip("The requested resolution of the camera may not be supported by the chosen camera. In such cases, the closest available values will be used.\n\n" +
                                 "When set to (0,0), the highest supported resolution will be used.")]
        
        public Vector2Int requestedResolution;
        [SerializeField] public PassthroughCameraPermissions CameraPermissions;

        /// <summary>
        /// Returns <see cref="WebCamTexture"/> reference if required permissions were granted and this component is enabled. Else, returns null.
        /// </summary>
        public WebCamTexture WebCamTexture { get; private set; }

        private bool hasPermission;

        private void Awake()
        {
            Assert.AreEqual(1, FindObjectsByType<PassthroughCameraManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                $"[Passthrough Camera Manager] More than one {nameof(PassthroughCameraManager)} component. Only one instance is allowed at a time.");
#if UNITY_ANDROID
            CameraPermissions.AskCameraPermissions();
#endif
        }

        private void OnEnable()
        {
            if (!PassthroughCameraUtils.IsSupported) throw new PlatformNotSupportedException("Passthrough camera is not supported.");
            
            if (!PassthroughCameraPermissions.HasCameraPermission == true)
            {
                QueuedLogger.LogError(
                    $"[Passthrough Camera Manager] Passthrough Camera requires permission(s) {string.Join(" and ", PassthroughCameraPermissions.CameraPermissions)}. Waiting for them to be granted...");
                return;
            }

            QueuedLogger.Log("[Passthrough Camera Manager] All Permissions granted. Starting...");
            _ = StartCoroutine(WebCamTextureCoroutine());
        }

        private void OnDisable()
        {
            StopCoroutine(WebCamTextureCoroutine());
            if (!WebCamTexture) return;
            WebCamTexture.Stop();
            Destroy(WebCamTexture);
            WebCamTexture = null;
        }

        private void Update()
        {
            if (hasPermission) return;
            if (PassthroughCameraPermissions.HasCameraPermission != true) return;
            hasPermission = true;
            _ = StartCoroutine(WebCamTextureCoroutine());
        }

        private IEnumerator WebCamTextureCoroutine()
        {
            while (true)
            {
                var devices = WebCamTexture.devices;
                if (PassthroughCameraUtils.EnsureInitialized() && PassthroughCameraUtils.CameraEyeToCameraIdMap.TryGetValue(Eye, out var cameraData))
                {
                    if (cameraData.index < devices.Length)
                    {
                        var deviceName = devices[cameraData.index].name;
                        WebCamTexture webCamTexture;
                        if (requestedResolution == Vector2Int.zero)
                        {
                            var largestResolution = PassthroughCameraUtils.GetOutputSizes(Eye).OrderBy(static size => size.x * size.y).Last();
                            webCamTexture = new WebCamTexture(deviceName, largestResolution.x, largestResolution.y);
                        }
                        else
                        {
                            webCamTexture = new WebCamTexture(deviceName, requestedResolution.x, requestedResolution.y);
                        }
                        // There is a bug in the current implementation of WebCamTexture: if 'Play()' is called at the same frame the WebCamTexture was created, this error is logged and the WebCamTexture object doesn't work:
                        //     Camera2: SecurityException java.lang.SecurityException: validateClientPermissionsLocked:1325: Callers from device user 0 are not currently allowed to connect to camera "66"
                        //     Camera2: Timeout waiting to open camera.
                        // Waiting for one frame is important and prevents the bug.
                        yield return null;
                        webCamTexture.Play();
                        var currentResolution = new Vector2Int(webCamTexture.width, webCamTexture.height);
                        if (requestedResolution != Vector2Int.zero && requestedResolution != currentResolution)
                        {
                            QueuedLogger.LogWarning($"[Passthrough Camera Manager] Requested resolution is not supported. Current resolution: {currentResolution}.");
                        }
                        WebCamTexture = webCamTexture;
                        QueuedLogger.Log($"WebCamTexture created, texturePtr: {WebCamTexture.GetNativeTexturePtr()}, size: {WebCamTexture.width}/{WebCamTexture.height}");
                        yield break;
                    }
                }
                
                QueuedLogger.LogError($"Requested camera is not present in WebCamTexture.devices: {string.Join(", ", devices)}.");
                yield return null;
            }
        }
    }

    /// <summary>
    /// Defines the position of a passthrough camera relative to the headset
    /// </summary>
    public enum PassthroughCameraEye
    {
        Left,
        Right
    }
}
