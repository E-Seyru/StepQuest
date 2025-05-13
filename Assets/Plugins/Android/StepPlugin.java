// Filepath: Assets/Plugins/Android/com/StepQuest/steps/StepPlugin.java
package com.StepQuest.steps; // VOTRE package

import android.app.Activity;
import android.content.Context;
import android.content.pm.PackageManager;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.util.Log;

import androidx.core.app.ActivityCompat;
import androidx.core.content.ContextCompat;

import com.google.android.gms.fitness.FitnessLocal;
import com.google.android.gms.fitness.LocalRecordingClient;
import com.google.android.gms.fitness.data.LocalBucket;
import com.google.android.gms.fitness.data.LocalDataPoint;
import com.google.android.gms.fitness.data.LocalDataSet;
import com.google.android.gms.fitness.data.LocalDataType;
import com.google.android.gms.fitness.data.LocalField;
import com.google.android.gms.fitness.request.LocalDataReadRequest;
import com.google.android.gms.fitness.result.LocalDataReadResponse;
import com.unity3d.player.UnityPlayer;

import java.time.LocalDateTime;
import java.time.LocalTime;
import java.time.ZoneId;
import java.util.concurrent.TimeUnit;

public class StepPlugin implements SensorEventListener {

    private static final String TAG = "StepPlugin";
    private static final String PERMISSION_ACTIVITY_RECOGNITION = "android.permission.ACTIVITY_RECOGNITION";
    private static final int REQUEST_CODE_ACTIVITY_RECOGNITION = 1001;

    // API Recording
    private static long lastReadStepsForCustomRange = -1;
    private static long lastReadStepsForToday = -1;
    private static boolean isReadingAPIData = false;
    private static final Object apiReadLock = new Object();

    // Capteur Direct (Sensor.TYPE_STEP_COUNTER)
    private static SensorManager sensorManager;
    private static Sensor stepCounterSensor;
    private static StepPlugin instance;
    private static long currentDeviceRawSteps = -1;
    private static boolean directSensorListenerActive = false;

    public static StepPlugin getInstance() {
        if (instance == null) {
            instance = new StepPlugin();
        }
        return instance;
    }

    private StepPlugin() { }

    public static boolean hasActivityRecognitionPermission() {
        Activity currentActivity = UnityPlayer.currentActivity;
        if (currentActivity == null) { Log.e(TAG, "UnityPlayer.currentActivity is null for permission check."); return false; }
        return ContextCompat.checkSelfPermission(currentActivity, PERMISSION_ACTIVITY_RECOGNITION) == PackageManager.PERMISSION_GRANTED;
    }

    public static void requestActivityRecognitionPermission() {
        Activity currentActivity = UnityPlayer.currentActivity;
        if (currentActivity == null) { Log.e(TAG, "UnityPlayer.currentActivity is null for permission request."); return; }
        ActivityCompat.requestPermissions(currentActivity, new String[]{PERMISSION_ACTIVITY_RECOGNITION}, REQUEST_CODE_ACTIVITY_RECOGNITION);
    }

    public static void subscribeToRecordingAPI() {
        Log.i(TAG, "[API Recording] subscribeToRecordingAPI called.");
        Activity currentActivity = UnityPlayer.currentActivity;
        if (currentActivity == null) { Log.e(TAG, "[API Recording] CurrentActivity null."); return; }
        if (!hasActivityRecognitionPermission()) { Log.w(TAG, "[API Recording] No permission for subscribe."); return; }
        FitnessLocal.getLocalRecordingClient(currentActivity).subscribe(LocalDataType.TYPE_STEP_COUNT_DELTA)
            .addOnSuccessListener(aVoid -> Log.i(TAG, "[API Recording] Subscribed successfully!"))
            .addOnFailureListener(e -> Log.w(TAG, "[API Recording] Subscription failed.", e));
    }

    // Pour GetDeltaSince(fromEpochMs)
    public static void readStepsForTimeRange(long startTimeEpochMs, long endTimeEpochMs) {
        Log.i(TAG, "[API Recording] readStepsForTimeRange called. StartMs: " + startTimeEpochMs + ", EndMs: " + endTimeEpochMs);
        lastReadStepsForCustomRange = -1; // Réinitialiser avant la lecture

        long startTimeSeconds = startTimeEpochMs / 1000;
        long endTimeSeconds = endTimeEpochMs / 1000;

        // Cas spécial pour la première fois (lastSync = 0), on lit les pas du jour.
        // C# saura interpréter que si fromEpochMs était 0, ce delta est "depuis le début connu"
        // qui, pour la première fois, sera équivalent aux pas du jour.
        if (startTimeEpochMs == 0 && endTimeEpochMs > 0) { // Typiquement endTimeEpochMs sera "maintenant"
             Log.w(TAG, "[API Recording] GetDeltaSince(0) detected. Reading today's steps for this custom range request.");
             // Lire les pas du jour et stocker le résultat dans lastReadStepsForCustomRange
             readStepDataInternal(true, true); // true = use today's range, true = store in customRange variable
             return;
        }
        
        if (startTimeSeconds >= endTimeSeconds) {
            Log.w(TAG, "[API Recording] Invalid time range for readStepsForTimeRange. StartS: " + startTimeSeconds + " >= EndS: " + endTimeSeconds + ". Setting steps to 0.");
            lastReadStepsForCustomRange = 0;
            return;
        }
        // Lire pour la plage personnalisée et stocker le résultat dans lastReadStepsForCustomRange
        readStepDataInternal(startTimeSeconds, endTimeSeconds, true); // true = store in customRange variable
    }
    
