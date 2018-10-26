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
using HomeKitAccessory.Data;
using HomeKitAccessory.Serialization;

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

            if (notificationTimer != null)
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
            return server.Accessories
                .FirstOrDefault(a => a.Id == accessoryId)?
                .FindCharacteristic(instanceId);
        }

        private JObject SerializeCharacteristic(
            Characteristic characteristic,
            AccessoryCharacteristicId id,
            bool includeAccessoryId,
            CharacteristicReadRequest options)
        {
            var result = new JObject();
            if (includeAccessoryId)
            {
                result["aid"] = id.AccessoryId;
            }
            result["iid"] = characteristic.Id;
            if (characteristic.CanRead)
            {
                result["value"] = JToken.FromObject(characteristic.Value);
            }
            if (options.IncludeEvent)
            {
                result["ev"] = subscriptions.ContainsKey(id);
            }
            if (options.IncludeType)
            {
                result["type"] = HapTypeConverter.Format(characteristic.Type);
            }
            if (options.IncludePerms)
            {
                var perms = new JArray();
                if (characteristic.CanRead) perms.Add("pr");
                if (characteristic.CanWrite) perms.Add("pw");
                if (characteristic is IObservable<object>) perms.Add("ev");
                result["perms"] = perms;
            }
            if (options.IncludeMeta)
            {
                result["format"] = FormatName(characteristic.Format);
                if (characteristic.MinValue.HasValue)
                    result["minValue"] = characteristic.MinValue.Value;
                if (characteristic.MaxValue.HasValue)
                    result["maxValue"] = characteristic.MaxValue.Value;
                if (characteristic.MinStep.HasValue)
                    result["minStep"] = characteristic.MinStep.Value;
                if (characteristic.MaxLen.HasValue)
                    result["maxLen"] = characteristic.MaxLen.Value;
                if (characteristic.MaxDataLen.HasValue)
                    result["maxDataLen"] = characteristic.MaxDataLen.Value;
                if (characteristic.Unit.HasValue)
                    result["unit"] = characteristic.Unit.Value.ToString().ToLowerInvariant();
                if (characteristic.ValidValues != null)
                    result["valid-values"] = JToken.FromObject(characteristic.ValidValues);
                if (characteristic.ValidValuesRange != null)
                    result["valid-values-range"] = new JArray()
                    {
                        characteristic.ValidValuesRange.Item1,
                        characteristic.ValidValuesRange.Item2
                    };
            }
            return result;
        }

        private static string FormatName(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(int)) return "int";
            if (type == typeof(double)) return "float";
            if (type == typeof(byte[])) return "data";
            if (type == typeof(TLVCollection)) return "tlv8";
            if (type == typeof(byte)) return "uint8";
            if (type == typeof(ushort)) return "uint16";
            if (type == typeof(uint)) return "uint32";
            if (type == typeof(ulong)) return "uint64";
            throw new ArgumentException("Unknown type " + type, nameof(type));
        }

        public JObject ReadCharacteristic(AccessoryCharacteristicId id, CharacteristicReadRequest options)
        {
            var characteristic = FindCharacteristic(id.AccessoryId, id.InstanceId);
            if (characteristic == null)
            {
                throw new NotExistException(id);
            }
            if (!characteristic.CanRead &&
                !options.IncludeEvent &&
                !options.IncludeMeta &&
                !options.IncludePerms &&
                !options.IncludeType)
            {
                // If the attempt is to just read the value of the characteristic, return an error, because it is not readable.
                // If there is a request for metadata of some kind, then the read request is valid, it just won't include a value.
                throw new WriteOnlyException(id);
            }
            return SerializeCharacteristic(characteristic, id, true, options);
        }

        public HapResponse HandleCharacteristicReadRequest(CharacteristicReadRequest request)
        {
            var results = new JArray();

            var anyErrors = false;

            foreach (var id in request.Ids)
            {
                JObject data;
                try
                {
                    data = ReadCharacteristic(id, request);
                }
                catch (HapException ex)
                {
                    anyErrors = true;
                    data = new JObject()
                    {
                        { "aid", ex.AccessoryId },
                        { "iid", ex.CharacteristicId },
                        { "status", ex.ErrorCode }
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    anyErrors = true;
                    data = new JObject()
                    {
                        { "aid", id.AccessoryId },
                        { "iid", id.InstanceId },
                        { "status", -70407 }
                    };
                }
                results.Add(data);
            }

            var response = new HapResponse();
            if (anyErrors)
            {
                if (results.Count == 1)
                {
                    response.Status = 400;
                }
                else
                {
                    response.Status = 207;
                    foreach (var result in results)
                    {
                        if (result["status"] == null)
                        {
                            result["status"] = 0;
                        }
                    }
                }
            }
            else {
                response.Status = 200;
            }
            response.Body = new JObject()
            {
                { "characteristics", results }
            };

            return response;
        }

        private void WriteCharacteristic(CharacteristicWriteItem item)
        {
            var ser = new JsonSerializer();
            ser.Converters.Add(new TLVConverter());

            var characteristic = FindCharacteristic(item.AccessoryId, item.InstanceId);
            if (characteristic == null)
            {
                throw new NotExistException(item.AccessoryId, item.InstanceId);
            }
            if (item.Value != null &!characteristic.CanWrite)
            {
                throw new ReadOnlyException(item.AccessoryId, item.InstanceId);
            }
            if (item.Events.HasValue && !(characteristic is IObservable<object>))
            {
                throw new NotificationNotSupportedException(item.AccessoryId, item.InstanceId);
            }

            if (item.Value != null)
            {
                try
                {
                    characteristic.Value = item.Value.ToObject(characteristic.Format, ser);
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new InvalidValueException(item.AccessoryId, item.InstanceId);
                }
                catch (ArgumentException)
                {
                    throw new InvalidValueException(item.AccessoryId, item.InstanceId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw new OutOfResourcesException(item.AccessoryId, item.InstanceId);
                }
            }
            
            if (item.Events.HasValue)
            {
                var itemid = (AccessoryCharacteristicId)item;
                if (item.Events.Value)
                {
                    if (!subscriptions.ContainsKey(itemid))
                    {
                        subscriptions[itemid] = ((IObservable<object>)characteristic).Subscribe(new Observer(this, itemid));
                    }
                }
                else
                {
                    if (subscriptions.TryGetValue(itemid, out IDisposable disposable))
                    {
                        subscriptions.Remove(itemid);
                        disposable.Dispose();
                    }
                }
            }
        }

        public HapResponse HandleCharacteristicWriteRequest(CharacteristicWriteRequest request)
        {
            var characteristics = new JArray();
            var anyErrors = false;

            foreach (var item in request.Characteristics)
            {
                var result = new JObject();
                characteristics.Add(result);
                result["aid"] = item.AccessoryId;
                result["iid"] = item.InstanceId;
                try
                {
                    WriteCharacteristic(item);
                    result["status"] = 0;
                }
                catch (HapException ex)
                {
                    result["status"] = ex.ErrorCode;
                }
            }

            var response = new HapResponse();

            if (anyErrors)
            {
                if (characteristics.Count > 1)
                {
                    response.Status = 207;
                }
                else
                {
                    response.Status = 400;
                }
                response.Body = new JObject()
                {
                    { "characteristics", characteristics }
                };
            }
            else
            {
                response.Status = 204;
            }

            return response;
        }

        public HapResponse GetAccessoryDatabase()
        {
            var options = new CharacteristicReadRequest
            {
                IncludeEvent = false,
                IncludeMeta = true,
                IncludePerms = true,
                IncludeType = true
            };

            var result = new JObject();
            var accessories = new JArray();
            result["accessories"] = accessories;
            foreach (var accessory in server.Accessories)
            {
                var acc = new JObject();
                accessories.Add(acc);
                acc["aid"] = accessory.Id;
                var services = new JArray();
                acc["services"] = services;
                foreach (var service in accessory.Services)
                {
                    var svc = new JObject();
                    services.Add(svc);
                    svc["iid"] = service.Id;
                    svc["type"] = HapTypeConverter.Format(service.Type);
                    var characteristics = new JArray();
                    svc["characteristics"] = characteristics;
                    foreach (var characteristic in service.Characteristics)
                    {
                        var id = new AccessoryCharacteristicId(accessory.Id, characteristic.Id);
                        characteristics.Add(SerializeCharacteristic(characteristic, id, false, options));
                    }
                }
            }
            return new HapResponse
            {
                Status = 200,
                Body = result
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