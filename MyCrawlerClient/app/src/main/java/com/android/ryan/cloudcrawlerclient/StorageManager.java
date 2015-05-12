package com.android.ryan.cloudcrawlerclient;

import android.content.Context;
import android.os.Environment;
import android.provider.Browser;
import android.util.Log;

import java.io.BufferedOutputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.ObjectInputStream;
import java.io.ObjectOutputStream;
import java.io.OutputStream;
import java.util.ArrayList;

/**
 * Created by Ryan on 4/24/2015.
 */
public class StorageManager {

    private static final StorageManager _instance = new StorageManager();

    public static StorageManager instance(){
        synchronized (_instance){
            return _instance;
        }
    }

    public static final String DIRECTORY_PREFIX = "feeds";

    public void saveFeedResults(FeedResults feed, Context context){
        if(feed.userPageRank != null && feed.userPageRank.size() != 0) {
            String fileName = feed.url;
            if (fileName.startsWith("http://"))
                fileName = fileName.substring(7);
            if (fileName.startsWith("www."))
                fileName = fileName.substring(4);

            fileName = fileName.replaceAll("/", "&");
            try {
                feed.addUserId(AccessState.instance().getUserID(context));
                File file = new File(MainActivity.mContext.getFilesDir() + File.separator + DIRECTORY_PREFIX, fileName);
                FileOutputStream fStream = new FileOutputStream(file);
                ObjectOutputStream oStream = new ObjectOutputStream(fStream);
                oStream.writeObject(feed);
                oStream.close();
            } catch (Exception ex) {
                ex.printStackTrace();
                Log.d(this.getClass().toString(), ex.getStackTrace().toString());
            }
        }
    }

    public ArrayList<String> readFeedTitles(Context context){
        ArrayList<String> titles = new ArrayList<String>();
        try {
            String userid = AccessState.instance().getUserID(context);
            File directory = new File(MainActivity.mContext.getFilesDir() + File.separator + DIRECTORY_PREFIX);
            File[] files = directory.listFiles();
            if(files != null) {
                for (File file : files) {
                    FileInputStream fStream = new FileInputStream(file);
                    ObjectInputStream oStream = new ObjectInputStream(fStream);
                    FeedResults results = (FeedResults) oStream.readObject();
                    if (results.getUserId().equalsIgnoreCase(userid)) {
                        String title = file.getName().replace("&", "/");
                        titles.add(title);
                    }
                }
            }
        } catch (Exception ex) {
            ex.printStackTrace();
            Log.d(this.getClass().toString(), ex.getStackTrace().toString());
        }
        return titles;
    }

    public FeedResults readFeedResults(String filename, Context context){
        FeedResults results = null;
        try{
            String validFilename = filename.replaceAll("/", "&");
            File file = new File(MainActivity.mContext.getFilesDir() + File.separator +
                                    DIRECTORY_PREFIX + File.separator + validFilename);
            FileInputStream fStream = new FileInputStream(file);
            ObjectInputStream oStream = new ObjectInputStream(fStream);
            results = (FeedResults)oStream.readObject();
        } catch (Exception ex){
            ex.printStackTrace();
            Log.d(this.getClass().toString(), ex.getStackTrace().toString());
        }
        return results;
    }

    public void wipeUserLinkFeed(Context context, String userid, String recordid, String resultsid){
        try{
            File directory = new File(MainActivity.mContext.getFilesDir() + File.separator + DIRECTORY_PREFIX);
            File[] files = directory.listFiles();
            for(File file : files) {
                FileInputStream fStream = new FileInputStream(file);
                ObjectInputStream oStream = new ObjectInputStream(fStream);
                FeedResults results = (FeedResults)oStream.readObject();
                if(results.getUserId().equalsIgnoreCase(userid) &&
                        results.getRecordId().equalsIgnoreCase(recordid) &&
                        results.getResultsId().equalsIgnoreCase(resultsid)) {
                    file.delete();
                    File[] test = directory.listFiles();
                    break;
                }
            }
        } catch (Exception ex) {
            ex.printStackTrace();
            Log.d(this.getClass().toString(), ex.getStackTrace().toString());
        }
    }

    public void wipeAllUserData(Context context, String userid){
        try {
            File directory = new File(MainActivity.mContext.getFilesDir() + File.separator + DIRECTORY_PREFIX);
            File[] files = directory.listFiles();
            for(File file : files) {
                FileInputStream fStream = new FileInputStream(file);
                ObjectInputStream oStream = new ObjectInputStream(fStream);
                FeedResults results = (FeedResults)oStream.readObject();
                if(results.getUserId().equalsIgnoreCase(userid)) {
                    context.deleteFile(file.getName());
                }
            }
        } catch (Exception ex) {
            ex.printStackTrace();
            Log.d(this.getClass().toString(), ex.getStackTrace().toString());
        }
    }

}
