package com.android.ryan.cloudcrawlerclient;

import android.accounts.Account;
import android.accounts.AccountManager;
import android.app.AlertDialog;
import android.content.Context;
import android.content.DialogInterface;
import android.content.Intent;
import android.os.AsyncTask;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.widget.EditText;
import android.widget.Toast;

import org.json.JSONObject;

import java.util.List;

/**
 * Created by Ryan on 5/6/2015.
 */
public class NavigationDrawerDialogs {

    private static Context context = MainActivity.mContext;

    public static void showDeleteAccountDialog() {
        AlertDialog.Builder dialogBuilder = new AlertDialog.Builder(context);
        dialogBuilder.setCancelable(true);
        dialogBuilder.setTitle("Are you sure?");
        dialogBuilder.setNegativeButton("Yes", new DialogInterface.OnClickListener() {
            @Override
            public void onClick(DialogInterface dialogInterface, int which) {
                new AsyncTask<String, Void, RestAPI.ServerResponse>() {
                    @Override
                    protected RestAPI.ServerResponse doInBackground(String... params) {
                        String userid = params[0];
                        RestAPI.ServerResponse response = RestAPI.ServerResponse.ServerError;
                        RestAPI api = new RestAPI();
                        try {
                            JSONObject obj = api.DeleteUser(userid);
                            JSONParser parser = new JSONParser();
                            response = parser.parseServerResponse(obj);

                        } catch (Exception ex) {
                            ex.printStackTrace();
                            Log.d(this.getClass().toString(), ex.getStackTrace().toString());
                        }
                        return response;
                    }

                    @Override
                    protected void onPostExecute(RestAPI.ServerResponse response) {
                        switch (response) {
                            case Success:
                                deleteAccount();
                                StorageManager.instance().wipeAllUserData(context,
                                        AccessState.instance().getUserID(context));
                                AccessState.instance().setUserLoggedOut(context);
                                context.startActivity(new Intent(context,
                                        LoginActivity.class));
                                break;
                            case ServerError:
                                Toast.makeText(context, "Server Error - Try again", Toast.LENGTH_SHORT);
                                break;
                        }
                    }

                    protected void deleteAccount(){
                        AccountManager accountManager = AccountManager.get(context);
                        Account[] accounts = accountManager.getAccountsByType(LoginActivity.ACCOUNT_TYPE);
                        String username = AccessState.instance().getUserName(context);
                        for(Account account : accounts){
                            if(account.name == username) {
                                accountManager.removeAccount(account, null, null);
                                break;
                            }
                        }
                    }
                }.execute((AccessState.instance().getUserID(context)));
            }
        });
        dialogBuilder.setPositiveButton("No", new DialogInterface.OnClickListener() {
            @Override
            public void onClick(DialogInterface dialogInterface, int which) {
            }
        });
        AlertDialog dialog = dialogBuilder.create();
        dialog.show();
    }

    public static void showChangePasswordDialog() {
        AlertDialog.Builder dialogBuilder = new AlertDialog.Builder(context);
        dialogBuilder.setCancelable(true);
        LayoutInflater inflater = LayoutInflater.from(context);
        View view = inflater.inflate(R.layout.change_password_prompt, null);
        final EditText currentPassword = (EditText) view.findViewById(R.id.current_password);
        final EditText newPassword = (EditText) view.findViewById(R.id.new_password);
        dialogBuilder.setView(view);
        dialogBuilder.setPositiveButton("Ok", new DialogInterface.OnClickListener() {
            @Override
            public void onClick(DialogInterface dialog, int id) {
                String currentPass = currentPassword.getText().toString();
                String newPass = newPassword.getText().toString();
                new AsyncChangePassword().execute(currentPass, newPass);
            }
        });
        dialogBuilder.create();
        dialogBuilder.show();
    }

