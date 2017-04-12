using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADBReaderService
{
    public class Device
    {
        public Device() { }
        public Device(int p_id, string p_dev_id, string p_adb_status, string p_machine, bool p_used, int p_appium_port, int p_bootstrap_port, int p_pid) {
            id = p_id;
            dev_id = p_dev_id;
            adb_status = p_adb_status;
            machine = p_machine;
            used = p_used;
            appium_port = p_appium_port;
            bootstrap_port = p_bootstrap_port;
            pid = p_pid;
        }
        public int id { get; set; }
        public string dev_id { get; set; }
        public string adb_status { get; set; }
        public string machine { get; set; }
        public bool used { get; set; }
        public int appium_port { get; set; }
        public int bootstrap_port { get; set; }
        public int pid { get; set; }
    }
}