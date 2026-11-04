import 'package:flutter/services.dart';

import 'supabase_api.dart';

class LocalSave {
  const LocalSave(this.json, this.modified);
  final String json;
  final DateTime modified;
}

/// Reads and writes the save file Unity keeps in the app's storage.
/// Only called while Unity is not running (before launch, after return).
class LocalSaveStore {
  const LocalSaveStore();
  static const _channel = MethodChannel('pigtycoon/save');

  Future<LocalSave?> read() async {
    final r = await _channel.invokeMapMethod<String, Object?>('readSave');
    if (r == null) return null;
    return LocalSave(r['json'] as String,
        DateTime.fromMillisecondsSinceEpoch((r['modifiedMs'] as num).toInt()));
  }

  Future<void> write(String json) =>
      _channel.invokeMethod<void>('writeSave', {'json': json});
}

enum SyncResult { nothing, downloaded, uploaded, kept }

/// Newest copy wins: the cloud copy replaces the local file before playing,
/// and the local file is uploaded after the player returns from Unity.
class SaveSync {
  SaveSync(this.api, {this.store = const LocalSaveStore()});
  final SupabaseApi api;
  final LocalSaveStore store;

  Future<SyncResult> pullBeforeLaunch() async {
    final cloud = await api.fetchSave();
    if (cloud == null) return SyncResult.nothing;
    final local = await store.read();
    if (local != null && !cloud.savedAt.isAfter(local.modified)) {
      return SyncResult.kept;
    }
    await store.write(cloud.saveJson);
    return SyncResult.downloaded;
  }

  Future<SyncResult> pushAfterReturn(String character) async {
    final local = await store.read();
    if (local == null) return SyncResult.nothing;
    await api.uploadSave(
        saveJson: local.json, character: character, savedAt: local.modified);
    return SyncResult.uploaded;
  }
}
