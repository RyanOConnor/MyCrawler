package com.android.ryan.cloudcrawlerclient;

import android.content.SharedPreferences;

import android.content.Context;
import android.preference.PreferenceManager;

/**
 * Created by Ryan on 4/17/2015.
 */
public class AccessState {

    private static AccessState _instance;

    public static AccessState instance (){
        if(_instance == null)
            _instance = new AccessState();
        return _instance;
    };

    private AccessState(){

    }

    SharedPreferences prefs;
    SharedPreferences.Editor editor;

    public boolean previousAccount(Context context){
        prefs = PreferenceManager.getDefaultSharedPreferences(context);
        //prefs = context.getSharedPreferences("MyPrefs", 0);
        return prefs.contains("username");
    }

    public boolean userIsLoggedIn(Context context){
        try {
            prefs = PreferenceManager.getDefaultSharedPreferences(context);
            //prefs = context.getSharedPreferences("MyPrefs", 0);
        } catch (Exception ex){
            ex.printStackTrace();
        }
        return prefs.getBoolean("logged_in", false);
    }

    public void setUserLoggedIn(Context context, String username) {
        prefs = context.getSharedPreferences("MyPref", 0);
        editor = prefs.edit();
        editor.putString("username", username);
        editor.putBoolean("logged_in", true);
        editor.commit();
    }

    public void setUserLoggedOut(Context context){
        prefs = context.getSharedPreferences("MyPref", 0);
        editor = prefs.edit();
        editor.putString("username", null);
        editor.putBoolean("logged_in", false);
        editor.commit();
    }

}
