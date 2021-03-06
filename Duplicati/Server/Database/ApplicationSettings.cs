//  Copyright (C) 2011, Kenneth Skovhede

//  http://www.hexad.dk, opensource@hexad.dk
//
//  This library is free software; you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 2.1 of the
//  License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful, but
//  WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;
using System.Linq;
using System.Collections.Generic;

namespace Duplicati.Server.Database
{
    public class ApplicationSettings
    {
        private class CONST
        {
            public const string STARTUP_DELAY = "startup-delay";
            public const string DOWNLOAD_SPEED_LIMIT = "max-download-speed";
            public const string UPLOAD_SPEED_LIMIT = "max-upload-speed";
            public const string THREAD_PRIORITY = "thread-priority";
            public const string LAST_WEBSERVER_PORT = "last-webserver-port";
            public const string IS_FIRST_RUN = "is-first-run";
            public const string SERVER_PORT_CHANGED = "server-port-changed";
            public const string SERVER_PASSPHRASE = "server-passphrase";
            public const string SERVER_PASSPHRASE_SALT = "server-passphras-salt";
            public const string UPDATE_CHECK_LAST = "last-update-check";
            public const string UPDATE_CHECK_INTERVAL = "update-check-interval";
            public const string UPDATE_CHECK_NEW_VERSION = "update-check-latest";
            public const string UNACKED_ERROR = "unacked-error";
            public const string UNACKED_WARNING = "unacked-warning";
        }
        
        private Dictionary<string, string> m_values;
        private Library.AutoUpdater.UpdateInfo m_latestUpdate;

        internal ApplicationSettings(Connection con)
        {
            var settings = con.GetSettings(Connection.APP_SETTINGS_ID).ToDictionary(x => x.Name, x => x.Value);
            m_values = new Dictionary<string, string>();
            
            string nx;
            foreach(var n in typeof(CONST).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Static).Select(x => (string)x.GetValue(null)))
            {
                settings.TryGetValue(n, out nx);
                m_values[n] = nx;
            }
        }
        
        private void SaveSettings()
        {
            Program.DataConnection.SetSettings(
                from n in m_values
                select (Duplicati.Server.Serialization.Interface.ISetting)new Setting() {
                    Filter = "",
                    Name = n.Key,
                    Value = n.Value
                }, Database.Connection.APP_SETTINGS_ID);
        }
        
        public string StartupDelayDuration
        {
            get 
            {
                return m_values[CONST.STARTUP_DELAY];
            }
            set
            {
                lock(Program.DataConnection.m_lock)
                    m_values[CONST.STARTUP_DELAY] = value;
                SaveSettings();
            }
        }
        
        public System.Threading.ThreadPriority? ThreadPriorityOverride
        {
            get
            {
                var tp = m_values[CONST.THREAD_PRIORITY];
                if (string.IsNullOrEmpty(tp))
                    return null;
                  
                System.Threading.ThreadPriority r;  
                if (Enum.TryParse<System.Threading.ThreadPriority>(tp, true, out r))
                    return r;
                
                return null;
            }
            set
            {
                lock(Program.DataConnection.m_lock)
                    m_values[CONST.THREAD_PRIORITY] = value.HasValue ? Enum.GetName(typeof(System.Threading.ThreadPriority), value.Value) : null;
            }
        }
        
        public string DownloadSpeedLimit
        {
            get 
            {
                return m_values[CONST.DOWNLOAD_SPEED_LIMIT];
            }
            set
            {
                lock(Program.DataConnection.m_lock)
                    m_values[CONST.DOWNLOAD_SPEED_LIMIT] = value;
                SaveSettings();
            }
        }
        
        public string UploadSpeedLimit
        {
            get 
            {
                return m_values[CONST.UPLOAD_SPEED_LIMIT];
            }
            set
            {
                lock(Program.DataConnection.m_lock)
                    m_values[CONST.UPLOAD_SPEED_LIMIT] = value;
                SaveSettings();
            }
        }

        public bool IsFirstRun
        {
            get
            {
                var tp = m_values[CONST.IS_FIRST_RUN];
                if (string.IsNullOrEmpty(tp))
                    return true;

                return Duplicati.Library.Utility.Utility.ParseBoolOption(m_values, CONST.IS_FIRST_RUN);
            }
            set
            {
                lock(Program.DataConnection.m_lock)
                    m_values[CONST.IS_FIRST_RUN] = value.ToString();
                SaveSettings();
            }
        }

        public bool UnackedError
        {
            get
            {
                return Duplicati.Library.Utility.Utility.ParseBoolOption(m_values, CONST.UNACKED_ERROR);
            }
            set
            {
                lock(Program.DataConnection.m_lock)
                    m_values[CONST.UNACKED_ERROR] = value.ToString();
                SaveSettings();
            }
        }

