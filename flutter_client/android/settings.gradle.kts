pluginManagement {
    val flutterSdkPath =
        run {
            val properties = java.util.Properties()
            file("local.properties").inputStream().use { properties.load(it) }
            val flutterSdkPath = properties.getProperty("flutter.sdk")
            require(flutterSdkPath != null) { "flutter.sdk not set in local.properties" }
            flutterSdkPath
        }

    includeBuild("$flutterSdkPath/packages/flutter_tools/gradle")

    repositories {
        google()
        mavenCentral()
        gradlePluginPortal()
    }
}

plugins {
    id("dev.flutter.flutter-plugin-loader") version "1.0.0"
    id("com.android.application") version "9.1.1" apply false
    id("com.android.library") version "9.1.1" apply false
    id("org.jetbrains.kotlin.android") version "2.4.0" apply false
}

include(":app")

// Export from the pinned Unity 6000.6.3f1 project. Never build a shell APK
// that silently omits gameplay when the export has not been produced.
val unityExport = file("../../Builds/AndroidExport/unityLibrary")
if (!unityExport.resolve("build.gradle").isFile) {
    throw GradleException("Missing Unity export: ${unityExport.absolutePath}. Export Unity 6000.6.3f1 to Builds/AndroidExport before building the Flutter Android app.")
}
include(":unityLibrary")
project(":unityLibrary").projectDir = unityExport
dependencyResolutionManagement {
    repositories {
        google()
        mavenCentral()
        flatDir { dirs(unityExport.resolve("libs")) }
    }
}
