using System;
using System.Collections.Generic;
using System.Text;

namespace APCoreDevice.Business.Models
{
    public class CommandModel
    {
        public int Order { get; set; }
        public string Name { get; set; }
        public long ID { get; set; }
        public int DelayBefore { get; set; }
        public int DelayAfter { get; set; }

        public bool HasReversePinVal { get; set; } = false;
        public int DelayForReversePinVal { get; set; }
        public bool? ReversePinVal { get; set; }
        public PinCommand[] PinActions { get; set; }

        public string Tcode { get; set; }
        public long Tick { get; set; }

        public string TcodeDynamic { get; set; }
    }

    public class PinCommand
    {
        public int PinNumber { get; set; }

        //True = Higth , false = Low
        public bool PinVal { get; set; }

        //In Secound
        public int CommandDuration { get; set; }

        //1=OutPut
        public int PinMode { get; set; }

        public int DelayBefore { get; set; }
        public int DelayAfter { get; set; }
    }
}