    public static long getStoredStepsForCustomRange() {
        Log.d(TAG, "[API Recording] C# requested getStoredStepsForCustomRange. Returning: " + lastReadStepsForCustomRange);
        return lastReadStepsForCustomRange;
    }

    // Pour GetDeltaToday()
    public static void readTodaysStepData() {
        Log.i(TAG, "[API Recording] readTodaysStepData (public) called.");
        lastReadStepsForToday = -1; // Réinitialiser
        // Lire les pas du jour et stocker le résultat dans lastReadStepsForToday
        readStepDataInternal(true, false); // true = use today's range, false = store in today's variable (not custom)
    }

    public static long getStoredStepsForToday() {
        Log.d(TAG, "[API Recording] C# requested getStoredStepsForToday. Returning: " + lastReadStepsForToday);
        return lastReadStepsForToday;
    }

    // Méthode interne principale pour lire les données de pas API
    // Si useTodaysDateRange est true, startTimeEpochSec et endTimeEpochSec sont ignorés.
    // Si storeInCustomRangeVariable est true, le résultat va dans lastReadStepsForCustomRange, sinon dans lastReadStepsForToday.
    private static void readStepDataInternal(boolean useTodaysDateRange, boolean storeInCustomRangeVariable) {
        long startEpochSec, endEpochSec;
        if (useTodaysDateRange) {
            LocalDateTime midnight = LocalDateTime.now(ZoneId.systemDefault()).with(LocalTime.MIN);
            LocalDateTime now = LocalDateTime.now(ZoneId.systemDefault());
            startEpochSec = midnight.atZone(ZoneId.systemDefault()).toEpochSecond();
            endEpochSec = now.atZone(ZoneId.systemDefault()).toEpochSecond();
            Log.i(TAG, "[API Internal] Reading for Today's Range. Store in: " + (storeInCustomRangeVariable ? "CustomVar" : "TodayVar"));
        } else {
            // Ce cas ne devrait plus être appelé directement avec cette refonte, on passe les temps.
            // On garde la signature au cas où, mais il faudrait fournir les temps.
            // Pour éviter une erreur, on utilise la plage d'aujourd'hui si ce chemin est pris par erreur.
             Log.e(TAG, "[API Internal] readStepDataInternal(boolean, boolean) called without specific time range, defaulting to today. This path should be reviewed.");
            LocalDateTime midnight = LocalDateTime.now(ZoneId.systemDefault()).with(LocalTime.MIN);
            LocalDateTime now = LocalDateTime.now(ZoneId.systemDefault());
            startEpochSec = midnight.atZone(ZoneId.systemDefault()).toEpochSecond();
            endEpochSec = now.atZone(ZoneId.systemDefault()).toEpochSecond();
        }
        readStepDataInternal(startEpochSec, endEpochSec, storeInCustomRangeVariable);
    }
    
