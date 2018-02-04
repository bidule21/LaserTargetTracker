using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace LaserTargetTracker
{
    class Shot
    {
        public int Id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string Time { get; set; }
        public double DeltaT { get; set; }

        

        public Shot(int id, int x, int y, string time, double dt)
        {
            X = x;
            Y = y;
            Id = id;
            Time = time;
            DeltaT = dt;
        }
    }
}
