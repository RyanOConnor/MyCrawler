package com.android.ryan.cloudcrawlerclient;

import android.content.ContentProvider;
import android.content.ContentValues;
import android.database.Cursor;
import android.net.Uri;

import java.net.ContentHandler;

/**
 * Created by Ryan on 4/14/2015.
 */
public class StubProvider extends ContentProvider {

    @Override
    public boolean onCreate() {
        return true;
    }

    @Override
    public String getType(Uri uri) {
        return new String();
    }

    @Override
    public Cursor query(Uri uri, String[] projection, String selection, String[] selectionArgs, String sortOrder){
        return null;
    }

    @Override
    public Uri insert(Uri uri, ContentValues values){


        return null;
    }

    @Override
    public int delete(Uri uri, String selection, String[] selectionArgs){
        return 0 ;
    }

    public int update(Uri uri, ContentValues values, String selection, String[] selectionArgs){
        return 0;
    }
}
