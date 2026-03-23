# Proguard rules for SAF Picker module

# Keep SafPickerBridge class accessible from Unity
-keep class com.vrplayer.saf.SafPickerBridge { *; }

# Keep SafPickerProxyActivity class
-keep class com.vrplayer.saf.SafPickerProxyActivity { *; }

# Keep JSON classes
-keep class org.json.** { *; }
