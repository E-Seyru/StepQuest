package com.StepQuest.steps;

import android.app.Activity;
import android.content.pm.PackageManager;
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
import com.google.android.gms.tasks.OnFailureListener;
import com.google.android.gms.tasks.OnSuccessListener;
import com.unity3d.player.UnityPlayer;

import java.time.LocalDateTime;
import java.time.LocalTime;
import java.time.ZoneId;
import java.util.concurrent.TimeUnit;

public class StepPlugin {

    private static final String TAG = "StepPlugin";
    private static final String PERMISSION_ACTIVITY_RECOGNITION = "android.permission.ACTIVITY_RECOGNITION";
    private static final int REQUEST_CODE_ACTIVITY_RECOGNITION = 1001;

    // Variable pour stocker le dernier nombre de pas lu
    private static long lastReadTotalStepsToday = 0;
    private static boolean isReadingData = false; // Pour éviter les lectures concurrentes si besoin

    public static String getGreeting(String name) {
        Log.i(TAG, "getGreeting called with: " + name);
        return "Hello " + name + " from Android Plugin!";
    }

    public static boolean hasActivityRecognitionPermission() {
        Activity currentActivity = UnityPlayer.currentActivity;
        if (currentActivity == null) {
            Log.e(TAG, "UnityPlayer.currentActivity is null for permission check.");
            return false;
        }
        int permissionStatus = ContextCompat.checkSelfPermission(currentActivity, PERMISSION_ACTIVITY_RECOGNITION);
        boolean hasPermission = permissionStatus == PackageManager.PERMISSION_GRANTED;
        Log.i(TAG, "Activity Recognition Permission status: " + (hasPermission ? "GRANTED" : "DENIED"));
        return hasPermission;
    }

    public static void requestActivityRecognitionPermission() {
        Activity currentActivity = UnityPlayer.currentActivity;
        if (currentActivity == null) {
            Log.e(TAG, "UnityPlayer.currentActivity is null for permission request.");
            return;
        }
        Log.i(TAG, "Requesting Activity Recognition Permission...");
        ActivityCompat.requestPermissions(currentActivity,
                new String[]{PERMISSION_ACTIVITY_RECOGNITION},
                REQUEST_CODE_ACTIVITY_RECOGNITION);
    }

    public static void subscribeToSteps() {
        Activity currentActivity = UnityPlayer.currentActivity;
        if (currentActivity == null) {
            Log.e(TAG, "UnityPlayer.currentActivity is null. Cannot subscribe to steps.");
            return;
        }
        if (!hasActivityRecognitionPermission()) {
            Log.w(TAG, "Activity recognition permission not granted for subscribe. Requesting it now.");
            requestActivityRecognitionPermission();
            return;
        }
        Log.i(TAG, "Permission GRANTED for subscribe. Attempting to subscribe...");
        LocalRecordingClient localRecordingClient = FitnessLocal.getLocalRecordingClient(currentActivity);
        localRecordingClient.subscribe(LocalDataType.TYPE_STEP_COUNT_DELTA)
                .addOnSuccessListener(new OnSuccessListener<Void>() {
                    @Override
                    public void onSuccess(Void aVoid) {
                        Log.i(TAG, "Successfully subscribed to step count delta!");
                    }
                })
                .addOnFailureListener(new OnFailureListener() {
                    @Override
                    public void onFailure(Exception e) {
                        Log.w(TAG, "There was a problem subscribing to step count delta.", e);
                    }
                });
    }

    // Nouvelle méthode pour que C# récupère les pas stockés
    public static long getStoredTodaysSteps() {
        Log.d(TAG, "C# requested stored steps. Returning: " + lastReadTotalStepsToday);
        return lastReadTotalStepsToday;
    }

