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
using System.Diagnostics;
using HomeKitAccessory.Core;

namespace HomeKitAccessory.Net
{
    public class CharacteristicHandler : IDisposable
    {
        private Server server;
        private Timer notificationTimer;
        private Dictionary<AccessoryCharacteristicId, object> pendingNotifications;
        private Action<JObject> eventHandler;
        private Dictionary<AccessoryCharacteristicId, IDisposable> subscriptions;
        private object responseLock;

        public CharacteristicHandler(Server server, Action<JObject> eventHandler)
        {
            this.server = server;
            this.eventHandler = eventHandler;

            pendingNotifications = new Dictionary<AccessoryCharacteristicId, object>();
            subscriptions = new Dictionary<AccessoryCharacteristicId, IDisposable>();
            responseLock = new object();
        }

        public void Dispose()
        {
            Trace.TraceInformation("Disposing of CharacteristicHandler {0}", this.GetHashCode());

            notificationTimer.Dispose();
            foreach (var entry in subscriptions)
            {
                Trace.TraceInformation("Disposing of subscription to {0}", entry.Key);
                entry.Value.Dispose();
            }
        }

        private void CharacteristicChanged(AccessoryCharacteristicId id, object value)
        {
            lock (pendingNotifications)
             {
                pendingNotifications[id] = value;
                if (notificationTimer == null) {
                    notificationTimer = new Timer(OnNotificationTimer, null, 250, Timeout.Infinite);
                }
            }
        }

        private class Observer : IObserver<object>
        {
            private CharacteristicHandler connection;
            private AccessoryCharacteristicId id;

            public Observer(CharacteristicHandler connection, AccessoryCharacteristicId id)
            {
                this.connection = connection;
                this.id = id;
            }

            public void OnCompleted() {}

            public void OnError(Exception error) {}

            public void OnNext(object value)
            {
                connection.CharacteristicChanged(id, value);
            }
        }

        private Characteristic FindCharacteristic(ulong accessoryId, ulong instanceId)
        {
            var accessory = server.Accessories.FirstOrDefault(a => a.Id == accessoryId);
            if (accessory == null) return null;
            var characteristic = accessory.Services.SelectMany(s => s.Characteristics).FirstOrDefault(c => c.Id == instanceId);
            return characteristic;
        }

        public Task<HapResponse> HandleCharacteristicReadRequest(CharacteristicReadRequest request)
        {
            var tasks = new List<Task>();
            var results = new JArray();

            foreach (var id in request.Ids) {
                var characteristic = FindCharacteristic(id.AccessoryId, id.InstanceId);
                var result = new JObject();
                result["aid"] = id.AccessoryId;
                result["iid"] = id.InstanceId;
                results.Add(result);

                if (characteristic == null) {
                    result["status"] = -70409;
                } 
                else if (characteristic.Read == null) {
                    result["status"] = -70405;
                }
                else {
                    if (request.IncludeType) {
                        result["type"] = HapTypeConverter.Format(characteristic.Type);
                    }
                    if (request.IncludePerms) {
                        result["perms"] = CharacteristicConverter.FormatPerms(characteristic);
                    }
                    if (request.IncludeEvent && characteristic.Observable != null) {
                        result["ev"] = subscriptions.ContainsKey(id);
                    }
                    if (request.IncludeMeta) {
                        CharacteristicConverter.PopulateMeta(characteristic, result);
                    }
                    tasks.Add(characteristic.Read().ContinueWith(task => {
                        if (task.IsFaulted) {
                            result["status"] = -70407;
                        }
                        else {
                            result["status"] = 0;
                            result["value"] =  JToken.FromObject(task.Result);
                        }
                    }));
                }
            }

            return Task.WhenAll(tasks).ContinueWith(allReads => {
                var response = new HapResponse();
                var includeStatus = results.Any(x => (int)x["status"] != 0);
                if (!includeStatus)
                {
                    response.Status = 200;
                }
                else if (results.Count == 1)
                {
                    response.Status = 400;
                }
                else
                {
                    response.Status = 207;
                }

                response.Body = new JObject() {
                    {"characteristics", results}
                };

                return response;
            });
        }

        public Task<HapResponse> HandleCharacteristicWriteRequest(CharacteristicWriteRequest request)
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
                else if (characteristic.Observable == null && item.Events.HasValue) {
                    result["status"] = -70406;
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
                    else
                    {
                        result["status"] = 0;
                    }

                    if (item.Events.HasValue) {
                        var itemid = (AccessoryCharacteristicId)item;
                        if (item.Events.Value) {
                            if (!subscriptions.ContainsKey(itemid))
                            {
                                subscriptions[itemid] = characteristic.Observable.Subscribe(new Observer(this, itemid));
                            }
                        }
                        else {
                            if (subscriptions.TryGetValue(itemid, out IDisposable disposable))
                            {
                                subscriptions.Remove(itemid);
                                disposable.Dispose();
                            }
                        }
                    }
                }
            }

            return Task.WhenAll(tasks).ContinueWith(allWrites => {
                var response = new HapResponse();
                
                if (characteristics.All(c => (int)c["status"] == 0)) {
                    response.Status = 204;
                }
                else {
                    response.Body = new JObject() {
                        {"characteristics", characteristics}
                    };
                    if (request.Characteristics.Count > 1) {
                        response.Status = 207;
                    }
                    else {
                        response.Status = 400;
                    }
                }
                return response;
            });
        }

        public HapResponse GetAccessoryDatabase()
        {
            var serializer = new JsonSerializer();
            serializer.Converters.Add(new CharacteristicConverter());
            var accessories = JToken.FromObject(server.Accessories, serializer);
            return new HapResponse
            {
                Status = 200,
                Body = new JObject()
                {
                    { "accessories", accessories }
                }
            };
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

            if (characteristics.Count > 0) {
                eventHandler.Invoke(new JObject() {
                    { "characteristics", characteristics }
                });
            }
        }
    }
}