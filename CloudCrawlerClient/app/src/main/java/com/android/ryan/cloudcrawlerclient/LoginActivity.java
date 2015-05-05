package com.android.ryan.cloudcrawlerclient;

import android.accounts.Account;
import android.accounts.AccountAuthenticatorActivity;
import android.accounts.AccountManager;
import android.content.ContentResolver;
import android.content.Context;
import android.content.Intent;
import android.os.AsyncTask;
import android.os.Bundle;
import android.util.Log;
import android.util.Pair;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.Toast;

import org.json.JSONObject;

import java.util.List;


public class LoginActivity extends AccountAuthenticatorActivity {

    public static final String AUTHORITY = "com.android.ryan.cloudcrawlerclient.provider";
    public static final String ACCOUNT_TYPE = "com.android.ryan.cloudcrawlerclient.account";
    public static final String CONTENT_AUTHORITY = "com.android.ryan.cloudcrawlerclient.provider";
    public static final long SYNC_FREQUENCY = 60*5;

    public static Account mAccount;

    EditText enteredUsername, enteredPassword;
    Button btnLogin, btnCreateAccount;
    Context context;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_login);
        context = this;

        if (AccessState.instance().userIsLoggedIn(context)) {
            startActivity(new Intent(LoginActivity.this, MainActivity.class));
        }

        enteredUsername = (EditText) findViewById(R.id.et_username);
        enteredPassword = (EditText) findViewById(R.id.et_password);
        btnLogin = (Button) findViewById(R.id.btn_Login);
        btnCreateAccount = (Button) findViewById(R.id.btn_Create_Account);

        btnLogin.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                //  -------ONLY FOR DEBUGGING PURPOSES------
                if (enteredUsername.getText().toString().isEmpty()) {
                    setLoggedInState("admin", "");
                    Intent intent = new Intent(LoginActivity.this, MainActivity.class);
                    startActivity(intent);
                    //  -------ONLY FOR DEBUGGING PURPOSES------
                } else {
                    String username = enteredUsername.getText().toString();
                    String password = enteredPassword.getText().toString();
                    new AsyncLogin().execute(username, password);
                }
            }
        });

        btnCreateAccount.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                //  -------ONLY FOR DEBUGGING PURPOSES------
                if (enteredUsername.getText().toString().isEmpty()) {
                    setLoggedInState("admin", "");
                    Intent intent = new Intent(LoginActivity.this, MainActivity.class);
                    startActivity(intent);
                    //  -------ONLY FOR DEBUGGING PURPOSES------
                } else {
                    String username = enteredUsername.getText().toString();
                    String password = enteredPassword.getText().toString();
                    if (username.length() >= 8) {
                        UserDetailsTable userDetails = new UserDetailsTable(username, password);
                        new AsyncCreateUser().execute(userDetails);
                    } else {
                        Toast.makeText(context, "Username must have 8 or more characters", Toast.LENGTH_LONG).show();
                    }
                }
            }
        });
    }

    private void addAccount(String username, String password){
        AccountManager accountManager = AccountManager.get(this);
        mAccount = new Account(username, ACCOUNT_TYPE);
        if(accountManager.addAccountExplicitly(mAccount, password, null)){
            ContentResolver.setIsSyncable(mAccount, CONTENT_AUTHORITY, 1);
            ContentResolver.setSyncAutomatically(mAccount, AUTHORITY, true);
            ContentResolver.addPeriodicSync(mAccount, CONTENT_AUTHORITY, new Bundle(), SYNC_FREQUENCY);
        } else {
            ContentResolver.setIsSyncable(mAccount, CONTENT_AUTHORITY, 1);
        }
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        // Inflate the menu; this adds items to the action bar if it is present.
        getMenuInflater().inflate(R.menu.menu_login, menu);
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        // Handle action bar item clicks here. The action bar will
        // automatically handle clicks on the Home/Up button, so long
        // as you specify a parent activity in AndroidManifest.xml.
        int id = item.getItemId();

        return super.onOptionsItemSelected(item);
    }

    public void setLoggedInState(String username, String userid) {
        AccessState.instance().setUserLoggedIn(context, username, userid);
        new AsyncGetAllUserData().execute(userid);
    }

    protected class AsyncLogin extends AsyncTask<String, JSONObject, String> {

        String username = null;

        @Override
        protected String doInBackground(String... params) {
            RestAPI api = new RestAPI();
            String userid = "";
            try {
                JSONObject jsonObj = api.UserAuthorization(params[0], params[1]);
                JSONParser parser = new JSONParser();
                userid = parser.parseUserAuth(jsonObj);
                username = params[0];

            } catch (Exception ex) {
                Log.d("AsyncLogin", ex.getMessage());
                ex.printStackTrace();
            }
            return userid;
        }

        @Override
        protected void onPreExecute() {
            super.onPreExecute();
            Toast.makeText(context, "Please Wait...", Toast.LENGTH_SHORT).show();
        }

        @Override
        protected void onPostExecute(String userid) {
            if (!userid.isEmpty()) {
                if (!userid.matches("[0]+")) {
                    addAccount(username, userid);
                    setLoggedInState(username, userid);
                } else {
                    Toast.makeText(context, "Invalid username/password", Toast.LENGTH_SHORT).show();
                }
            } else {
                Toast.makeText(context, "Server error, try again", Toast.LENGTH_SHORT).show();
            }
        }
    }
    protected class AsyncCreateUser extends AsyncTask<UserDetailsTable, Void, Pair<RestAPI.ServerResponse, String>> {

        String username, password;

        @Override
        protected Pair<RestAPI.ServerResponse, String> doInBackground(UserDetailsTable... params) {
            username = params[0].getUsername();
            password = params[0].getPassword();
            RestAPI api = new RestAPI();
            Pair<RestAPI.ServerResponse, String> response = null;
            try {
                JSONObject obj = api.CreateNewAccount(username, password);
                JSONParser parser = new JSONParser();
                response = parser.parseServerPair(obj);

            } catch (Exception ex) {
                Log.d("AsyncCreateUser", ex.getMessage());
            }
            return response;
        }

        @Override
        protected void onPostExecute(Pair<RestAPI.ServerResponse, String> response) {
            switch (response.first) {
                case Success:
                    addAccount(username, response.second);
                    setLoggedInState(username, response.second);
                    break;
                case UsernameAlreadyExists:
                    Toast.makeText(context, "Username Already Exists", Toast.LENGTH_SHORT).show();
                    break;
                case InvalidPassword:
                    Toast.makeText(context, "Invalid Password", Toast.LENGTH_SHORT).show();
                    break;
                case ServerError:
                    Toast.makeText(context, "Server Error", Toast.LENGTH_SHORT).show();
            }
        }
    }

    protected class AsyncGetAllUserData extends AsyncTask<String, Void, List<FeedResults>> {

        @Override
        protected List<FeedResults> doInBackground(String... params) {
            String userid = params[0];
            List<FeedResults> linkFeeds = null;
            RestAPI api = new RestAPI();
            try {
                JSONObject obj = api.RetrieveUpdates(userid);
                JSONParser parser = new JSONParser();
                linkFeeds = parser.parseAllUserData(obj);

            } catch (Exception ex) {
                ex.printStackTrace();
                Log.d(this.getClass().toString(), ex.getStackTrace().toString());
            }
            return linkFeeds;
        }

        @Override
        protected void onPostExecute(List<FeedResults> linkFeeds) {
            if (linkFeeds != null) {
                for (FeedResults linkFeed : linkFeeds)
                    StorageManager.instance().saveFeedResults(linkFeed, context);
            }
            Intent intent = new Intent(LoginActivity.this, MainActivity.class);
            startActivity(intent);
        }
    }
}