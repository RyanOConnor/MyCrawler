package com.android.ryan.cloudcrawlerclient;

import android.app.Service;
import android.content.Intent;
import android.os.IBinder;

/**
 * Created by Ryan on 4/14/2015.
 */
public class SyncService extends Service {

    private static SyncAdapter sSyncAdapter = null;
    private static final Object sSyncAdapterLock = new Object();

    public void onCreate(){
        synchronized (sSyncAdapterLock){
            if(sSyncAdapter == null){
                sSyncAdapter = new SyncAdapter(getApplicationContext(), true);
            }
        }
    }

    @Override
    public IBinder onBind(Intent intent){

        return sSyncAdapter.getSyncAdapterBinder();
    }
}
