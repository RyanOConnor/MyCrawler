package com.android.ryan.cloudcrawlerclient;

import android.util.Log;
import android.util.Pair;

import org.apache.http.NameValuePair;
import org.apache.http.message.BasicNameValuePair;
import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Iterator;
import java.util.List;
import java.util.Map;

/**
 * Created by Ryan on 4/15/2015.
 */
public class JSONParser {

    public JSONParser(){
        super();
    }

    public String parseUserAuth(JSONObject obj) {
        String userAuth = "";
        try{
            userAuth = obj.getString("Value");
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

    public Pair<RestAPI.ServerResponse, String> parseServerPair(JSONObject obj){
        Pair<RestAPI.ServerResponse, String> response = null;
        try{
            if(!obj.getBoolean("Successful"))
                return response = new Pair<>(RestAPI.ServerResponse.ServerError, null);
            else {
                JSONObject pair = obj.getJSONObject("Value");
                RestAPI.ServerResponse[] responses = RestAPI.ServerResponse.values();
                for (RestAPI.ServerResponse resp : responses) {
                    if (resp.ordinal() == pair.get("Item1")) {
                        response = new Pair<RestAPI.ServerResponse, String>(resp, pair.getString("Item2"));
                        break;
                    }
                }
            }
        }catch(Exception ex){
            ex.printStackTrace();
        }
        return response;
    }

    public Pair<RestAPI.ServerResponse, LinkFeed> parseAddFeedResponse(JSONObject obj) {
        Pair<RestAPI.ServerResponse, LinkFeed> response = null;
        FeedResults results = null;
        try{
            if(!obj.getBoolean("Successful"))
                return new Pair<>(RestAPI.ServerResponse.ServerError, null);
            else {
                JSONObject resultsPair = obj.getJSONObject("Value");
                RestAPI.ServerResponse[] responses = RestAPI.ServerResponse.values();
                for (RestAPI.ServerResponse resp : responses) {
                    if (resp.ordinal() == resultsPair.get("Item1")) {
                        if(resp == RestAPI.ServerResponse.Success){
                            JSONObject resultsObj = resultsPair.getJSONObject("Item2");
                            JSONArray jKeywords = resultsObj.getJSONArray("keywords");
                            HashSet<String> keywords = new HashSet<String>();
                            for (int i = 0; i < jKeywords.length(); i++) {
                                keywords.add(jKeywords.getString(i));
                            }
                            results = new FeedResults(resultsObj.getString("url"), keywords, resultsObj.getString("htmlTags"),
                                    null, resultsObj.getString("recordid"), resultsObj.getString("resultsid"));
                        }
                        response = new Pair<>(resp, (LinkFeed)results);
                        break;
                    }
                }
            }
        }catch(Exception ex){
            ex.printStackTrace();
            Log.d(this.getClass().toString(), ex.getStackTrace().toString());
        }
        return response;
    }

    public List<FeedResults> parseAllUserData(JSONObject obj) {
        List<FeedResults> linkFeeds = new ArrayList<>();
        try{
            if(!obj.getBoolean("Successful"))
                return linkFeeds;
            else{
                JSONArray tupleList = obj.getJSONArray("Value");
                for(int i = 0; i < tupleList.length(); i++){
                    JSONObject tuple = tupleList.getJSONObject(i);
                    FeedResults feedResults = parseFeedResults(tuple.getJSONObject("Item1"));
                    feedResults.userPageRank = parseListOfLinks(tuple.getJSONArray("Item2"));
                    linkFeeds.add(feedResults);
                }
            }
        }catch(Exception ex){
            ex.printStackTrace();
            Log.d(this.getClass().toString(), ex.getStackTrace().toString());
        }
        return linkFeeds;
    }

    private FeedResults parseFeedResults(JSONObject obj) {
        FeedResults results = null;
        try{
            JSONArray jKeywords = obj.optJSONArray("keywords");
            HashSet<String> keywords = new HashSet<>();
            for(int i = 0; i < jKeywords.length(); i++){
                keywords.add(jKeywords.getString(i));
            }
            results = new FeedResults(obj.getString("url"), keywords, obj.getString("htmlTags"), null, obj.getString("recordid"), obj.getString("resultsid"));
        } catch(Exception ex){
            ex.printStackTrace();
            Log.d(this.getClass().toString(), ex.getStackTrace().toString());
        }
        return results;
    }

    private List<Link> parseListOfLinks(JSONArray jArray){
        List<Link> links = null;
        try{
            for(int i = 0; i < jArray.length(); i++){
                JSONObject jLink = jArray.getJSONObject(i);
                links.add(new Link(jLink.getString("url"), jLink.getString("innerText"), jLink.getInt("pageRank")));
            }
        }catch(Exception ex){
            ex.printStackTrace();
            Log.d(this.getClass().toString(), ex.getStackTrace().toString());
        }
        return links;
    }

    public List<Link> parseJavascriptStringify(JSONArray jArray){
        List<Link> links = new ArrayList<Link>();
        try{
            for(int i = 0; i < jArray.length(); i++){
                JSONArray pair = jArray.getJSONArray(i);
                links.add(new Link(pair.getString(0), pair.getString(1), (Integer)0));
            }
        }catch(Exception ex){
            ex.printStackTrace();
            Log.d(this.getClass().toString(), ex.getStackTrace().toString());
        }
        return links;
    }
}
