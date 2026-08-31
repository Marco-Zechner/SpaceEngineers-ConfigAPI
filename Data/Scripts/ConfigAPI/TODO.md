- on world unload mod didn't unregister, so on new load it will try to connect again while already being connected.
- it does unload but log goes into default, and causes a silent stacktrace in the default log

---

Mod should now be working?
Write test cases and check everything again for a user perspective.

----
- export still causes a file switch.
- i think there is data left in world storage?
- mod still tries to register twice...
- wrong default file name