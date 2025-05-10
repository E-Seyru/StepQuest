package com.StepQuest.steps; // This package name is important!

import android.util.Log;

public class StepPlugin {
    public static String getGreeting(String name) {
        Log.i("StepPlugin", "getGreeting called with: " + name);
        return "Hello " + name + " from Android Plugin!";
    }
}