    public static void showModifyFeedDialog(int childPos, final String feedTitle) {
        final FeedResults results = StorageManager.instance().readFeedResults(feedTitle, context);
        AlertDialog.Builder dialogBuilder = new AlertDialog.Builder(context);
        dialogBuilder.setCancelable(true);
        dialogBuilder.setTitle(feedTitle);
        dialogBuilder.setNegativeButton("Modify", new DialogInterface.OnClickListener() {
            @Override
            public void onClick(DialogInterface dialogInterface, int which) {
                Intent intent = new Intent(context, TargetContentActivity.class);
                intent.putExtra("modifyingFeed", true);
                intent.putExtra("feedResults", results);
                context.startActivity(intent);
            }
        });
        dialogBuilder.setPositiveButton("Remove", new DialogInterface.OnClickListener() {
            @Override
            public void onClick(DialogInterface dialogInterface, int which) {
                AlertDialog.Builder builder = new AlertDialog.Builder(context);
                builder.setCancelable(true);
                builder.setTitle("Remove " + feedTitle + "?");
                builder.setNegativeButton("Yes", new DialogInterface.OnClickListener() {
                    @Override
                    public void onClick(DialogInterface dialogInterface, int which) {
                        new AsyncTask<String, Void, RestAPI.ServerResponse>() {
                            @Override
                            protected RestAPI.ServerResponse doInBackground(String... params) {
                                String userid = params[0];
                                String resultsid = params[1];
                                RestAPI.ServerResponse response = RestAPI.ServerResponse.ServerError;
                                RestAPI api = new RestAPI();
                                try {
                                    JSONObject obj = api.RemoveFeed(userid, resultsid);
                                    JSONParser parser = new JSONParser();
                                    response = parser.parseServerResponse(obj);
                                } catch (Exception ex) {
                                    ex.printStackTrace();
                                    Log.d(this.getClass().toString(), ex.getStackTrace().toString());
                                }
                                return response;
                            }

                            @Override
                            protected void onPostExecute(RestAPI.ServerResponse response) {
                                switch (response) {
                                    case Success:
                                        StorageManager.instance().wipeUserLinkFeed(context, results.getUserId(),
                                                results.getRecordId(), results.getResultsId());
                                        context.startActivity(new Intent(context, MainActivity.class));
                                        break;
                                    case ServerError:
                                        Toast.makeText(context, "Server Error - Try again", Toast.LENGTH_SHORT);
                                        break;
                                }
                            }
                        }.execute(AccessState.instance().getUserID(context), results.getResultsId());
                    }
                });
                builder.setPositiveButton("No", new DialogInterface.OnClickListener() {
                    @Override
                    public void onClick(DialogInterface dialog, int which) {
                    }
                });
                builder.create().show();
            }
        });
        dialogBuilder.create().show();
    }

    protected static class AsyncChangePassword extends AsyncTask<String, Void, RestAPI.ServerResponse> {

        @Override
        protected RestAPI.ServerResponse doInBackground(String... params){
            String userid = AccessState.instance().getUserID(context);
            String currentPassword = params[0];
            String newPassword = params[1];
            RestAPI api = new RestAPI();
            RestAPI.ServerResponse response = RestAPI.ServerResponse.ServerError;
            try{
                JSONObject obj = api.ChangePassword(userid, currentPassword, newPassword);
                JSONParser parser = new JSONParser();
                response = parser.parseServerResponse(obj);
            } catch(Exception ex) {
                ex.printStackTrace();
                Log.d(this.getClass().toString(), ex.getStackTrace().toString());
            }
            return response;
        }

        @Override
        protected void onPostExecute(RestAPI.ServerResponse response){
            switch (response){
                case Success:
                    Toast.makeText(context, "Password changed. Login again.", Toast.LENGTH_SHORT).show();
                    AccessState.instance().setUserLoggedOut(context);
                    context.startActivity(new Intent(context, LoginActivity.class));
                    break;
                case InvalidPassword:
                    Toast.makeText(context, "Password must be new", Toast.LENGTH_SHORT).show();
                    break;
                case InvalidPasswordType:
                    Toast.makeText(context, "Invalid password type.\nMake sure your password is at least 8 characters", Toast.LENGTH_SHORT).show();
                    break;
                case ServerError:
                    Toast.makeText(context, "Server Error - Try again", Toast.LENGTH_SHORT).show();
                    break;
            }
        }
    }
}