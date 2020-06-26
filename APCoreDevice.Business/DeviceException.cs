using System;
using System.Collections.Generic;
using System.Text;

namespace APCoreDevice.Business
{
    public class DeviceException : Exception
    {
        public DeviceException(string msg) : base(msg)
        {

        }
    }
}
