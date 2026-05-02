using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using static Inventor82.Executables;


namespace Inventor82
{
    /// <summary>
    /// Represents simplified information about an Autodesk Inventor document for API serialization.
    /// </summary>
    /// <remarks>
    /// This class provides a lightweight, serializable representation of document properties
    /// suitable for JSON responses from the API server.
    /// </remarks>
    public class DocInfo
    {
        /// <summary>
        /// Gets or sets the display name of the document as shown in the Inventor window title bar.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the full file path of the document on disk.
        /// </summary>
        /// <remarks>
        /// This value may be <c>null</c> or empty for unsaved documents.
        /// </remarks>
        public string FullFileName { get; set; }

        /// <summary>
        /// Gets or sets the document type as a string (e.g., "Part", "Assembly", "Drawing").
        /// </summary>
        public string DocumentType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this document is the currently active document in the Inventor application.
        /// </summary>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Provides a lightweight HTTP API server that exposes Autodesk Inventor automation functionality
    /// via REST endpoints. Implements <see cref="IDisposable"/> for clean shutdown.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The server listens on the specified port on both <c>localhost</c> and <c>127.0.0.1</c> loopback addresses.
    /// All responses are returned in JSON format. Request processing is handled asynchronously to allow
    /// concurrent API calls.
    /// </para>
    /// <para>
    /// <b>Available Endpoints:</b>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Endpoint</term>
    ///     <description>Method &amp; Description</description>
    ///   </listheader>
    ///   <item>
    ///     <term><c>/api/health</c></term>
    ///     <description><b>GET</b> - Returns the server status.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>/api/getopenedwindows</c></term>
    ///     <description><b>GET</b> - Returns a JSON array of visible document information.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>/api/getallopeneddocuments</c></term>
    ///     <description><b>GET</b> - Returns a JSON array of all open document information.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>/api/addipt</c></term>
    ///     <description><b>POST</b> - Creates a new part document. Optional query parameter: <c>name</c>.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>/api/addiptwithsketch</c></term>
    ///     <description><b>POST</b> - Creates a new part document with a sketch. Query parameters: <c>plane</c> (1=XZ, 2=YZ, 3=XY), optional <c>iptname</c>, optional <c>sketchname</c>.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>/api/activatedoc</c></term>
    ///     <description><b>POST</b> - Activates a document by name. Query parameter: <c>name</c>.</description>
    ///   </item>
    /// </list>
    /// </para>
    /// </remarks>
    public class ApiServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly int _port;
        private bool _isRunning;
        private readonly Action<string> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiServer"/> class.
        /// </summary>
        /// <param name="port">
        /// The TCP port number on which the HTTP server will listen.
        /// </param>
        /// <param name="logger">
        /// An optional callback for logging server activity. If <c>null</c>, logs are written
        /// to <see cref="Debug"/> output with the prefix "[INVENTOR82 API]".
        /// </param>
        /// <remarks>
        /// The listener is configured to accept requests on both <c>http://localhost:{port}/</c>
        /// and <c>http://127.0.0.1:{port}/</c>. The server does not start automatically;
        /// call <see cref="Start"/> to begin processing requests.
        /// </remarks>
        public ApiServer(int port, Action<string> logger = null)
        {
            _port = port;
            _logger = logger == null ? (msg => Debug.WriteLine($"[INVENTOR82 API]: {msg}")) : logger;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{_port}/");
            _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
        }