    public static void readTodaysStepData() {
        final Activity currentActivity = UnityPlayer.currentActivity;
        if (currentActivity == null) {
            Log.e(TAG, "UnityPlayer.currentActivity is null. Cannot read steps.");
            return;
        }
        if (!hasActivityRecognitionPermission()) {
            Log.w(TAG, "Activity recognition permission not granted for read. Please request permission first.");
            return;
        }
        if (isReadingData) {
            Log.d(TAG, "Already reading data, skipping this request.");
            return;
        }
        isReadingData = true;
        Log.i(TAG, "Attempting to read today's step data...");
        LocalRecordingClient localRecordingClient = FitnessLocal.getLocalRecordingClient(currentActivity);

        LocalDateTime midnight = LocalDateTime.now(ZoneId.systemDefault()).with(LocalTime.MIN);
        LocalDateTime now = LocalDateTime.now(ZoneId.systemDefault());
        long startTimeSeconds = midnight.atZone(ZoneId.systemDefault()).toEpochSecond();
        long endTimeSeconds = now.atZone(ZoneId.systemDefault()).toEpochSecond();

        if (startTimeSeconds >= endTimeSeconds) {
             Log.w(TAG, "Start time is not before end time. Setting start to midnight, end to now ensuring end > start.");
             startTimeSeconds = midnight.atZone(ZoneId.systemDefault()).toEpochSecond();
             endTimeSeconds = now.atZone(ZoneId.systemDefault()).toEpochSecond();
             if (startTimeSeconds >= endTimeSeconds) { // If it's exactly midnight
                if (endTimeSeconds > 0) startTimeSeconds = endTimeSeconds -1; // Read last second if possible
                else { // Unlikely, but covers edge case of endTime being 0
                    Log.e(TAG, "Cannot set a valid time range for reading step data.");
                    isReadingData = false;
                    return;
                }
             }
        }

        LocalDataReadRequest readRequest = new LocalDataReadRequest.Builder()
                .aggregate(LocalDataType.TYPE_STEP_COUNT_DELTA)
                .bucketByTime((int) (endTimeSeconds - startTimeSeconds), TimeUnit.SECONDS)
                .setTimeRange(startTimeSeconds, endTimeSeconds, TimeUnit.SECONDS)
                .build();

        localRecordingClient.readData(readRequest)
                .addOnSuccessListener(new OnSuccessListener<LocalDataReadResponse>() {
                    @Override
                    public void onSuccess(LocalDataReadResponse dataReadResponse) {
                        long totalStepsInThisRead = 0;
                        Log.d(TAG, "Read data onSuccess. Number of buckets: " + dataReadResponse.getBuckets().size());
                        for (LocalBucket bucket : dataReadResponse.getBuckets()) {
                            Log.d(TAG, "Processing bucket...");
                            for (LocalDataSet dataSet : bucket.getDataSets()) {
                                totalStepsInThisRead += processDataSet(dataSet);
                            }
                        }
                        if (dataReadResponse.getBuckets().isEmpty() && !dataReadResponse.getDataSets().isEmpty()){
                            Log.d(TAG, "No buckets, but direct datasets found. Processing them.");
                             for (LocalDataSet dataSet : dataReadResponse.getDataSets()) {
                                totalStepsInThisRead += processDataSet(dataSet);
                            }
                        }
                        Log.i(TAG, "Successfully read data. Total steps for this read: " + totalStepsInThisRead);
                        lastReadTotalStepsToday = totalStepsInThisRead; // Mise à jour de la variable stockée
                        isReadingData = false;
                    }
                })
                .addOnFailureListener(new OnFailureListener() {
                    @Override
                    public void onFailure(Exception e) {
                        Log.w(TAG, "There was an error reading step data.", e);
                        isReadingData = false;
                    }
                });
    }

    private static long processDataSet(LocalDataSet dataSet) {
        long stepsInDataSet = 0;
        //Log.d(TAG, "Processing DataSet for type: " + dataSet.getDataType().getName());
        if (dataSet.isEmpty()) {
            //Log.d(TAG, "\tData set is empty.");
            return 0;
        }
        for (LocalDataPoint dp : dataSet.getDataPoints()) {
            //Log.d(TAG, "\tData point:");
            //Log.d(TAG, "\t\tType: " + dp.getDataType().getName());
            //LocalDateTime startTime = LocalDateTime.ofEpochSecond(dp.getStartTime(TimeUnit.SECONDS), 0, ZoneId.systemDefault().getRules().getOffset(LocalDateTime.now()));
            //LocalDateTime endTime = LocalDateTime.ofEpochSecond(dp.getEndTime(TimeUnit.SECONDS), 0, ZoneId.systemDefault().getRules().getOffset(LocalDateTime.now()));
            //Log.d(TAG, "\t\tStart: " + dp.getStartTime(TimeUnit.SECONDS) + " (" + startTime + ")");
            //Log.d(TAG, "\t\tEnd: " + dp.getEndTime(TimeUnit.SECONDS) + " (" + endTime + ")");
            for (LocalField field : dp.getDataType().getFields()) {
                if (field.equals(LocalField.FIELD_STEPS)) {
                    long stepValue = dp.getValue(field).asInt();
                    stepsInDataSet += stepValue;
                    //Log.d(TAG, "\t\tField: " + field.getName() + " Value: " + stepValue);
                } else {
                    //Log.d(TAG, "\t\tField: " + field.getName() + " Value (raw): " + dp.getValue(field));
                }
            }
        }
        return stepsInDataSet;
    }
}