using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;
using NLog;
using NLog.Config;
using System.Data;

namespace ADBReaderService
{
    public static class ADBReader
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        public static string server { get; set; }
        public static int port { get; set; }
        private static string dbname { get; set; }
        public static string user { get; set; }
        public static string password { get; set; }

        private static MySqlConnection connection = null;
        public static MySqlConnection Connection
        {
            get { return connection; }
        }

        public static bool Connect()
        {
            bool result = true;

            server = "ovz1.akolomiec.znog6.vps.myjino.ru";
            port = 49283;
            dbname = "adb";
            user = "root";
            password = "KRUS56_ak+";

            if (String.IsNullOrEmpty(dbname))
                result = false;
            string connstring = string.Format("Server={0}; Port={1}; database={2}; UID={3}; password={4}", server, port, dbname, user, password);
            connection = new MySqlConnection(connstring);
            connection.Open();
            result = true;

            return result;
        }

        public static void Close()
        {
            connection.Close();
        }

        // получить список всех устройств из БД
        [STAThread]
        public static List<Device> getDevicesList()
        {
            List<Device> result = new List<Device>();

            try
            {
                Connect();
                string cmdtext = "SELECT d.id as id, d.dev_id as dev_id, d.adb_status as adb_status, d.machine as machine, d.used as used, d.appium_port as appium_port, d.bootstrap_port as bootstrap_port, d.pid as pid FROM devices as d";
                MySqlCommand cmd = new MySqlCommand(cmdtext, connection);
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new Device(int.Parse(reader["id"].ToString()),
                        reader["dev_id"].ToString(),
                        reader["adb_status"].ToString(),
                        reader["machine"].ToString(),
                        bool.Parse(reader["used"].ToString()),
                        int.Parse(reader["appium_port"].ToString()),
                        int.Parse(reader["bootstrap_port"].ToString()),
                        int.Parse(reader["pid"].ToString())));
                }
                reader.Close();
                Close();
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
            }

            return result;
        }

        // поставить статус оффлайн всем устройствам из базы данных
        [STAThread]
        public static bool DevicesSetOffline()
        {
            bool result = true;

            Connect();
            string cmdtext = "UPDATE devices SET adb_status='offline' WHERE used=0;";
            MySqlCommand cmd = new MySqlCommand(cmdtext, connection);
            cmd.ExecuteNonQuery();
            Close();

            return result;
        }

        // поставить статус оффлайн всем устройствам для указанного компьютера
        [STAThread]
        public static bool DevicesSetOffline(string machine)
        {
            bool result = true;

            Connect();
            string cmdtext = string.Format("UPDATE devices SET adb_status='offline' WHERE machine='{0}' AND used=0;", machine);
            MySqlCommand cmd = new MySqlCommand(cmdtext, connection);
            cmd.ExecuteNonQuery();
            Close();

            return result;
        }

        // проверка существования устройства в базе данных
        [STAThread]
        public static bool DeviceExists(string dev_id)
        {
            bool result = false;

            //Connect();
            string cmdtext = string.Format("SELECT COUNT(dev_id) AS c FROM devices WHERE dev_id='{0}'", dev_id);
            MySqlCommand cmd = new MySqlCommand(cmdtext, connection);
            MySqlDataReader reader = cmd.ExecuteReader();
            reader.Read();
            result = (int.Parse(reader["c"].ToString()) > 0);
            //Close();

            return result;
        }

        // добавление устройства
        //[STAThread]
        //public static bool DeviceAdd(string dev_id, string adb_status)
        //{
        //    bool result = true;

        //    if (!DeviceExists(dev_id))
        //    {
        //        Connect();
        //        string cmdtext = string.Format("INSERT INTO devices (dev_id, adb_status, used, appium_port, bootstrap_port, pid) VALUES ('{0}', '{1}', 0, 0, 0, 0)", dev_id, adb_status);
        //        MySqlCommand cmd = new MySqlCommand(cmdtext, connection);
        //        cmd.ExecuteNonQuery();
        //        Close();
        //    }
        //    else
        //    {
        //        Connect();
        //        string cmdtext = string.Format("UPDATE devices SET adb_status='{1}', appium_port=0, bootstrap_port=0, pid=-1 WHERE dev_id='{0}' AND used=0", dev_id, adb_status);
        //        MySqlCommand cmd = new MySqlCommand(cmdtext, connection);
        //        cmd.ExecuteNonQuery();
        //        Close();
        //    }

        //    return result;
        //}

        [STAThread]
        public static bool DevicesAdd(List<Device> devices)
        {
            bool result = true;

            Connect();
            MySqlTransaction transaction = connection.BeginTransaction();

            foreach (Device d in devices)
            {
                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = connection;
                cmd.Transaction = transaction;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "insert_device";
                cmd.Parameters.AddWithValue("p_dev_id", d.dev_id);
                cmd.Parameters.AddWithValue("p_adb_status", d.adb_status);
                cmd.Parameters.AddWithValue("p_machine", d.machine);
                cmd.ExecuteNonQuery();
            }

            transaction.Commit();
            Close();

            return result;
        }
    }
}