        /// <summary>
        /// Starts the HTTP listener and begins processing incoming requests asynchronously.
        /// </summary>
        /// <exception cref="Exception">
        /// Thrown if the listener fails to start, typically due to the port being in use or
        /// insufficient permissions to bind to the address.
        /// </exception>
        /// <remarks>
        /// If the server is already running, this method has no effect. Request processing
        /// occurs on a background task and does not block the calling thread.
        /// </remarks>
        public void Start()
        {
            if (_isRunning) return;

            try
            {
                _listener.Start();
                _isRunning = true;
                _ = Task.Run(ListenAsync);
                _logger($"API server started on port {_port}");
            }
            catch (Exception ex)
            {
                _logger($"Failed to start API server: {ex.Message}");
                throw;
            }
        }
        private async Task ListenAsync()
        {
            while (_isRunning)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => ProcessRequestAsync(context));
                }
                catch (HttpListenerException) when (!_isRunning)
                {
                    // Expected during shutdown
                }
                catch (Exception ex)
                {
                    _logger($"Listener error: {ex.Message}");
                }
            }
        }
        async Task ProvideResponse(HttpListenerResponse response, string responseString, int statusCode = 200)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            response.StatusCode = 200;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                _logger($"Request: {request.HttpMethod} {request.Url.AbsolutePath}");

                switch (request.Url.AbsolutePath)
                {
                    case "/api/health":
                        if (request.HttpMethod == "GET")
                        {
                            string responseString = "{ \"status\": \"RUNNING\"}";
                            await ProvideResponse(response, responseString);
                        }
                        else
                        {
                            response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        }
                        break;
                    case "/api/getopenedwindows":
                        if (request.HttpMethod == "GET")
                        {
                            var docs = Executables.GetOpenedWindows().Select(d => new DocInfo
                            {
                                DisplayName = d.DisplayName,
                                FullFileName = d.FullFileName,
                                DocumentType = d.DocumentType.ToString(),
                                IsActive = d == Standalone.m_inventorApplication.ActiveDocument
                            }).ToList();
                            string responseString = JsonSerializer.Serialize(docs);
                            await ProvideResponse(response, responseString);
                        }
                        else
                        {
                            response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        }
                        break;
                    case "/api/getallopeneddocuments":
                        if (request.HttpMethod == "GET")
                        {
                            var docs = Executables.GetAllOpenedDocuments().Select(d => new DocInfo
                            {
                                DisplayName = d.DisplayName,
                                FullFileName = d.FullFileName,
                                DocumentType = d.DocumentType.ToString(),
                                IsActive = d == Standalone.m_inventorApplication.ActiveDocument
                            }).ToList();
                            string responseString = JsonSerializer.Serialize(docs);
                            await ProvideResponse(response, responseString);
                        }
                        else
                        {
                            response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        }
                        break;
                    case "/api/addiptwithsketch":
                        if (request.HttpMethod == "POST")
                        {
                            var query = request.QueryString;
                            if (!string.IsNullOrWhiteSpace(query["plane"]))
                            {
                                SketchPlanes plane = (SketchPlanes)Convert.ToInt32(query["plane"]);
                                if(plane > SketchPlanes.XZ || plane < SketchPlanes.XZ)
                                {
                                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                                    await ProvideResponse(response, "{ \"error\": \"Invalid plane value. Must be 1 (XZ), 2 (YZ), or 3 (XY).\"}", 400);
                                    return;
                                }
                                else
                                {
                                    string iptName = query["iptname"] ?? "";
                                    string sketchName = query["sketchname"] ?? "";
                                    Executables.AddNewIptWithSketch(plane, iptName, sketchName);
                                    await ProvideResponse(response, "{ \"status\": \"Part and sketch created successfully.\"}");
                                }
                            }
                            else
                            {
                                response.StatusCode = (int)HttpStatusCode.BadRequest;
                            }
                        }
                        else
                        {
                            response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        }
                        break;
                    case "/api/addipt":
                        if (request.HttpMethod == "POST")
                        {
                            var query = request.QueryString;
                            if (!string.IsNullOrWhiteSpace(query["name"]))
                            {
                                string iptName = query["name"];
                                Executables.AddNewIpt(iptName);
                                await ProvideResponse(response, "{ \"status\": \"Part created successfully.\"}");
                            }
                            else
                            {
                                Executables.AddNewIpt();
                                await ProvideResponse(response, "{ \"status\": \"Part created successfully.\"}");
                            }
                        }
                        else
                        {
                            response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        }
                        break;
                    case "/api/activatedoc":
                        if (request.HttpMethod == "POST")
                        {
                            var query = request.QueryString;
                            if (!string.IsNullOrEmpty(query["name"]))
                            {
                                string docName = query["name"];
                                if(Executables.ActivateDocumentByName(docName))
                                    await ProvideResponse(response, "{ \"status\": \"Document activated successfully.\"}");
                                else
                                    await ProvideResponse(response, "{ \"status\": \"Document wasn't found.\"}", 404);
                            }
                        }
                        else
                        {
                            response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        }
                        break;
                   
                    default:
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        break;
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                _logger($"Request processing error: {ex.Message}");
            }
            finally
            {
                context.Response.Close();
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="ApiServer"/> and stops the HTTP listener.
        /// </summary>
        /// <remarks>
        /// Calling this method signals the listener to stop accepting new requests and closes
        /// the underlying <see cref="HttpListener"/>. Any in-progress requests may complete
        /// before the server fully shuts down.
        /// </remarks>
        public void Dispose()
        {
            _isRunning = false;
            _listener.Close();
            _logger("API server stopped");
        }

    }//
}