        public bool UnackedWarning
        {
            get
            {
                return Duplicati.Library.Utility.Utility.ParseBoolOption(m_values, CONST.UNACKED_WARNING);
            }
            set
            {
                lock(Program.DataConnection.m_lock)
                    m_values[CONST.UNACKED_WARNING] = value.ToString();
                SaveSettings();
            }
        }
        public bool ServerPortChanged
        {
            get
            {
                return Duplicati.Library.Utility.Utility.ParseBoolOption(m_values, CONST.SERVER_PORT_CHANGED);
            }
            set
            {
                lock(Program.DataConnection.m_lock)
                    m_values[CONST.SERVER_PORT_CHANGED] = value.ToString();
                SaveSettings();
            }
        }

        public int LastWebserverPort
        {
            get
            {
                var tp = m_values[CONST.LAST_WEBSERVER_PORT];
                int p;
                if (string.IsNullOrEmpty(tp) || !int.TryParse(tp, out p))
                    return -1;

                return p;
            }
            set
            {
                lock(Program.DataConnection.m_lock)
                    m_values[CONST.LAST_WEBSERVER_PORT] = value.ToString();
                SaveSettings();
            }
        }

        public string WebserverPassword
        {
            get 
            {
                return m_values[CONST.SERVER_PASSPHRASE];
            }
        }

        public string WebserverPasswordSalt
        {
            get 
            {
                return m_values[CONST.SERVER_PASSPHRASE_SALT];
            }
        }

        public void SetWebserverPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                lock(Program.DataConnection.m_lock)
                {
                    m_values[CONST.SERVER_PASSPHRASE] = "";
                    m_values[CONST.SERVER_PASSPHRASE_SALT] = "";
                }
                SaveSettings();
            }
            else
            {
                var prng = System.Security.Cryptography.RNGCryptoServiceProvider.Create();
                var buf = new byte[32];
                prng.GetBytes(buf);
                var salt = Convert.ToBase64String(buf);

                var sha256 = System.Security.Cryptography.SHA256.Create();
                var str = System.Text.Encoding.UTF8.GetBytes(password);

                sha256.TransformBlock(str, 0, str.Length, str, 0);
                sha256.TransformFinalBlock(buf, 0, buf.Length);
                var pwd = Convert.ToBase64String(sha256.Hash);

                lock(Program.DataConnection.m_lock)
                {
                    m_values[CONST.SERVER_PASSPHRASE] = pwd;
                    m_values[CONST.SERVER_PASSPHRASE_SALT] = salt;
                }

                SaveSettings();
            }
        }

        public DateTime LastUpdateCheck
        {
            get 
            {
                long t;
                if (long.TryParse(m_values[CONST.UPDATE_CHECK_LAST], out t))
                    return new DateTime(t, DateTimeKind.Utc);
                else
                    return new DateTime(0, DateTimeKind.Utc);
            }
            set
            {
                lock(Program.DataConnection.m_lock)
                    m_values[CONST.UPDATE_CHECK_LAST] = value.ToUniversalTime().Ticks.ToString();
                SaveSettings();
            }
        }

        public string UpdateCheckInterval
        {
            get
            {
                var tp = m_values[CONST.UPDATE_CHECK_INTERVAL];
                if (string.IsNullOrWhiteSpace(tp))
                    tp = "1W";

                return tp;
            }
            set
            {
                lock(Program.DataConnection.m_lock)
                    m_values[CONST.UPDATE_CHECK_INTERVAL] = value;
                SaveSettings();
                Program.UpdatePoller.Reschedule();
            }
        }

        public DateTime NextUpdateCheck
        {
            get
            {
                try
                {
                    return Duplicati.Library.Utility.Timeparser.ParseTimeInterval(UpdateCheckInterval, LastUpdateCheck);
                }
                catch
                {
                    return LastUpdateCheck.AddDays(7);
                }
            }
        }

        public Library.AutoUpdater.UpdateInfo UpdatedVersion
        {
            get
            {
                if (string.IsNullOrWhiteSpace(m_values[CONST.UPDATE_CHECK_NEW_VERSION]))
                    return null;

                try
                {
                    if (m_latestUpdate != null)
                        return m_latestUpdate;

                    using(var tr = new System.IO.StringReader(m_values[CONST.UPDATE_CHECK_NEW_VERSION]))
                        return m_latestUpdate = Server.Serialization.Serializer.Deserialize<Library.AutoUpdater.UpdateInfo>(tr);
                }
                catch
                {
                }

                return null;
            }
            set
            {
                string result = null;
                if (value != null)
                {
                    var sb = new System.Text.StringBuilder();
                    using(var tw = new System.IO.StringWriter(sb))
                        Server.Serialization.Serializer.SerializeJson(tw, value);

                    result = sb.ToString();
                }

                m_latestUpdate = value;
                lock(Program.DataConnection.m_lock)
                    m_values[CONST.UPDATE_CHECK_NEW_VERSION] = result;

                SaveSettings();
            }
        }
    }
}