    private static void readStepDataInternal(long startTimeEpochSec, long endTimeEpochSec, boolean storeInCustomRangeVariable) {
        synchronized (apiReadLock) {
            if (isReadingAPIData) {
                Log.d(TAG, "[API Internal] Already reading API data, skipping.");
                return;
            }
            isReadingAPIData = true;
        }

        final Activity currentActivity = UnityPlayer.currentActivity;
        if (currentActivity == null) { Log.e(TAG, "[API Internal] CurrentActivity null."); resetIsReadingFlag(); return; }
        if (!hasActivityRecognitionPermission()) { Log.w(TAG, "[API Internal] No permission."); resetIsReadingFlag(); return; }

        Log.i(TAG, "[API Internal] Reading API. Store in: " + (storeInCustomRangeVariable ? "CustomVar" : "TodayVar") + ". StartS: " + startTimeEpochSec + ", EndS: " + endTimeEpochSec);

        if (startTimeEpochSec >= endTimeEpochSec) {
            Log.w(TAG, "[API Internal] Invalid time range. StartS: " + startTimeEpochSec + " >= EndS: " + endTimeEpochSec + ". Setting to 0.");
            if (storeInCustomRangeVariable) lastReadStepsForCustomRange = 0;
            else lastReadStepsForToday = 0;
            resetIsReadingFlag();
            return;
        }

        LocalDataReadRequest readRequest = new LocalDataReadRequest.Builder()
                .aggregate(LocalDataType.TYPE_STEP_COUNT_DELTA)
                .bucketByTime((int) (endTimeEpochSec - startTimeEpochSec), TimeUnit.SECONDS)
                .setTimeRange(startTimeEpochSec, endTimeEpochSec, TimeUnit.SECONDS)
                .build();
        
        FitnessLocal.getLocalRecordingClient(currentActivity).readData(readRequest)
            .addOnSuccessListener(dataReadResponse -> {
                long totalSteps = 0;
                for (LocalBucket bucket : dataReadResponse.getBuckets()) {
                    for (LocalDataSet dataSet : bucket.getDataSets()) {
                        totalSteps += processSingleDataSet(dataSet); // Renommé pour clarté
                    }
                }
                 if (dataReadResponse.getBuckets().isEmpty() && !dataReadResponse.getDataSets().isEmpty()){
                     for (LocalDataSet dataSet : dataReadResponse.getDataSets()) {
                        totalSteps += processSingleDataSet(dataSet); // Renommé pour clarté
                    }
                }
                Log.i(TAG, "[API Internal] Read success. Steps: " + totalSteps + ". Store in: " + (storeInCustomRangeVariable ? "CustomVar" : "TodayVar"));
                if (storeInCustomRangeVariable) {
                    lastReadStepsForCustomRange = totalSteps;
                } else {
                    lastReadStepsForToday = totalSteps;
                }
                resetIsReadingFlag();
            })
            .addOnFailureListener(e -> {
                Log.w(TAG, "[API Internal] Read failed.", e);
                if (storeInCustomRangeVariable) lastReadStepsForCustomRange = -1;
                else lastReadStepsForToday = -1;
                resetIsReadingFlag();
            });
    }
    
    private static void resetIsReadingFlag() {
        synchronized (apiReadLock) {
            isReadingAPIData = false;
        }
    }

    private static long processSingleDataSet(LocalDataSet dataSet) {
        long steps = 0;
        if (dataSet.isEmpty()) return 0;
        for (LocalDataPoint dp : dataSet.getDataPoints()) {
            for (LocalField field : dp.getDataType().getFields()) {
                if (field.equals(LocalField.FIELD_STEPS)) {
                    steps += dp.getValue(field).asInt();
                }
            }
        }
        return steps;
    }

    // --- CAPTEUR DIRECT ---
    public static void startDirectStepCounterListener() {
        Log.i(TAG, "[Direct Sensor] startDirectStepCounterListener called.");
        Activity currentActivity = UnityPlayer.currentActivity;
        if (currentActivity == null) { Log.e(TAG, "[Direct Sensor] CurrentActivity null."); return; }
        if (!hasActivityRecognitionPermission()) { Log.w(TAG, "[Direct Sensor] No permission."); return; }

        if (sensorManager == null) {
            sensorManager = (SensorManager) currentActivity.getSystemService(Context.SENSOR_SERVICE);
        }
        if (stepCounterSensor == null) {
            stepCounterSensor = sensorManager.getDefaultSensor(Sensor.TYPE_STEP_COUNTER);
        }

        if (stepCounterSensor != null) {
            if (!directSensorListenerActive) {
                if (sensorManager.registerListener(getInstance(), stepCounterSensor, SensorManager.SENSOR_DELAY_UI)) {
                    directSensorListenerActive = true;
                    Log.i(TAG, "[Direct Sensor] Listener registered.");
                } else { Log.e(TAG, "[Direct Sensor] Listener registration FAILED."); }
            } else { Log.i(TAG, "[Direct Sensor] Listener already active."); }
        } else {
            Log.w(TAG, "[Direct Sensor] TYPE_STEP_COUNTER sensor not available.");
            currentDeviceRawSteps = -2;
        }
    }

    public static void stopDirectStepCounterListener() {
        Log.i(TAG, "[Direct Sensor] stopDirectStepCounterListener called.");
        if (sensorManager != null && stepCounterSensor != null && directSensorListenerActive) {
            sensorManager.unregisterListener(getInstance(), stepCounterSensor);
            directSensorListenerActive = false;
            Log.i(TAG, "[Direct Sensor] Listener unregistered.");
        } else { Log.i(TAG, "[Direct Sensor] Listener not active or SnsrMgr/Sensor null."); }
    }

    public static long getCurrentRawSensorSteps() {
        if (stepCounterSensor == null) return -2;
        if (!directSensorListenerActive && currentDeviceRawSteps == -1) { // Si jamais lu et listener pas actif
             Log.w(TAG, "[Direct Sensor] getCurrentRawSensorSteps: Listener not active & no prior value. Try starting listener first.");
             return -3; // Indique que le listener n'est pas actif et qu'on n'a pas de valeur fraîche
        }
        return currentDeviceRawSteps;
    }

    @Override
    public void onSensorChanged(SensorEvent event) {
        if (event.sensor.getType() == Sensor.TYPE_STEP_COUNTER) {
            currentDeviceRawSteps = (long) event.values[0];
        }
    }

    @Override
    public void onAccuracyChanged(Sensor sensor, int accuracy) { /* Peut être ignoré */ }
}