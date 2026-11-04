package com.pigtycoon.pig_tycoon_flutter_client;

import android.os.Bundle;
import android.os.Build;
import android.window.OnBackInvokedCallback;
import android.window.OnBackInvokedDispatcher;
import com.unity3d.player.UnityPlayer;
import com.unity3d.player.UnityPlayerActivity;
import java.lang.ref.WeakReference;

/** Real full-screen Unity activity. Runs in :unity so quitting cannot kill Flutter. */
public final class UnityGameActivity extends UnityPlayerActivity {
    private static WeakReference<UnityGameActivity> active = new WeakReference<>(null);
    private boolean awaitingSave;
    private Object backCallback;

    @Override protected void onCreate(Bundle state) {
        super.onCreate(state);
        active = new WeakReference<>(this);
        if (Build.VERSION.SDK_INT >= 33) {
            backCallback = BackApi33.register(this);
        }
    }

    @Override public void onBackPressed() { requestSaveAndReturn(); }

    private void requestSaveAndReturn() {
        if (awaitingSave) return;
        awaitingSave = true;
        UnityPlayer.UnitySendMessage("PixelWorldAndroidBridge", "SaveAndReturn", "");
    }

    /** Unity calls only after SaveSystem has committed successfully. */
    public static void finishAfterSave() {
        UnityGameActivity activity = active.get();
        if (activity != null) activity.runOnUiThread(() -> {
            activity.finish();
        });
    }

    /** Allow retry if a save failed; Unity displays the actual failure. */
    public static void saveFailed() {
        UnityGameActivity activity = active.get();
        if (activity != null) activity.runOnUiThread(() -> activity.awaitingSave = false);
    }

    @Override protected void onDestroy() {
        if (Build.VERSION.SDK_INT >= 33) {
            BackApi33.unregister(this, backCallback);
        }
        active.clear();
        super.onDestroy();
    }

    // Keep API-33 types out of the outer activity's instance initialization.
    // The pinned Unity minimum includes Android 8, where these classes do not exist.
    private static final class BackApi33 {
        static Object register(UnityGameActivity activity) {
            OnBackInvokedCallback callback = activity::requestSaveAndReturn;
            activity.getOnBackInvokedDispatcher().registerOnBackInvokedCallback(
                OnBackInvokedDispatcher.PRIORITY_DEFAULT, callback);
            return callback;
        }
        static void unregister(UnityGameActivity activity, Object callback) {
            if (callback != null) activity.getOnBackInvokedDispatcher()
                .unregisterOnBackInvokedCallback((OnBackInvokedCallback) callback);
        }
    }
}
