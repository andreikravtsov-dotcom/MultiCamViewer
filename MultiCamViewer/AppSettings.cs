/******************************************************************************
 *
 * File: AppSettings.cs
 *
 * Description: Settings for the MultiCamViewer application, including camera configuration and layout options.
 *
 * Date: 08.06.2026		Author: Andrei Kravtsov
 *
 *****************************************************************************/
using System.Collections.Generic;

namespace MultiCamViewer
{
    public sealed class AppSettings
    {
        /// <summary>
        /// Number of cameras to display. Supported values are 1, 2, 3 and 4.
        /// </summary>
        public int CameraCount { get; set; } = 4;

        /// <summary>
        /// Layout of the camera views. Supported values are "Grid", "Vertical" and "Horizontal".
        /// </summary>
        public string Layout { get; set; } = nameof(CameraLayout.Grid);

        /// <summary>
        /// List of camera settings. The number of items in this list should match the value of CameraCount.
        /// </summary>
        public List<CameraSettings> Cameras { get; set; } = [];

        private const int CameraTotal = 4;

        /// <summary>
        /// Creates a default AppSettings instance with the default values for CameraCount, Layout and a list of CameraSettings for each camera.
        /// </summary>
        /// <returns></returns>
        public static AppSettings CreateDefault()
        {
            var settings = new AppSettings();

            for (var index = 0; index < CameraTotal; index++)
            {
                settings.Cameras.Add(CameraSettings.CreateDefault(index));
            }
            return settings;
        }
    }
}
