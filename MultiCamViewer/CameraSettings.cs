namespace MultiCamViewer
{
    public sealed class CameraSettings
    {
        /// <summary>
        /// Indicates whether the camera is enabled and should be displayed in the viewer.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// The URL of the camera stream, typically in RTSP format (e.g., "rtsp://
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// The login username for accessing the camera stream, if required. This is often used for cameras that require authentication to access the video feed.
        /// </summary>
        public string Login { get; set; } = "admin";
        
        /// <summary>
        /// The password for accessing the camera stream, if required.
        /// </summary>
        public string Password { get; set; } = "password";

        /// <summary>
        /// The index of the stream to use from the camera. Some cameras may provide multiple streams (e.g., different resolutions or quality levels), 
        /// and this property allows you to specify which stream to use. The index is typically zero-based, meaning that the first stream would be index 0, 
        /// the second stream would be index 1, and so on.
        /// </summary>
        public int StreamIndex { get; set; }

        /// <summary>
        /// The index of the transport protocol to use for the camera stream. 
        /// This is relevant for cameras that support multiple transport protocols (e.g., TCP, UDP, HTTP) for streaming video.
        /// </summary>
        public int TransportIndex { get; set; }

        /// <summary>
        /// Creates a default CameraSettings instance with a predefined URL format based on the provided index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static CameraSettings CreateDefault(int index)
        {
            return new CameraSettings
            {
                Url = $"rtsp://192.168.1.10{index + 1}/live"
            };
        }
    }
}
