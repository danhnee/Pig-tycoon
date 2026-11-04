package com.pigtycoon.pig_tycoon_flutter_client

import android.content.Intent
import java.io.File
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel

class MainActivity : FlutterActivity() {
    private var launchResult: MethodChannel.Result? = null
    private var leftForUnity = false

    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)
        configureSaveChannel(flutterEngine)
        MethodChannel(flutterEngine.dartExecutor.binaryMessenger, "pigtycoon/unity")
            .setMethodCallHandler { call, result ->
                if (call.method != "launchGame") {
                    result.notImplemented()
                    return@setMethodCallHandler
                }
                val player = call.argument<String>("player")
                if (player != "An" && player != "Khoa") {
                    result.error("invalid_player", "Chỉ có An hoặc Khoa.", null)
                } else if (launchResult != null) {
                    result.error("unity_running", "Game đang chạy.", null)
                } else {
                    try {
                        val intent = Intent(this, UnityGameActivity::class.java)
                            .putExtra("pigtycoon.player", player)
                        launchResult = result
                        startActivity(intent)
                    } catch (e: RuntimeException) {
                        launchResult = null
                        result.error("unity_launch_failed", "Không mở được game: ${e.message}", null)
                    }
                }
            }
    }

    // Unity's Application.persistentDataPath is the app-specific external files
    // dir when it exists, otherwise internal storage. Check both for the save.
    private fun saveCandidates(): List<File> =
        listOfNotNull(getExternalFilesDir(null), filesDir).map { File(it, SAVE_NAME) }

    private fun configureSaveChannel(flutterEngine: FlutterEngine) {
        MethodChannel(flutterEngine.dartExecutor.binaryMessenger, "pigtycoon/save")
            .setMethodCallHandler { call, result ->
                if (launchResult != null) {
                    result.error("unity_running", "Không đọc/ghi bản lưu khi game đang chạy.", null)
                    return@setMethodCallHandler
                }
                try {
                    when (call.method) {
                        "readSave" -> {
                            val file = saveCandidates().filter { it.isFile }.maxByOrNull { it.lastModified() }
                            result.success(file?.let {
                                mapOf("json" to it.readText(), "modifiedMs" to it.lastModified())
                            })
                        }
                        "writeSave" -> {
                            val json = call.argument<String>("json")
                            if (json == null) {
                                result.error("bad_args", "Thiếu nội dung bản lưu.", null)
                                return@setMethodCallHandler
                            }
                            val target = saveCandidates().firstOrNull { it.isFile } ?: saveCandidates().first()
                            target.parentFile?.mkdirs()
                            // Same pattern as Unity: write a temp file, then replace.
                            val temp = File(target.path + ".pending")
                            temp.writeText(json)
                            if (!temp.renameTo(target)) {
                                target.delete()
                                if (!temp.renameTo(target)) throw java.io.IOException("Không thay được tệp lưu.")
                            }
                            result.success(null)
                        }
                        else -> result.notImplemented()
                    }
                } catch (e: Exception) {
                    result.error("save_io", "Lỗi đọc/ghi bản lưu: ${e.message}", null)
                }
            }
    }

    override fun onPause() {
        if (launchResult != null) leftForUnity = true
        super.onPause()
    }

    override fun onResume() {
        super.onResume()
        // Unity requires singleTask. It cannot return an activity result to a
        // different task, so complete only when Flutter actually resumes.
        if (leftForUnity) {
            leftForUnity = false
            val pending = launchResult
            launchResult = null
            // This acknowledges returning to the menu, not save success.
            // The Unity bridge acknowledges saves before finishing its activity.
            pending?.success(null)
        }
    }

    private companion object {
        const val SAVE_NAME = "pig_tycoon_save.json"
    }
}
