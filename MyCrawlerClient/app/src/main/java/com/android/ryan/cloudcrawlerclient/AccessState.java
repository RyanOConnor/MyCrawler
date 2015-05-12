package com.android.ryan.cloudcrawlerclient;

import android.content.Context;
import android.content.SharedPreferences;
import android.preference.PreferenceManager;
import android.util.Log;

/**
 * Created by Ryan on 4/17/2015.
 */
public class AccessState {

    private static AccessState _instance;

    public static AccessState instance (){
        if(_instance == null)
            _instance = new AccessState();
        return _instance;
    }

    private AccessState(){
    }

    SharedPreferences prefs;
    SharedPreferences.Editor editor;

    public boolean userIsLoggedIn(Context context){
        prefs = context.getSharedPreferences("MyPref", 0);
        return prefs.getBoolean("logged_in", false);
    }

    public void setUserLoggedIn(Context context, String username, String userid) {
        prefs = context.getSharedPreferences("MyPref", 0);
        editor = prefs.edit();
        editor.putString("username", username);
        editor.putString("userid", userid);
        editor.putBoolean("logged_in", true);
        editor.commit();
    }

    public void setUserLoggedOut(Context context){
        prefs = context.getSharedPreferences("MyPref", 0);
        editor = prefs.edit();
        editor.putString("username", null);
        editor.putString("userid", null);
        editor.putBoolean("logged_in", false);
        editor.commit();
        SyncService.shutDownSync();
    }

    public String getUserName(Context context){
        prefs = context.getSharedPreferences("MyPref", 0);
        String username = prefs.getString("username", null);
        return username;
    }

    public String getUserID(Context context){
        prefs = context.getSharedPreferences("MyPref", 0);
        return prefs.getString("userid", null);
    }
}
