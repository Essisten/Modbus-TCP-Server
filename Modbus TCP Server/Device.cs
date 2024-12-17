using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modbus_TCP_Server
{
    public class Device
    {
        public int ID { get; set; }
        public bool Coil { get; set; }
        public bool Discrete_Input { get; set; }
        public short Holding_Register { get; set; }
        public short Input_Register { get; set; }
        public Device(int id, bool coil, bool di, short hr, short ir)
        {
            Coil = coil;
            Discrete_Input = di;
            Holding_Register = hr;
            Input_Register = ir;
            ID = id;
        }
    }
}
