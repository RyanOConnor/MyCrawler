package com.android.ryan.cloudcrawlerclient;

import android.util.Log;

import org.json.JSONException;
import org.json.JSONObject;

/**
 * Created by Ryan on 4/15/2015.
 */
public class JSONParser {

    public JSONParser(){
        super();
    }

    public boolean parseUserAuth(JSONObject obj) {
        boolean userAuth = false;
        try{
            userAuth = obj.getBoolean("Value");
        } catch(JSONException ex) {
            Log.d("JSONParse=parseUserAuth", ex.getMessage());
            ex.printStackTrace();
        }
        return userAuth;
    }

    public RestAPI.ServerResponse parseServerResponse(JSONObject obj){
        RestAPI.ServerResponse response = null;
        try{
            RestAPI.ServerResponse[] responses = RestAPI.ServerResponse.values();
            for(RestAPI.ServerResponse resp : responses){
                if(resp.ordinal() == obj.get("Value")){
                    response = resp;
                    break;
                }
            }
        } catch(JSONException ex) {
            Log.d("JSONParse=parseServer", ex.getMessage());
            ex.printStackTrace();
        }
        return response;
    }
}
