using AForge;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserTargetTracker
{

    class CameraManager
    {
        public FilterInfoCollection Cameras;

        public CameraManager()
        {

            Cameras = new FilterInfoCollection(FilterCategory.VideoInputDevice);

        }

    }
}
