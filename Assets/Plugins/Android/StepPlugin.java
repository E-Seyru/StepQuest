// Filepath: Assets/Plugins/Android/com/StepQuest/steps/StepPlugin.java
package com.StepQuest.steps;

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
import com.unity3d.player.UnityPlayer;

import java.util.concurrent.TimeUnit;

public class StepPlugin implements SensorEventListener {

    private static final String TAG = "StepPlugin";
    private static final String PERMISSION_ACTIVITY_RECOGNITION = "android.permission.ACTIVITY_RECOGNITION";
    private static final int REQUEST_CODE_ACTIVITY_RECOGNITION = 1001;

    // API Recording
    private static long lastReadStepsForCustomRange = -1;
    private static boolean isReadingAPIData = false;
    private static final Object apiReadLock = new Object();
    private static final long MAX_REASONABLE_STEPS = 100000; // Limite maximale raisonnable pour détection d'anomalies

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
        if (currentActivity == null) { 
            Log.e(TAG, "UnityPlayer.currentActivity is null for permission check."); 
            return false; 
        }
        
        boolean hasPermission = ContextCompat.checkSelfPermission(currentActivity, PERMISSION_ACTIVITY_RECOGNITION) == PackageManager.PERMISSION_GRANTED;
        Log.i(TAG, "hasActivityRecognitionPermission check result: " + hasPermission);
        return hasPermission;
    }

    public static void requestActivityRecognitionPermission() {
        Activity currentActivity = UnityPlayer.currentActivity;
        if (currentActivity == null) { 
            Log.e(TAG, "UnityPlayer.currentActivity is null for permission request."); 
            return; 
        }
        
        Log.i(TAG, "Requesting ACTIVITY_RECOGNITION permission");
        ActivityCompat.requestPermissions(currentActivity, new String[]{PERMISSION_ACTIVITY_RECOGNITION}, REQUEST_CODE_ACTIVITY_RECOGNITION);
        Log.i(TAG, "Permission request sent to Android system");
    }

    public static void subscribeToRecordingAPI() {
        Log.i(TAG, "[API Recording] subscribeToRecordingAPI called.");
        Activity currentActivity = UnityPlayer.currentActivity;
        if (currentActivity == null) { 
            Log.e(TAG, "[API Recording] CurrentActivity null."); 
            return; 
        }
        if (!hasActivityRecognitionPermission()) { 
            Log.w(TAG, "[API Recording] No permission for subscribe."); 
            return; 
        }
        FitnessLocal.getLocalRecordingClient(currentActivity).subscribe(LocalDataType.TYPE_STEP_COUNT_DELTA)
            .addOnSuccessListener(aVoid -> Log.i(TAG, "[API Recording] Subscribed successfully!"))
            .addOnFailureListener(e -> Log.w(TAG, "[API Recording] Subscription failed.", e));
    }

    // Pour GetDeltaSince(fromEpochMs)
    public static void readStepsForTimeRange(long startTimeEpochMs, long endTimeEpochMs) {
        Log.i(TAG, "[API Recording] readStepsForTimeRange called. StartMs: " + startTimeEpochMs + ", EndMs: " + endTimeEpochMs);
        
        // Réinitialiser la valeur pour indiquer que le processus a commencé
        synchronized (apiReadLock) {
            lastReadStepsForCustomRange = -1;
        }

        // Cas spécial si startTimeEpochMs est 0 (premier lancement)
        if (startTimeEpochMs <= 1) {
            Log.w(TAG, "[API Recording] startTimeEpochMs is 0/1, reading all available step history");
            // Utiliser une date ancienne comme point de départ pour tout récupérer
            startTimeEpochMs = 1; // Ou une date suffisamment ancienne, 1 millisecondes après epoch
        }

        long startTimeSeconds = startTimeEpochMs / 1000;
        long endTimeSeconds = endTimeEpochMs / 1000;
        
        if (startTimeSeconds >= endTimeSeconds) {
            Log.w(TAG, "[API Recording] Invalid time range for readStepsForTimeRange. StartS: " + startTimeSeconds + " >= EndS: " + endTimeSeconds + ". Setting steps to 0.");
            synchronized (apiReadLock) {
                lastReadStepsForCustomRange = 0;
            }
            return;
        }
        
        // Limiter l'intervalle de temps pour éviter des lectures trop longues qui pourraient causer des problèmes
        if (endTimeSeconds - startTimeSeconds > 2592000) { // 30 jours en secondes
            Log.w(TAG, "[API Recording] Time range too large (> 30 days). Limiting to 30 days.");
            startTimeSeconds = endTimeSeconds - 2592000;
        }
        
        readStepDataInternal(startTimeSeconds, endTimeSeconds);
    }
    
    public static long getStoredStepsForCustomRange() {
        synchronized (apiReadLock) {
            Log.d(TAG, "[API Recording] C# requested getStoredStepsForCustomRange. Returning: " + lastReadStepsForCustomRange);
            return lastReadStepsForCustomRange;
        }
    }

    // Méthode interne principale pour lire les données de pas API
    private static void readStepDataInternal(long startTimeEpochSec, long endTimeEpochSec) {
        synchronized (apiReadLock) {
            if (isReadingAPIData) {
                Log.d(TAG, "[API Internal] Already reading API data, skipping.");
                return;
            }
            isReadingAPIData = true;
        }

        final Activity currentActivity = UnityPlayer.currentActivity;
        if (currentActivity == null) { 
            Log.e(TAG, "[API Internal] CurrentActivity null."); 
            resetIsReadingFlag(); 
            return; 
        }
        if (!hasActivityRecognitionPermission()) { 
            Log.w(TAG, "[API Internal] No permission."); 
            resetIsReadingFlag(); 
            return; 
        }

        Log.i(TAG, "[API Internal] Reading API. StartS: " + startTimeEpochSec + ", EndS: " + endTimeEpochSec);

        if (startTimeEpochSec >= endTimeEpochSec) {
            Log.w(TAG, "[API Internal] Invalid time range. StartS: " + startTimeEpochSec + " >= EndS: " + endTimeEpochSec + ". Setting to 0.");
            synchronized (apiReadLock) {
                lastReadStepsForCustomRange = 0;
            }
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
                        totalSteps += processSingleDataSet(dataSet);
                    }
                }
                if (dataReadResponse.getBuckets().isEmpty() && !dataReadResponse.getDataSets().isEmpty()){
                    for (LocalDataSet dataSet : dataReadResponse.getDataSets()) {
                        totalSteps += processSingleDataSet(dataSet);
                    }
                }
                
                // Vérifier si la valeur est suspicieusement élevée
                if (totalSteps > MAX_REASONABLE_STEPS) {
                    Log.w(TAG, "[API Internal] Unusually high step count detected: " + totalSteps + 
                           ". Period: " + (endTimeEpochSec - startTimeEpochSec) + " seconds. Capping to " + MAX_REASONABLE_STEPS);
                    totalSteps = MAX_REASONABLE_STEPS;
                }
                
                Log.i(TAG, "[API Internal] Read success. Steps: " + totalSteps);
                synchronized (apiReadLock) {
                    lastReadStepsForCustomRange = totalSteps;
                }
                resetIsReadingFlag();
            })
            .addOnFailureListener(e -> {
                Log.w(TAG, "[API Internal] Read failed.", e);
                synchronized (apiReadLock) {
                    lastReadStepsForCustomRange = -1;
                }
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
        if (currentActivity == null) { 
            Log.e(TAG, "[Direct Sensor] CurrentActivity null."); 
            return; 
        }
        if (!hasActivityRecognitionPermission()) { 
            Log.w(TAG, "[Direct Sensor] No permission."); 
            return; 
        }

        if (sensorManager == null) {
            sensorManager = (SensorManager) currentActivity.getSystemService(Context.SENSOR_SERVICE);
        }
        if (stepCounterSensor == null) {
            stepCounterSensor = sensorManager.getDefaultSensor(Sensor.TYPE_STEP_COUNTER);
        }

        if (stepCounterSensor != null) {
            if (!directSensorListenerActive) {
                // Essayer d'enregistrer l'écouteur avec une fréquence plus basse pour économiser la batterie
                if (sensorManager.registerListener(getInstance(), stepCounterSensor, SensorManager.SENSOR_DELAY_NORMAL)) {
                    directSensorListenerActive = true;
                    Log.i(TAG, "[Direct Sensor] Listener registered with NORMAL delay.");
                } else { 
                    // Tentative avec une fréquence plus élevée en cas d'échec
                    if (sensorManager.registerListener(getInstance(), stepCounterSensor, SensorManager.SENSOR_DELAY_UI)) {
                        directSensorListenerActive = true;
                        Log.i(TAG, "[Direct Sensor] Listener registered with UI delay.");
                    } else {
                        Log.e(TAG, "[Direct Sensor] Listener registration FAILED with all delays."); 
                    }
                }
            } else { 
                Log.i(TAG, "[Direct Sensor] Listener already active."); 
            }
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
        } else { 
            Log.i(TAG, "[Direct Sensor] Listener not active or SnsrMgr/Sensor null."); 
        }
    }

    public static long getCurrentRawSensorSteps() {
        if (stepCounterSensor == null) return -2;
        if (!directSensorListenerActive && currentDeviceRawSteps == -1) {
             Log.w(TAG, "[Direct Sensor] getCurrentRawSensorSteps: Listener not active & no prior value.");
             return -3;
        }
        return currentDeviceRawSteps;
    }

    @Override
    public void onSensorChanged(SensorEvent event) {
        if (event.sensor.getType() == Sensor.TYPE_STEP_COUNTER) {
            // Enregistrer uniquement des changements significatifs pour éviter les micro-fluctuations
            // qui peuvent causer des ajouts fantômes
            if (currentDeviceRawSteps == -1 || Math.abs(event.values[0] - currentDeviceRawSteps) >= 1.0f) {
                currentDeviceRawSteps = (long) event.values[0];
            }
        }
    }

    @Override
    public void onAccuracyChanged(Sensor sensor, int accuracy) { 
        // Log accuracy changes to help debug potential issues
        if (sensor.getType() == Sensor.TYPE_STEP_COUNTER) {
            Log.i(TAG, "[Direct Sensor] Accuracy changed to: " + accuracy);
        }
    }
}