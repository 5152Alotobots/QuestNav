// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;
using PCD = QuestNav.Passthrough.PassthroughCameraDebugger;

namespace QuestNav.Passthrough
{
    /// <summary>
    /// Serves the passthrough camera stream as an MJPEG stream over HTTP
    /// </summary>
    public class PassthroughMjpegServer : MonoBehaviour
    {
        [SerializeField] private WebCamTextureManager webCamTextureManager;
        [SerializeField] private int port = 8080;
        [SerializeField] private int jpegQuality = 75;
        [SerializeField] private int frameRate = 30;
        [SerializeField] private bool startServerOnAwake = true;
        
        private HttpListener httpListener;
        private bool isRunning;
        private Thread serverThread;
        private readonly List<HttpListenerContext> clients = new List<HttpListenerContext>();
        private readonly object clientsLock = new object();
        private byte[] currentFrameData;
        private DateTime lastFrameTime;
        private readonly object frameDataLock = new object();
        private int frameInterval;
        
        private Texture2D renderTexture;
        
        void Awake()
        {
            if (startServerOnAwake)
            {
                StartServer();
            }
            
            // Calculate frame interval in milliseconds
            frameInterval = 1000 / frameRate;
        }
        
        void OnEnable()
        {
            if (webCamTextureManager == null)
            {
                webCamTextureManager = GetComponent<WebCamTextureManager>();
                if (webCamTextureManager == null)
                {
                    webCamTextureManager = FindObjectOfType<WebCamTextureManager>();
                    if (webCamTextureManager == null)
                    {
                        PCD.DebugMessage(LogType.Error, "No WebCamTextureManager found. MJPEG server will not function.");
                    }
                }
            }
        }
        
        void OnDestroy()
        {
            StopServer();
        }
        
        void Update()
        {
            if (!isRunning || webCamTextureManager == null || webCamTextureManager.WebCamTexture == null)
                return;
            
            // Only capture frames at the specified frame rate
            if ((DateTime.Now - lastFrameTime).TotalMilliseconds < frameInterval)
                return;
                
            lastFrameTime = DateTime.Now;
            
            // Capture frame from WebCamTexture
            CaptureFrame();
        }
        
        private void CaptureFrame()
        {
            var webCamTexture = webCamTextureManager.WebCamTexture;
            if (webCamTexture == null || !webCamTexture.isPlaying || webCamTexture.width <= 16)
                return;
            
            // Create texture for rendering if needed
            if (renderTexture == null || renderTexture.width != webCamTexture.width || 
                renderTexture.height != webCamTexture.height)
            {
                if (renderTexture != null)
                    Destroy(renderTexture);
                
                renderTexture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGB24, false);
            }
            
            // Copy WebCamTexture to our Texture2D
            renderTexture.SetPixels(webCamTexture.GetPixels());
            renderTexture.Apply();
            
            // Convert to JPEG
            byte[] jpegBytes = renderTexture.EncodeToJPG(jpegQuality);
            
            // Update current frame data
            lock (frameDataLock)
            {
                currentFrameData = jpegBytes;
            }
        }
        
