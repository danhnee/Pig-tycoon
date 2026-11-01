using System;
using System.IO;
using System.Text;
using PigTycoon.Core;
using UnityEngine;

namespace PigTycoon.Presentation.Services
{
    public enum GameSaveLoadStatus
    {
        Unavailable,
        Missing,
        Parsed,
        Loaded,
        Invalid,
        UnsupportedVersion,
        RestoreFailed
    }

    public enum GameSaveLoadFailureStage
    {
        None,
        Read,
        Parse,
        Restore
    }

    public enum GameSaveWriteStatus
    {
        Saved,
        NotReady,
        Blocked,
        Failed
    }

    public sealed class GameSaveLoadResult
    {
        public GameSaveLoadStatus Status { get; }
        public GameSaveLoadFailureStage FailureStage { get; }
        public GameSaveData Data { get; }
        public Exception Error { get; }
        public string Message { get; }
        public bool Succeeded => Status == GameSaveLoadStatus.Loaded;

        public GameSaveLoadResult(
            GameSaveLoadStatus status,
            GameSaveData data = null,
            Exception error = null,
            GameSaveLoadFailureStage failureStage = GameSaveLoadFailureStage.None,
            string message = null)
        {
            Status = status;
            Data = data;
            Error = error;
            FailureStage = failureStage;
            Message = message ?? error?.Message ?? status.ToString();
        }

        public static GameSaveLoadResult Unavailable(string message = "Save service is unavailable.") =>
            new GameSaveLoadResult(GameSaveLoadStatus.Unavailable, message: message);
    }

    public sealed class GameSaveOperationResult
    {
        public GameSaveWriteStatus Status { get; }
        public Exception Error { get; }
        public string Message { get; }
        public bool Succeeded => Status == GameSaveWriteStatus.Saved;

        public GameSaveOperationResult(GameSaveWriteStatus status, string message = null, Exception error = null)
        {
            Status = status;
            Error = error;
            Message = message ?? error?.Message ?? status.ToString();
        }
    }

    public sealed class GameSaveService
    {
        private readonly string savePath;
        private SaveFileStatus currentStatus;

        public string SavePath => savePath;
        public SaveFileStatus CurrentStatus => currentStatus;
        public GameSaveLoadResult LastLoadResult { get; private set; }

        public GameSaveService()
        {
            savePath = Path.Combine(Application.persistentDataPath, "pig_tycoon_save.json");
        }

        public GameSaveLoadResult Load()
        {
            bool fileExists = File.Exists(savePath);
            if (!fileExists)
            {
                currentStatus = SaveFileStatus.Missing;
                return LastLoadResult = new GameSaveLoadResult(GameSaveLoadStatus.Missing);
            }

            string json;
            try
            {
                json = File.ReadAllText(savePath);
            }
            catch (Exception ex)
            {
                currentStatus = SaveFileStatus.Invalid;
                return LastLoadResult = new GameSaveLoadResult(
                    GameSaveLoadStatus.Invalid,
                    error: ex,
                    failureStage: GameSaveLoadFailureStage.Read,
                    message: $"Could not read save file '{savePath}'.");
            }

            GameSaveData data;
            try
            {
                data = JsonUtility.FromJson<GameSaveData>(json);
            }
            catch (Exception ex)
            {
                currentStatus = SaveFileStatus.Invalid;
                return LastLoadResult = new GameSaveLoadResult(
                    GameSaveLoadStatus.Invalid,
                    error: ex,
                    failureStage: GameSaveLoadFailureStage.Parse,
                    message: $"Could not parse save file '{savePath}'.");
            }

            currentStatus = GameSaveCompatibility.Classify(true, data);
            if (currentStatus == SaveFileStatus.Loaded)
                return LastLoadResult = new GameSaveLoadResult(GameSaveLoadStatus.Parsed, data);

            GameSaveLoadStatus status = currentStatus == SaveFileStatus.UnsupportedVersion
                ? GameSaveLoadStatus.UnsupportedVersion
                : GameSaveLoadStatus.Invalid;
            GameSaveLoadFailureStage stage = data == null
                ? GameSaveLoadFailureStage.Parse
                : GameSaveLoadFailureStage.None;
            string message = data == null
                ? $"Save file '{savePath}' did not contain valid save data."
                : $"Save version {data.SaveVersion} is not supported.";
            return LastLoadResult = new GameSaveLoadResult(status, data, failureStage: stage, message: message);
        }

        public void MarkRestoreSucceeded(GameSaveData data)
        {
            if (currentStatus != SaveFileStatus.Loaded)
                return;

            LastLoadResult = new GameSaveLoadResult(GameSaveLoadStatus.Loaded, data);
        }

        public GameSaveLoadResult MarkRestoreFailed(Exception error)
        {
            currentStatus = SaveFileStatus.Invalid;
            return LastLoadResult = new GameSaveLoadResult(
                GameSaveLoadStatus.RestoreFailed,
                LastLoadResult?.Data,
                error,
                GameSaveLoadFailureStage.Restore,
                $"Save file '{savePath}' was parsed but could not be restored.");
        }

        public GameSaveOperationResult Save(GameEngine engine)
        {
            if (engine == null)
            {
                const string message = "GameEngine is unavailable; save skipped.";
                Debug.LogWarning($"[PigTycoon] {message}");
                return new GameSaveOperationResult(GameSaveWriteStatus.NotReady, message);
            }

            if (!GameSaveCompatibility.CanWriteSave(currentStatus))
            {
                string message = $"Save status {currentStatus} blocks overwriting the existing save.";
                Debug.LogWarning($"[PigTycoon] {message}");
                return new GameSaveOperationResult(GameSaveWriteStatus.Blocked, message);
            }

            if (engine.Clock.IsDuringCombatWave)
            {
                const string message = "Cannot save during a combat wave because encounter state is not resumable.";
                Debug.LogWarning($"[PigTycoon] {message}");
                return new GameSaveOperationResult(GameSaveWriteStatus.Blocked, message);
            }

            string tempPath = Path.Combine(
                Path.GetDirectoryName(savePath) ?? string.Empty,
                Path.GetFileName(savePath) + ".tmp." + Guid.NewGuid().ToString("N"));
            try
            {
                GameSaveData snapshot = engine.CreateSaveData();
                string json = JsonUtility.ToJson(snapshot, true);
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Flush(true);
                }

                if (File.Exists(savePath))
                    File.Replace(tempPath, savePath, null);
                else
                    File.Move(tempPath, savePath);

                currentStatus = SaveFileStatus.Loaded;
                Debug.Log($"[PigTycoon] Save written to {savePath}");
                return new GameSaveOperationResult(GameSaveWriteStatus.Saved, $"Save written to {savePath}.");
            }
            catch (Exception ex)
            {
                string message = $"Save failed for '{savePath}'. The existing save was not intentionally removed. Temporary file: '{tempPath}'.";
                Debug.LogError($"[PigTycoon] {message} {ex}");
                return new GameSaveOperationResult(GameSaveWriteStatus.Failed, message, ex);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch (Exception cleanupError)
                {
                    Debug.LogWarning($"[PigTycoon] Could not remove temporary save '{tempPath}': {cleanupError.Message}");
                }
            }
        }

        public void BlockWrites(SaveFileStatus status)
        {
            currentStatus = status;
        }
    }
}
