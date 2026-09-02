using System;
using System.Collections.Generic;
using MarcoZechner.ConfigAPI.V2.Api;
using MarcoZechner.ConfigAPI.V2.Domain;
using MarcoZechner.ConfigAPI.V2.Persistence;
using Mz.ApiProtocol;
using Mz.ApiProtocol.SpaceEngineers;
using Mz.ConfigApi;
using Mz.SemanticVersioning;
using NUnit.Framework;

namespace MarcoZechner.ConfigAPI.Tests.V2.Consumer
{
    [TestFixture]
    public sealed class ConfigApiSemanticClientTests
    {
        [Test]
        public void Consumer_Document_Wire_Format_Is_Provider_Compatible()
        {
            var document =
                new Mz.ConfigApi.ConfigDocument(
                    new ConfigEntry(
                        "Value",
                        ConfigValue.Integer(10)),
                    new ConfigEntry(
                        "Nested",
                        ConfigValue.Object(
                            new ConfigEntry(
                                "Enabled",
                                ConfigValue.Boolean(true)),
                            new ConfigEntry(
                                "Name",
                                ConfigValue.String("example")))),
                    new ConfigEntry(
                        "Items",
                        ConfigValue.Array(
                            ConfigValue.Integer(1),
                            ConfigValue.Null,
                            ConfigValue.Integer(3))));

            object payload =
                Mz.ConfigApi.ConfigDocumentWireCodec.Encode(
                    document);

            MarcoZechner.ConfigAPI.V2.Domain.ConfigDocument providerDocument =
                MarcoZechner.ConfigAPI.V2.Api.ConfigDocumentWireCodec.Decode(
                    payload);

            object providerPayload =
                MarcoZechner.ConfigAPI.V2.Api.ConfigDocumentWireCodec.Encode(
                    providerDocument);

            Mz.ConfigApi.ConfigDocument roundTrip =
                Mz.ConfigApi.ConfigDocumentWireCodec.Decode(
                    providerPayload);

            Assert.That(roundTrip.Equals(document), Is.True);
        }

        [Test]
        public void Consumer_Temporal_Wire_Values_RoundTrip_Through_Provider()
        {
            var date = new Mz.ConfigApi.ConfigDate(0, 2, 29);
            var time = new Mz.ConfigApi.ConfigTime(23, 59, 60, "0012300");

            var document =
                new Mz.ConfigApi.ConfigDocument(
                    new ConfigEntry(
                        "Offset",
                        ConfigValue.OffsetDateTime(
                            new Mz.ConfigApi.ConfigOffsetDateTime(
                                date,
                                time,
                                0,
                                true))),
                    new ConfigEntry(
                        "LocalDateTime",
                        ConfigValue.LocalDateTime(
                            new Mz.ConfigApi.ConfigLocalDateTime(
                                date,
                                time))),
                    new ConfigEntry(
                        "LocalDate",
                        ConfigValue.LocalDate(date)),
                    new ConfigEntry(
                        "LocalTime",
                        ConfigValue.LocalTime(time)));

            object consumerPayload =
                Mz.ConfigApi.ConfigDocumentWireCodec.Encode(document);

            MarcoZechner.ConfigAPI.V2.Domain.ConfigDocument providerDocument =
                MarcoZechner.ConfigAPI.V2.Api.ConfigDocumentWireCodec.Decode(
                    consumerPayload);

            object providerPayload =
                MarcoZechner.ConfigAPI.V2.Api.ConfigDocumentWireCodec.Encode(
                    providerDocument);

            Mz.ConfigApi.ConfigDocument roundTrip =
                Mz.ConfigApi.ConfigDocumentWireCodec.Decode(
                    providerPayload);

            Assert.That(roundTrip.Equals(document), Is.True);
        }

        [Test]
        public void Consumer_Temporal_Constructors_Reject_Provider_Invalid_States()
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentException>(
                    () => new Mz.ConfigApi.ConfigDate(2026, 2, 30));