        /// <summary>
        /// Starts the MJPEG HTTP server
        /// </summary>
        public void StartServer()
        {
            if (isRunning) return;
            
            try
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add($"http://*:{port}/");
                httpListener.Start();
                
                isRunning = true;
                PCD.DebugMessage(LogType.Log, $"MJPEG Server started on port {port}");
                
                // Start server thread
                serverThread = new Thread(ServerThreadMethod);
                serverThread.IsBackground = true;
                serverThread.Start();
                
                // Start client connection listener
                StartCoroutine(AcceptClientConnections());
            }
            catch (Exception ex)
            {
                PCD.DebugMessage(LogType.Error, $"Failed to start MJPEG server: {ex.Message}");
                isRunning = false;
            }
        }
        
        /// <summary>
        /// Stops the MJPEG HTTP server
        /// </summary>
        public void StopServer()
        {
            if (!isRunning) return;
            
            isRunning = false;
            
            // Close all client connections
            lock (clientsLock)
            {
                foreach (var client in clients)
                {
                    try
                    {
                        client.Response.Close();
                    }
                    catch (Exception ex)
                    {
                        PCD.DebugMessage(LogType.Warning, $"Error closing MJPEG client connection: {ex.Message}");
                    }
                }
                clients.Clear();
            }
            
            // Stop HTTP listener
            if (httpListener != null)
            {
                httpListener.Stop();
                httpListener.Close();
                httpListener = null;
            }
            
            PCD.DebugMessage(LogType.Log, "MJPEG Server stopped");
        }
        
        /// <summary>
        /// Returns true if the server is currently running
        /// </summary>
        public bool IsRunning => isRunning;
        
        /// <summary>
        /// Toggles the server between running and stopped states
        /// </summary>
        public void ToggleServer()
        {
            if (isRunning)
                StopServer();
            else
                StartServer();
        }
        
        private IEnumerator AcceptClientConnections()
        {
            while (isRunning)
            {
                bool hadError = false;
                IAsyncResult asyncResult = null;
                
                try
                {
                    // Get the next client connection asynchronously
                    asyncResult = httpListener.BeginGetContext(null, null);
                }
                catch (Exception ex)
                {
                    hadError = true;
                    if (isRunning)
                    {
                        PCD.DebugMessage(LogType.Error, $"Error starting MJPEG BeginGetContext: {ex.Message}");
                    }
                }
                
                if (!hadError && asyncResult != null)
                {
                    // Wait for the connection without blocking the main thread
                    while (!asyncResult.IsCompleted && isRunning)
                    {
                        yield return null;
                    }
                    
                    if (isRunning)
                    {
                        try
                        {
                            // Get the context
                            var context = httpListener.EndGetContext(asyncResult);
                            
                            // Process the connection
                            ProcessNewConnection(context);
                        }
                        catch (Exception ex)
                        {
                            if (isRunning)
                            {
                                PCD.DebugMessage(LogType.Error, $"Error accepting MJPEG client connection: {ex.Message}");
                            }
                            hadError = true;
                        }
                    }
                }
                
                // If we had an error, wait a bit before trying again
                if (hadError)
                {
                    yield return new WaitForSeconds(1.0f);
                }
            }
        }
        
        private void ProcessNewConnection(HttpListenerContext context)
        {
            try
            {
                string url = context.Request.Url.LocalPath.ToLower();
                
                if (url == "/stream")
                {
                    // Add the client to our list for the MJPEG stream
                    lock (clientsLock)
                    {
                        clients.Add(context);
                    }
                    
                    PCD.DebugMessage(LogType.Log, $"New MJPEG client connected from {context.Request.RemoteEndPoint}, total clients: {clients.Count}");
                }
                else
                {
                    // Serve a simple HTML page with an MJPEG viewer
                    ServeHtmlPage(context);
                }
            }
            catch (Exception ex)
            {
                PCD.DebugMessage(LogType.Error, $"Error processing MJPEG connection: {ex.Message}");
                try
                {
                    context.Response.Close();
                }
                catch
                {
                    // Ignore errors on cleanup
                }
            }
        }
        
        private void ServeHtmlPage(HttpListenerContext context)
        {
            try
            {
                string html = @"<html><body><img src='/stream' style='width:100%'></body></html>";

                byte[] buffer = Encoding.UTF8.GetBytes(html);
                context.Response.ContentType = "text/html";
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.Close();
            }
            catch (Exception ex)
            {
                PCD.DebugMessage(LogType.Error, $"Error serving MJPEG HTML page: {ex.Message}");
            }
        }
        
        private void ServerThreadMethod()
        {
            byte[] headerBytes = Encoding.ASCII.GetBytes(
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: multipart/x-mixed-replace; boundary=--boundary\r\n\r\n");
            
            string frameHeaderTemplate = 
                "--boundary\r\n" +
                "Content-Type: image/jpeg\r\n" +
                "Content-Length: {0}\r\n\r\n";
            
            // Client send loop
            while (isRunning)
            {
                try
                {
                    // Get current frame
                    byte[] frameJpeg;
                    lock (frameDataLock)
                    {
                        frameJpeg = currentFrameData;
                    }
                    
                    if (frameJpeg == null || frameJpeg.Length == 0)
                    {
                        Thread.Sleep(10);
                        continue;
                    }
                    
                    // Create header for this frame
                    string frameHeader = string.Format(frameHeaderTemplate, frameJpeg.Length);
                    byte[] frameHeaderBytes = Encoding.ASCII.GetBytes(frameHeader);
                    
                    // Send to all clients
                    List<HttpListenerContext> disconnectedClients = new List<HttpListenerContext>();
                    
                    lock (clientsLock)
                    {
                        foreach (var client in clients)
                        {
                            try
                            {
                                var outputStream = client.Response.OutputStream;
                                
                                // For new clients, send the HTTP header first
                                if (client.Response.ContentType == null || !client.Response.ContentType.Contains("multipart"))
                                {
                                    client.Response.ContentType = "multipart/x-mixed-replace; boundary=--boundary";
                                    outputStream.Write(headerBytes, 0, headerBytes.Length);
                                    outputStream.Flush();
                                }
                                
                                // Send frame header
                                outputStream.Write(frameHeaderBytes, 0, frameHeaderBytes.Length);
                                
                                // Send frame data
                                outputStream.Write(frameJpeg, 0, frameJpeg.Length);
                                
                                // End frame with 2 newlines
                                outputStream.Write(new byte[] { 13, 10, 13, 10 }, 0, 4);
                                outputStream.Flush();
                            }
                            catch (Exception)
                            {
                                // Client disconnected, add to cleanup list
                                disconnectedClients.Add(client);
                            }
                        }
                        
                        // Remove disconnected clients
                        foreach (var client in disconnectedClients)
                        {
                            clients.Remove(client);
                            try
                            {
                                client.Response.Close();
                            }
                            catch
                            {
                                // Ignore errors on cleanup
                            }
                        }
                        
                        if (disconnectedClients.Count > 0)
                        {
                            PCD.DebugMessage(LogType.Log, $"MJPEG Clients disconnected: {disconnectedClients.Count}, remaining: {clients.Count}");
                        }
                    }
                    
                    // Sleep to maintain frame rate
                    Thread.Sleep(frameInterval);
                }
                catch (Exception ex)
                {
                    if (isRunning)
                    {
                        PCD.DebugMessage(LogType.Error, $"Error in MJPEG server thread: {ex.Message}");
                    }
                    
                    // Don't spam errors
                    Thread.Sleep(1000);
                }
            }
        }
    }
}