using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using NLog.Config;

namespace ADBReaderService
{
    class ADBWorkTimer
    {
        private int invokeCount;
        public int maxCount;
        public StreamWriter file;
        public string filename;
        private static string adb_path = @"C:\Program Files (x86)\Android\android-sdk\platform-tools\";

        public ADBWorkTimer(int count)
        {
            invokeCount = 0;
            maxCount = count;
        }

        public void Check(Object stateInfo)
        {
            AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;

            file.WriteLine(string.Format("{0} Проверка {1,2}.\n", DateTime.Now.ToString("HH:mm:ss.fff"), (++invokeCount).ToString()));

            if (invokeCount == maxCount)
            {
                invokeCount = 0;
                autoEvent.Set();
            }
        }

        [STAThread]
        public void getDevicesFromDB(Object stateInfo)
        {
            Logger log = LogManager.GetCurrentClassLogger();

            AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;

            file.WriteLine(string.Format("{0} {1,2}.\n", DateTime.Now.ToString("HH:mm:ss.fff"), (++invokeCount).ToString()));

            log.Info("Получаем список устройств...");
            List<Device> devices = ADBReader.getDevicesList();
            foreach (Device d in devices)
                file.WriteLine(d.dev_id);

            if (invokeCount == maxCount)
            {
                invokeCount = 0;
                autoEvent.Set();
            }
        }

        [STAThread]
        public void getOnlineDevices(Object stateInfo)
        {
            Logger log = LogManager.GetCurrentClassLogger();

            AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;

            file.WriteLine(string.Format("{0} {1,2}.\n", DateTime.Now.ToString("HH:mm:ss.fff"), (++invokeCount).ToString()));

            log.Info("Запуск adb devices");
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = adb_path + "adb.exe";
            p.StartInfo.Arguments = "devices";
            p.Start();
            log.Info(string.Format("PID процесса: {0}", p.Id));

            string machine = Environment.MachineName;
            log.Info(string.Format("Имя компьютера: {0}", machine));

            try
            {
                string list = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                file.WriteLine(string.Format("Результат adb devices: {0}", list));

                file.WriteLine("Установить статус offline всем устройствам, которые не используются.");
                ADBReader.DevicesSetOffline(machine);

                string s = list.Replace("\r\n", "|").Replace("\t", "+");

                string[] r = s.Split('|', '+');
                List<Device> devices = new List<Device>();

                file.WriteLine(string.Format("Запись в базу данных устройств."));

                for (int i = 0; i < r.Length - 2; i += 2)
                {
                    string t = r[i + 1];
                    if (t != "")
                    {
                        devices.Add(new Device(0, t, r[i + 2], machine, false, 0, 0, 0));
                        //file.WriteLine(string.Format("Запись в базу данных устройства: {0}.", t));
                        //ADBReader.DeviceAdd(t, r[i + 2]);
                        //log.Info(string.Format("{0} {1}", t, r[i + 2]));
                    }
                }

                if (ADBReader.DevicesAdd(devices))
                    file.WriteLine("Запись в БД завершена.");
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
            }

            if (invokeCount == maxCount)
            {
                invokeCount = 0;
                autoEvent.Set();
            }
        }
    }

    public partial class DevicesListService : ServiceBase
    {
        public DevicesListService()
        {
            InitializeComponent();
        }

        private Logger log = LogManager.GetCurrentClassLogger();
        private int timestart = 1000;
        private int period = 10000;
        private int count = 518400;

        private void startTimer()
        {
            ADBWorkTimer adbwork = new ADBWorkTimer(count);

            adbwork.filename = @"c:\machine\logs\adbreader\service_" + DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss") + ".log";
            adbwork.file = File.CreateText(adbwork.filename);

            AutoResetEvent autoEvent = new AutoResetEvent(false);

            TimerCallback tcb = adbwork.getOnlineDevices;

            adbwork.file.WriteLine("{0} Выполняем каждые " + (period / 1000) + " секунд\n", DateTime.Now.ToString("HH:mm:ss.fff"));
            Timer stateTimer = new Timer(tcb, autoEvent, timestart, period);

            autoEvent.WaitOne(count * period + 10000, false);

            adbwork.file.WriteLine("\n{0} Записываем и закрываем файл.", DateTime.Now.ToString("HH:mm:ss.fff"));
            adbwork.file.Close();
        }

        [STAThread]
        protected override void OnStart(string[] args)
        {
            log.Info("Запуск службы {0}", DateTime.Now.ToString("HH:mm:ss.fff"));
            Thread t = new Thread(new ThreadStart(startTimer));
            t.Start();
        }

        protected override void OnPause()
        {

        }

        protected override void OnContinue()
        {

        }

        protected override void OnStop()
        {

        }
    }
}