                Assert.Throws<ArgumentException>(
                    () => new Mz.ConfigApi.ConfigTime(24, 0, 0));

                Assert.Throws<ArgumentException>(
                    () => new Mz.ConfigApi.ConfigTime(0, 0, 0, "12x"));

                Assert.Throws<ArgumentException>(
                    () => new Mz.ConfigApi.ConfigOffsetDateTime(
                        new Mz.ConfigApi.ConfigDate(2026, 9, 1),
                        new Mz.ConfigApi.ConfigTime(0, 0, 0),
                        1440));

                Assert.Throws<ArgumentException>(
                    () => new Mz.ConfigApi.ConfigOffsetDateTime(
                        new Mz.ConfigApi.ConfigDate(2026, 9, 1),
                        new Mz.ConfigApi.ConfigTime(0, 0, 0),
                        60,
                        true));
            });
        }

        [Test]
        public void Client_Open_And_Save_RoundTrip_Through_Real_Provider()
        {
            var bus = new RecordingModMessageBus();
            var registry = new ConfigConsumerRegistrationRegistry();

            var provider =
                new ConfigApiProvider(
                    bus,
                    registry,
                    new FixedClock(
                        new DateTime(
                            2026,
                            9,
                            2,
                            2,
                            0,
                            0,
                            DateTimeKind.Utc)),
                    new SemanticVersion(0, 1, 0));

            provider.Start();

            var storage =
                new MemoryConsumerStorage();

            var client =
                new ConfigApiClient(
                    bus,
                    "Example.Mod",
                    "Example Mod",
                    new SemanticVersion(1, 0, 0),
                    true,
                    "Uses ConfigAPI.",
                    storage.Read,
                    storage.Write);

            client.Start();

            var defaults =
                new Mz.ConfigApi.ConfigDocument(
                    new ConfigEntry(
                        "Value",
                        ConfigValue.Integer(10)));

            Mz.ConfigApi.ConfigDocument opened =
                client.Open(
                    "Settings",
                    Mz.ConfigApi.ConfigLocation.Local,
                    "settings.toml",
                    defaults);

            var edited =
                new Mz.ConfigApi.ConfigDocument(
                    new ConfigEntry(
                        "Value",
                        ConfigValue.Integer(25)));

            Mz.ConfigApi.ConfigDocument saved =
                client.Save(
                    "Settings",
                    Mz.ConfigApi.ConfigLocation.Local,
                    "settings.toml",
                    defaults,
                    edited);

            var persisted =
                MarcoZechner.ConfigAPI.V2.Serialization.ConfigTomlSourceDecoder.Decode(
                    storage.Get(0, "settings.toml"),
                    new MarcoZechner.ConfigAPI.V2.Domain.ConfigDocument(
                        new ConfigObjectNode(
                            new[]
                            {
                                new ConfigObjectEntry(
                                    "Value",
                                    ConfigScalarNode.Integer(10))
                            })));

            ConfigNode persistedValue;

            Assert.Multiple(() =>
            {
                Assert.That(client.IsConnected, Is.True);
                Assert.That(opened.Equals(defaults), Is.True);
                Assert.That(saved.Equals(edited), Is.True);
                Assert.That(
                    persisted.TryGet(
                        new ConfigValuePath("Value"),
                        out persistedValue),
                    Is.True);

                Assert.That(
                    persistedValue.Equals(
                        ConfigScalarNode.Integer(25)),
                    Is.True);
            });

            client.Dispose();
            provider.Dispose();
        }

        [Test]
        public void Open_And_Save_Use_The_Active_Registration_Id()
        {
            var bus = new RecordingModMessageBus();
            Guid registeredId = Guid.Empty;
            Guid openedId = Guid.Empty;
            Guid savedId = Guid.Empty;

            var endpoints =
                new Dictionary<string, Delegate>(
                    StringComparer.Ordinal);

            endpoints.Add(
                "RegisterConsumer",
                new Func<
                    string,
                    Guid,
                    Func<int, string, string>,
                    Action<int, string, string>,
                    Action>(
                    delegate(
                        string consumerId,
                        Guid registrationId,
                        Func<int, string, string> read,
                        Action<int, string, string> write)
                    {
                        registeredId = registrationId;
                        return delegate { };
                    }));

            endpoints.Add(
                "OpenConfig",
                new Func<
                    string,
                    Guid,
                    string,
                    int,
                    string,
                    object,
                    object>(
                    delegate(
                        string consumerId,
                        Guid registrationId,
                        string configKey,
                        int location,
                        string file,
                        object defaults)
                    {
                        openedId = registrationId;
                        return defaults;
                    }));

            endpoints.Add(
                "SaveConfig",
                new Func<
                    string,
                    Guid,
                    string,
                    int,
                    string,
                    object,
                    object,
                    object>(
                    delegate(
                        string consumerId,
                        Guid registrationId,
                        string configKey,
                        int location,
                        string file,
                        object defaults,
                        object playerValues)
                    {
                        savedId = registrationId;
                        return playerValues;
                    }));

            var provider =
                CreateDiscoveryProvider(
                    bus,
                    endpoints);

            provider.Start();

            var client =
                CreateClient(bus);

            client.Start();

            var document =
                new Mz.ConfigApi.ConfigDocument(
                    new ConfigEntry(
                        "Value",
                        ConfigValue.Integer(10)));

            client.Open(
                "Settings",
                Mz.ConfigApi.ConfigLocation.Global,
                "settings.toml",
                document);

            client.Save(
                "Settings",
                Mz.ConfigApi.ConfigLocation.Global,
                "settings.toml",
                document,
                document);

            Assert.Multiple(() =>
            {
                Assert.That(registeredId, Is.Not.EqualTo(Guid.Empty));
                Assert.That(openedId, Is.EqualTo(registeredId));
                Assert.That(savedId, Is.EqualTo(registeredId));
            });

            client.Dispose();
            provider.Dispose();
        }

        [Test]
        public void Open_And_Save_Reject_Disconnected_And_Invalid_Location_Calls()
        {
            var bus = new RecordingModMessageBus();
            var openCount = 0;
            var saveCount = 0;

            var endpoints =
                new Dictionary<string, Delegate>(
                    StringComparer.Ordinal)
                {
                    {
                        "RegisterConsumer",
                        new Func<
                            string,
                            Guid,
                            Func<int, string, string>,
                            Action<int, string, string>,
                            Action>(
                            delegate(
                                string consumerId,
                                Guid registrationId,
                                Func<int, string, string> read,
                                Action<int, string, string> write)
                            {
                                return delegate { };
                            })
                    },
                    {
                        "OpenConfig",
                        new Func<
                            string,
                            Guid,
                            string,
                            int,
                            string,
                            object,
                            object>(
                            delegate(
                                string consumerId,
                                Guid registrationId,
                                string configKey,
                                int location,
                                string file,
                                object defaults)
                            {
                                openCount++;
                                return defaults;
                            })
                    },
                    {
                        "SaveConfig",
                        new Func<
                            string,
                            Guid,
                            string,
                            int,
                            string,
                            object,
                            object,
                            object>(
                            delegate(
                                string consumerId,
                                Guid registrationId,
                                string configKey,
                                int location,
                                string file,
                                object defaults,
                                object playerValues)
                            {
                                saveCount++;
                                return playerValues;
                            })
                    }
                };

            var provider =
                CreateDiscoveryProvider(
                    bus,
                    endpoints);

            provider.Start();

            var client =
                CreateClient(bus);

            var document =
                new Mz.ConfigApi.ConfigDocument(
                    new ConfigEntry(
                        "Value",
                        ConfigValue.Integer(10)));

            Assert.Throws<InvalidOperationException>(
                () => client.Open(
                    "Settings",
                    Mz.ConfigApi.ConfigLocation.Local,
                    "settings.toml",
                    document));

            Assert.Throws<InvalidOperationException>(
                () => client.Save(
                    "Settings",
                    Mz.ConfigApi.ConfigLocation.Local,
                    "settings.toml",
                    document,
                    document));

            client.Start();

            Assert.Throws<ArgumentException>(
                () => client.Open(
                    "Settings",
                    (Mz.ConfigApi.ConfigLocation)99,
                    "settings.toml",
                    document));

            Assert.Throws<ArgumentException>(
                () => client.Save(
                    "Settings",
                    (Mz.ConfigApi.ConfigLocation)99,
                    "settings.toml",
                    document,
                    document));

            Assert.Throws<InvalidOperationException>(
                () => client.Open(
                    "Settings",
                    Mz.ConfigApi.ConfigLocation.World,
                    "settings.toml",
                    document));

            Assert.Throws<InvalidOperationException>(
                () => client.Save(
                    "Settings",
                    Mz.ConfigApi.ConfigLocation.World,
                    "settings.toml",
                    document,
                    document));

            Assert.Multiple(() =>
            {
                Assert.That(openCount, Is.EqualTo(0));
                Assert.That(saveCount, Is.EqualTo(0));
            });

            client.Dispose();
            provider.Dispose();
        }

        [Test]
        public void Client_Rejects_Provider_Missing_Exact_Persistence_Endpoint()
        {
            var bus = new RecordingModMessageBus();

            var endpoints =
                new Dictionary<string, Delegate>(
                    StringComparer.Ordinal)
                {
                    {
                        "RegisterConsumer",
                        new Func<
                            string,
                            Guid,
                            Func<int, string, string>,
                            Action<int, string, string>,
                            Action>(
                            delegate(
                                string consumerId,
                                Guid registrationId,
                                Func<int, string, string> read,
                                Action<int, string, string> write)
                            {
                                return delegate { };
                            })
                    },
                    {
                        "OpenConfig",
                        new Func<
                            string,
                            Guid,
                            string,
                            int,
                            string,
                            object,
                            object>(
                            delegate(
                                string consumerId,
                                Guid registrationId,
                                string configKey,
                                int location,
                                string file,
                                object defaults)
                            {
                                return defaults;
                            })
                    }
                };

            var provider =
                CreateDiscoveryProvider(
                    bus,
                    endpoints);

            provider.Start();

            var client =
                CreateClient(bus);

            client.Start();

            Assert.Multiple(() =>
            {
                Assert.That(client.IsConnected, Is.False);
                Assert.That(client.LastError, Is.Not.Null);
                Assert.That(
                    client.LastError.Message,
                    Does.Contain("SaveConfig"));
            });

            client.Dispose();
            provider.Dispose();
        }

        [Test]
        public void Clr_Mapper_RoundTrips_Property_Config_Shapes()
        {
            var original =
                new PropertyConfig
                {
                    Enabled = true,
                    Count = 17,
                    Ratio = 0.75f,
                    Name = "example",
                    CurrentMode = ExampleMode.Advanced,
                    OptionalValue = null,
                    Nested =
                        new NestedConfig
                        {
                            Threshold = 42,
                            Allowed = false
                        },
                    Tags =
                        new List<string>
                        {
                            "alpha",
                            "beta"
                        },
                    Levels =
                        new[]
                        {
                            2,
                            4,
                            8
                        },
                    NamedValues =
                        new Dictionary<string, int>(
                            StringComparer.Ordinal)
                        {
                            { "start", 1 },
                            { "end", 10 }
                        }
                };

            Mz.ConfigApi.ConfigDocument document =
                ConfigClrMapper.ToDocument(original);

            ConfigValue optional;
            Assert.That(
                document.TryGet(
                    "OptionalValue",
                    out optional),
                Is.True);

            PropertyConfig roundTrip =
                ConfigClrMapper.FromDocument<PropertyConfig>(
                    document);

            Assert.Multiple(() =>
            {
                Assert.That(optional.Kind, Is.EqualTo(ConfigValueKind.Null));
                Assert.That(roundTrip.Enabled, Is.True);
                Assert.That(roundTrip.Count, Is.EqualTo(17));
                Assert.That(roundTrip.Ratio, Is.EqualTo(0.75f));
                Assert.That(roundTrip.Name, Is.EqualTo("example"));
                Assert.That(roundTrip.CurrentMode, Is.EqualTo(ExampleMode.Advanced));
                Assert.That(roundTrip.OptionalValue, Is.Null);
                Assert.That(roundTrip.Nested, Is.Not.Null);
                Assert.That(roundTrip.Nested.Threshold, Is.EqualTo(42));
                Assert.That(roundTrip.Nested.Allowed, Is.False);
                Assert.That(roundTrip.Tags, Is.EqualTo(new[] { "alpha", "beta" }));
                Assert.That(roundTrip.Levels, Is.EqualTo(new[] { 2, 4, 8 }));
                Assert.That(
                    roundTrip.NamedValues,
                    Is.EqualTo(
                        new Dictionary<string, int>(
                            StringComparer.Ordinal)
                        {
                            { "start", 1 },
                            { "end", 10 }
                        }));
            });
        }

        [Test]
        public void Clr_Mapper_RoundTrips_Public_Field_Config()
        {
            var original =
                new FieldConfig
                {
                    Count = 12,
                    Name = "field-config"
                };

            Mz.ConfigApi.ConfigDocument document =
                ConfigClrMapper.ToDocument(original);

            FieldConfig roundTrip =
                ConfigClrMapper.FromDocument<FieldConfig>(
                    document);

            Assert.Multiple(() =>
            {
                Assert.That(roundTrip.Count, Is.EqualTo(12));
                Assert.That(roundTrip.Name, Is.EqualTo("field-config"));
            });
        }

        [Test]
        public void Clr_Mapper_Rejects_Ambiguous_Or_Unwritable_Schemas()
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<NotSupportedException>(
                    () => ConfigClrMapper.ToDocument(
                        new MixedMemberConfig { Count = 1, Value = 2 }));

                Assert.Throws<NotSupportedException>(
                    () => ConfigClrMapper.ToDocument(
                        new ReadOnlyPropertyConfig()));
            });
        }

        [Test]
        public void Clr_Mapper_Rejects_Object_Cycles()
        {
            var value =
                new CyclicConfig();

            value.Next = value;

            Assert.Throws<NotSupportedException>(
                () => ConfigClrMapper.ToDocument(value));
        }

        [Test]
        public void Clr_Mapper_Rejects_Numeric_Overflow_And_Unknown_Enum()
        {
            var oversizedInteger =
                new Mz.ConfigApi.ConfigDocument(
                    new ConfigEntry(
                        "Value",
                        ConfigValue.Integer(long.MaxValue)));

            var unknownEnum =
                new Mz.ConfigApi.ConfigDocument(
                    new ConfigEntry(
                        "CurrentMode",
                        ConfigValue.String("Unknown")));

            Assert.Multiple(() =>
            {
                Assert.Throws<NotSupportedException>(
                    () => ConfigClrMapper.ToDocument(
                        new UnsignedConfig
                        {
                            Value = ulong.MaxValue
                        }));

                Assert.Throws<ArgumentException>(
                    () => ConfigClrMapper.FromDocument<IntRangeConfig>(
                        oversizedInteger));

                Assert.Throws<ArgumentException>(
                    () => ConfigClrMapper.FromDocument<EnumOnlyConfig>(
                        unknownEnum));
            });
        }

        [Test]
        public void Typed_Definition_Open_And_Save_RoundTrip_Through_Real_Provider()
        {
            var bus = new RecordingModMessageBus();
            var registry = new ConfigConsumerRegistrationRegistry();

            var provider =
                new ConfigApiProvider(
                    bus,
                    registry,
                    new FixedClock(
                        new DateTime(
                            2026,
                            9,
                            2,
                            2,
                            30,
                            0,
                            DateTimeKind.Utc)),
                    new SemanticVersion(0, 1, 0));

            provider.Start();

            var storage =
                new MemoryConsumerStorage();

            var client =
                new ConfigApiClient(
                    bus,
                    "Example.Typed.Mod",
                    "Example Typed Mod",
                    new SemanticVersion(1, 0, 0),
                    true,
                    "Uses typed ConfigAPI.",
                    storage.Read,
                    storage.Write);

            client.Start();

            var defaultsCreated = 0;

            var definition =
                new ConfigDefinition<PropertyConfig>(
                    "TypedSettings",
                    "typed-settings.toml",
                    delegate
                    {
                        defaultsCreated++;
                        return CreatePropertyDefaults();
                    });

            PropertyConfig opened =
                client.Open(
                    definition,
                    Mz.ConfigApi.ConfigLocation.Local);

            opened.Count = 25;
            opened.CurrentMode = ExampleMode.Expert;
            opened.OptionalValue = null;
            opened.Nested.Allowed = false;
            opened.Tags.Add("gamma");
            opened.NamedValues["end"] = 99;

            PropertyConfig saved =
                client.Save(
                    definition,
                    Mz.ConfigApi.ConfigLocation.Local,
                    opened);

            PropertyConfig reopened =
                client.Open(
                    definition,
                    Mz.ConfigApi.ConfigLocation.Local);

            Assert.Multiple(() =>
            {
                Assert.That(defaultsCreated, Is.EqualTo(3));

                Assert.That(saved.Count, Is.EqualTo(25));
                Assert.That(saved.CurrentMode, Is.EqualTo(ExampleMode.Expert));
                Assert.That(saved.OptionalValue, Is.Null);
                Assert.That(saved.Nested.Allowed, Is.False);
                Assert.That(saved.Tags, Is.EqualTo(new[] { "alpha", "beta", "gamma" }));
                Assert.That(saved.NamedValues["end"], Is.EqualTo(99));

                Assert.That(reopened.Count, Is.EqualTo(25));
                Assert.That(reopened.CurrentMode, Is.EqualTo(ExampleMode.Expert));
                Assert.That(reopened.OptionalValue, Is.Null);
                Assert.That(reopened.Nested.Allowed, Is.False);
                Assert.That(reopened.Tags, Is.EqualTo(new[] { "alpha", "beta", "gamma" }));
                Assert.That(reopened.NamedValues["end"], Is.EqualTo(99));
            });

            client.Dispose();
            provider.Dispose();
        }

        private static PropertyConfig CreatePropertyDefaults()
        {
            return new PropertyConfig
            {
                Enabled = true,
                Count = 10,
                Ratio = 0.5f,
                Name = "default",
                CurrentMode = ExampleMode.Basic,
                OptionalValue = 5,
                Nested =
                    new NestedConfig
                    {
                        Threshold = 20,
                        Allowed = true
                    },
                Tags =
                    new List<string>
                    {
                        "alpha",
                        "beta"
                    },
                Levels =
                    new[]
                    {
                        1,
                        2,
                        3
                    },
                NamedValues =
                    new Dictionary<string, int>(
                        StringComparer.Ordinal)
                    {
                        { "start", 1 },
                        { "end", 10 }
                    }
            };
        }

        private enum ExampleMode
        {
            Basic,
            Advanced,
            Expert
        }

        private sealed class PropertyConfig
        {
            public bool Enabled { get; set; }
            public int Count { get; set; }
            public float Ratio { get; set; }
            public string Name { get; set; }
            public ExampleMode CurrentMode { get; set; }
            public int? OptionalValue { get; set; }
            public NestedConfig Nested { get; set; }
            public List<string> Tags { get; set; }
            public int[] Levels { get; set; }
            public Dictionary<string, int> NamedValues { get; set; }
        }

        private sealed class NestedConfig
        {
            public int Threshold { get; set; }
            public bool Allowed { get; set; }
        }

        private sealed class FieldConfig
        {
            public int Count;
            public string Name;
        }

        private sealed class MixedMemberConfig
        {
            public int Count;
            public int Value { get; set; }
        }

        private sealed class ReadOnlyPropertyConfig
        {
            public int Value
            {
                get
                {
                    return 10;
                }
            }
        }

        private sealed class UnsignedConfig
        {
            public ulong Value { get; set; }
        }

        private sealed class IntRangeConfig
        {
            public int Value { get; set; }
        }

        private sealed class EnumOnlyConfig
        {
            public ExampleMode CurrentMode { get; set; }
        }

        private sealed class CyclicConfig
        {
            public CyclicConfig Next { get; set; }
        }

        private static ConfigApiClient CreateClient(
            IModMessageBus bus)
        {
            return new ConfigApiClient(
                bus,
                "Example.Mod",
                "Example Mod",
                new SemanticVersion(1, 0, 0),
                true,
                "Uses ConfigAPI.",
                (location, file) => null,
                (location, file, content) => { });
        }

        private static ApiDiscoveryProvider CreateDiscoveryProvider(
            IModMessageBus bus,
            IDictionary<string, Delegate> endpoints)
        {
            return new ApiDiscoveryProvider(
                bus,
                new ApiModIdentity(
                    "MarcoZechner.ConfigAPI",
                    "ConfigAPI",
                    new SemanticVersion(0, 1, 0)),
                new ApiDescriptor(
                    "MarcoZechner.ConfigAPI",
                    new SemanticVersion(2, 0, 0)),
                endpoints);
        }

        private sealed class FixedClock :
            IConfigClock
        {
            public DateTime UtcNow { get; private set; }

            public FixedClock(
                DateTime utcNow)
            {
                UtcNow = utcNow;
            }
        }

        private sealed class MemoryConsumerStorage
        {
            private readonly Dictionary<string, string> _content =
                new Dictionary<string, string>(
                    StringComparer.Ordinal);

            public string Read(
                int location,
                string file)
            {
                string content;

                return _content.TryGetValue(
                    Key(location, file),
                    out content)
                    ? content
                    : null;
            }

            public void Write(
                int location,
                string file,
                string content)
            {
                _content[Key(location, file)] =
                    content;
            }

            public string Get(
                int location,
                string file)
            {
                string content;

                return _content.TryGetValue(
                    Key(location, file),
                    out content)
                    ? content
                    : null;
            }

            private static string Key(
                int location,
                string file)
            {
                return location + "|" + file;
            }
        }

        private sealed class RecordingModMessageBus :
            IModMessageBus
        {
            private readonly Dictionary<long, List<Action<object>>> _handlers =
                new Dictionary<long, List<Action<object>>>();

            public void RegisterHandler(
                long channelId,
                Action<object> handler)
            {
                List<Action<object>> handlers;

                if (!_handlers.TryGetValue(
                    channelId,
                    out handlers))
                {
                    handlers =
                        new List<Action<object>>();

                    _handlers.Add(
                        channelId,
                        handlers);
                }

                handlers.Add(handler);
            }

            public void UnregisterHandler(
                long channelId,
                Action<object> handler)
            {
                List<Action<object>> handlers;

                if (_handlers.TryGetValue(
                    channelId,
                    out handlers))
                {
                    handlers.Remove(handler);
                }
            }

            public void Send(
                long channelId,
                object payload)
            {
                List<Action<object>> handlers;

                if (!_handlers.TryGetValue(
                    channelId,
                    out handlers))
                {
                    return;
                }

                Action<object>[] snapshot =
                    handlers.ToArray();

                for (var i = 0;
                    i < snapshot.Length;
                    i++)
                {
                    snapshot[i](payload);
                }
            }
        }
    }
}
