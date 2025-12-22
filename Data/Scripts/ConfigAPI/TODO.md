- save / load / export, handle file extensions for the user
- check event unload, it doesn't work
- unify the log output to keep line length.
  - remove "Client.ConfigAPI" it is already in the file name
  - replace ":" with "." for milliseconds


```log
2025-12-21 03:53:59.609  INFO                   // type
2025-12-21 03:53:59.609  WARN
2025-12-21 03:53:59.609 ERROR
2025-12-21 03:53:59.609  INFO         Setup     // topics
2025-12-21 03:53:59.609  INFO CommandOutput
2025-12-21 03:53:59.609  INFO        Status
2025-12-21 03:53:59.609  INFO        Status // severity
2025-12-21 03:53:59.609  INFO        Status
2025-12-21 03:53:59.609  INFO        Status
2025-12-21 03:53:59.609  INFO        Status ThisIsMyGenericMessage
2025-12-21 03:53:59.609  INFO        Status ThisIsMy
                                            Multiline Message
2025-12-21 03:53:59.609  INFO        Status ThisIsMy
                                            Multiline Message
2025-12-21 03:53:59.609  INFO ------------- ConfigUserHooksImpl             
                                            . LoadFile (
                                                locationType                = LocationType.Local
                                                filename                    = "mz.ConfigAPIExample.MyConfig.toml.default.toml"
                                            )
2025-12-21 03:53:59.609  INFO ------------- ConfigUserHooksImplsvsesevsvsevsvsevsv             
                                            . LoadFile (
                                                locationType                = LocationType.Local
                                                filename                    = "mz.ConfigAPIExample.MyConfig.toml.default.toml"
                                            )
2025-12-21 03:53:59.609  INFO               ConfigUserHooksImpl             
                                            . SaveFile (
                                                locationType                = LocationType.Local
                                                filename                    = "mz.ConfigAPIExample.MyConfig.toml"
                                                content                     = 
                                                toml
                                                    [MyConfig]
                                                    ConfigVersion = "1.0.0"
                                                    # If true, the system will respond to hello messages.
                                                    RespondToHello = false
                                                    # The message to send when responding to hello.
                                                    GreetingMessage = "bye"
                                            )
2025-12-21 03:53:59.609  WARN ------------- ConfigUserHooksImpl             
                                            . SaveFile (
                                                locationType                = LocationType.Local
                                                filename                    = "mz.ConfigAPIExample.MyConfig.toml"
                                                content                     = "null"
                                            )
```               
                                                
Log.Trace(nameof(ConfigUserHooksImpl), nameof(SaveFile), new object[] {nameof(locationType), locationType, nameof(filename), filename, nameof(content), Log.Indent(content, "toml") ?? "null" });