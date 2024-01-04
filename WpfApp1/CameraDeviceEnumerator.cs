using DirectShowLib;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp1
{
    public static class CameraDeviceEnumerator
    {
        public static List<CameraDevice> GetAllConnectedCameras()
        {
            var cameras = new List<CameraDevice>();
            var videoInputDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

            int openCvId = 0;
            return videoInputDevices.Select(v => new CameraDevice()
            {
                DeviceId = v.DevicePath,
                Name = v.Name,
                OpenCvId = openCvId++
            }).ToList();
        }
    }
}
