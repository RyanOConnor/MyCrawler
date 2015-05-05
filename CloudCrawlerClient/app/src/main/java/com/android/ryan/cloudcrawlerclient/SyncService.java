package com.android.ryan.cloudcrawlerclient;

import android.accounts.Account;
import android.accounts.AccountManager;
import android.app.Service;
import android.content.AbstractThreadedSyncAdapter;
import android.content.ContentProviderClient;
import android.content.ContentResolver;
import android.content.Context;
import android.content.Intent;
import android.content.SyncResult;
import android.os.Bundle;
import android.os.IBinder;
import android.util.Log;

import org.json.JSONObject;

import java.util.List;

/**
 * Created by Ryan on 4/14/2015.
 */
public class SyncService extends Service {

    private static SyncAdapter sSyncAdapter = null;
    private static final Object sSyncAdapterLock = new Object();

    @Override
    public void onCreate(){
        super.onCreate();
        synchronized (sSyncAdapterLock){
            if(sSyncAdapter == null) {
                sSyncAdapter = new SyncAdapter(getApplicationContext(), true);
            }
        }
    }

    @Override
    public IBinder onBind(Intent intent){

        return sSyncAdapter.getSyncAdapterBinder();
    }

    public static void shutDownSync(){
        if(sSyncAdapter != null) {
            sSyncAdapter.mContentResolver.cancelSync(LoginActivity.mAccount, LoginActivity.AUTHORITY);
            sSyncAdapter.mContentResolver.setIsSyncable(LoginActivity.mAccount, LoginActivity.CONTENT_AUTHORITY, 0);
        }
    }

    public class SyncAdapter extends AbstractThreadedSyncAdapter {

        ContentResolver mContentResolver;

        public SyncAdapter(Context context, boolean autoInitialize) {
            super(context, autoInitialize);
            mContentResolver = context.getContentResolver();
        }

        @Override
        public void onPerformSync(Account account, Bundle extras, String authority,
                                  ContentProviderClient provider, SyncResult syncResult) {
            if(mContentResolver.isSyncPending(account, LoginActivity.AUTHORITY) ||
                    mContentResolver.isSyncActive(account, LoginActivity.AUTHORITY)){
                mContentResolver.cancelSync(account, LoginActivity.AUTHORITY);
            }
            RestAPI api = new RestAPI();
            try {
                String authToken = AccountManager.get(getContext()).getPassword(account);
                JSONObject obj = api.RetrieveUpdates(authToken);
                JSONParser parser = new JSONParser();
                List<FeedResults> linkFeeds = parser.parseAllUserData(obj);
                if(linkFeeds != null){
                    for(FeedResults linkFeed : linkFeeds){
                        StorageManager.instance().saveFeedResults(linkFeed, getContext());
                    }
                }
            } catch(Exception ex) {
                ex.printStackTrace();
                Log.d(this.getClass().toString(), ex.getStackTrace().toString());
            }
        }
    }
}
