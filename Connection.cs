using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HomeKitAccessory
{
    public class Connection
    {
        private Server server;
        private Timer notificationTimer;
        private Dictionary<AccessoryCharacteristicId, object> pendingNotifications;
        private Stream client;
        private Dictionary<AccessoryCharacteristicId, IDisposable> subscriptions;
        private object responseLock;

        public Connection(Server server, Stream client)
        {
            this.server = server;
            this.client = client;

            pendingNotifications = new Dictionary<AccessoryCharacteristicId, object>();
            subscriptions = new Dictionary<AccessoryCharacteristicId, IDisposable>();
            responseLock = new object();
        }

        private void CharacteristicChanged(object sender, CharacteristicChangedEventArgs e)
        {
            var characteristic = (Characteristic)sender;
            var id = new AccessoryCharacteristicId(
                characteristic.Accessory.Id,
                 characteristic.InstanceId);
            lock (pendingNotifications)
             {
                pendingNotifications[id] = e.Value;
                if (notificationTimer == null) {
                    notificationTimer = new Timer(OnNotificationTimer, null, 250, Timeout.Infinite);
                }
            }
        }

        private Characteristic FindCharacteristic(int accessoryId, int instanceId)
        {
            var accessory = server.Accessories.Find(a => a.Id == accessoryId);
            if (accessory == null) return null;
            var characteristic = accessory.Characteristics.Find(c => c.InstanceId == instanceId);
            return characteristic;
        }

        private JObject CharacteristicError(AccessoryCharacteristicId id, int status)
        {
            return new JObject() {
                {"aid", id.AccessoryId},
                {"iid", id.InstanceId},
                {"status", status}
            };
        }

        private static string FormatType(Guid type)
        {
            var typeString = type.ToString();
            if (typeString.EndsWith("-0000-1000-8000-0026BB765291")) {
                typeString = typeString.Substring(0, 8).TrimStart('0');
            }
            return typeString;
        }

        private static JArray FormatPerms(Characteristic characteristic)
        {
            var perms = new JArray();
            if (characteristic.Read != null) {
                perms.Add("pr");
            }
            if (characteristic.Write != null) {
                perms.Add("pw");
            }
            if (characteristic.Subscribe != null) {
                perms.Add("ev");
            }
            return perms;
        }

        private string FormatUnit(CharacteristicUnit unit)
        {
            switch (unit) {
                case CharacteristicUnit.ARCDEGREES:
                    return "arcdegrees";
                case CharacteristicUnit.CELSIUS:
                    return "celsius";
            }
            throw new ArgumentOutOfRangeException(nameof(unit));
        }

        private void PopulateMeta(Characteristic characteristic, JObject result)
        {
            characteristic.Format.PopulateMeta(result);
            if (characteristic.Unit.HasValue)
                result["unit"] = FormatUnit(characteristic.Unit.Value);
        }

        private void HandleCharacteristicReadRequest(CharacteristicReadRequest request)
        {
            var characteristics = new JArray();
            var tasks = new List<Task>();
            foreach (var id in request.Ids) {
                var result = new JObject();
                characteristics.Add(result);
                result["aid"] = id.AccessoryId;
                result["iid"] = id.InstanceId;
                var characteristic = FindCharacteristic(id.AccessoryId, id.InstanceId);
                if (characteristic == null) {
                    result["status"] = -70409;
                } 
                else if (characteristic.Read == null) {
                    result["status"] = -70405;
                }
                else {
                    if (request.IncludeType) {
                        result["type"] = FormatType(characteristic.Type);
                    }
                    if (request.IncludePerms) {
                        result["perms"] = FormatPerms(characteristic);
                    }
                    if (request.IncludeEvent) {
                        result["ev"] = subscriptions.ContainsKey(id);
                    }
                    if (request.IncludeMeta) {
                        PopulateMeta(characteristic, result);
                    }
                    tasks.Add(characteristic.Read().ContinueWith(task => {
                        if (task.IsFaulted) {
                            result["status"] = -70407;
                        }
                        else {
                            result["status"] = 0;
                            result["value"] = JToken.FromObject(task.Result);
                        }
                    }));
                }
            }
            Task.WhenAll(tasks).ContinueWith(allReads => {
                string statusLine;
                if (characteristics.Any(c => (int)c["status"] != 0)) {
                    statusLine = "HTTP/1.1 207 Multi-Status\r\n";
                }
                else {
                    statusLine = "HTTP/1.1 200 OK\r\n";
                    foreach (JObject c in characteristics) {
                        c.Remove("status");
                    }
                }
                var body = Encoding.UTF8.GetBytes(
                    new JObject() {
                        {"characteristics", characteristics}
                    }.ToString());
                var header = Encoding.UTF8.GetBytes(
                    statusLine
                    + "Content-Type: application/hap+json\r\n"
                    + "Content-Length: " + body.Length + "\r\n"
                    + "\r\n");
                lock (responseLock) {
                    client.Write(header);
                    client.Write(body);
                }
            });
        }

        private void HandleCharacteristicWriteRequest(CharacteristicWriteRequest request)
        {
            var characteristics = new JArray();
            var tasks = new List<Task>();

            foreach (var item in request.Characteristics) {
                var result = new JObject();
                characteristics.Add(result);
                result["aid"] = item.AccessoryId;
                result["iid"] = item.InstanceId;
                var characteristic = FindCharacteristic(item.AccessoryId, item.InstanceId);
                if (characteristic == null) {
                    result["status"] = -70409;
                }
                else if (characteristic.Write == null && item.Value != null) {
                    result["status"] = -70404;
                }
                else {
                    if (item.Value != null) {
                        try {
                            tasks.Add(characteristic.Write(characteristic.Format.Coerce(item.Value)).ContinueWith(task => {
                                if (task.IsFaulted) {
                                    result["status"] = -70407;
                                }
                                else {
                                    result["status"] = 0;
                                }
                            }));
                        }
                        catch (ArgumentOutOfRangeException) {
                            result["status"] = -70410;
                        }
                        catch (ArgumentException) {
                            result["status"] = -70410;
                        }
                    }
                }
            }

            Task.WhenAll(tasks).ContinueWith(allWrites => {
                var body = Encoding.UTF8.GetBytes(
                    new JObject() {
                        {"characteristics", characteristics}
                    }.ToString());

                string header;
                if (characteristics.All(c => (int)c["status"] == 0)) {
                    header = "HTTP/1.1 204 No Content\r\n\r\n";
                    body = null;
                }
                else {
                    if (request.Characteristics.Count > 1) {
                        header = "HTTP/1.1 207 Multi-Status\r\n";
                    }
                    else {
                        header = "HTTP/1.1 400 Bad Request\r\n";
                    }
                    header += "Content-Type: application/hap+json\r\n\r\n"
                        + "Content-Length: " + body.Length + "\r\n\r\n";
                }

                lock (responseLock) {
                    client.Write(Encoding.UTF8.GetBytes(header));
                    if (body != null) {
                        client.Write(body);
                    }
                }
            });
        }

        private void OnNotificationTimer(object state)
        {
            var characteristics = new JArray();
            lock (pendingNotifications)
            {
                foreach (var kv in pendingNotifications)
                {
                    characteristics.Add(new JObject() {
                        {"aid", kv.Key.AccessoryId},
                        {"iid", kv.Key.InstanceId},
                        {"value", JToken.FromObject(kv.Value)}
                    });
                }
                pendingNotifications.Clear();

                if (characteristics.Count == 0)
                {
                    notificationTimer.Dispose();
                    notificationTimer = null;
                }
                else
                {
                    notificationTimer.Change(1000, Timeout.Infinite);
                }
            }

            SendEvent(characteristics);
        }

        private void SendEvent(JArray characteristics)
        {
            var body = Encoding.UTF8.GetBytes(new JObject() {
                {"characteristics", characteristics}
            }.ToString());
            var header = Encoding.UTF8.GetBytes(
                "EVENT/1.0 200 OK\r\n" +
                "Content-Type: application/hap+json\r\n" +
                "Content-Length: " + body.Length + "\r\n" +
                "\r\n");
                
            lock (responseLock) {
                client.Write(header);
                client.Write(body);
            }
        }

    }
}