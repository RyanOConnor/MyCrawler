package com.android.ryan.cloudcrawlerclient;

import android.accounts.AccountManager;
import android.app.Service;
import android.content.Intent;
import android.os.IBinder;

/**
 * Created by Ryan on 4/14/2015.
 */
public class AuthenticatorService extends Service{
    private Authenticator mAuthenticator;

    @Override
    public void onCreate() {

        mAuthenticator = new Authenticator(this);
    }

    @Override
    public IBinder onBind(Intent intent) {
        IBinder ret = null;
        if(intent.getAction().equals(AccountManager.ACTION_AUTHENTICATOR_INTENT))
            ret = getAuthenticator().getIBinder();
        return mAuthenticator.getIBinder();
    }

    private Authenticator getAuthenticator() {
        if(mAuthenticator == null)
            mAuthenticator = new Authenticator(this);
        return mAuthenticator;
    }
}
