using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverlayApp.Models
{
    public class DamageWithTimeStamp
    {
        public int Damage { get; set; }
        public TimeSpan Time { get; set; }
    }
}
