package com.android.ryan.cloudcrawlerclient;

import android.content.Context;
import android.content.Intent;
import android.os.AsyncTask;
import android.os.Bundle;
import android.support.v7.app.ActionBarActivity;
import android.util.Log;
import android.util.Pair;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.Toast;

import org.apache.http.message.BasicNameValuePair;
import org.json.JSONObject;

import java.util.List;


public class LoginActivity extends ActionBarActivity {

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
                if (enteredUsername.getText().toString().isEmpty()) {
                    setLoggedInState("admin", "");
                } else {
                    String username = enteredUsername.getText().toString();
                    String password = enteredPassword.getText().toString();
                    UserDetailsTable userDetails = new UserDetailsTable(username, password);
                    new AsyncCreateUser().execute(userDetails);
                }
            }
        });
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

        //noinspection SimplifiableIfStatement
        if (id == R.id.action_settings) {
            return true;
        }

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
            String userAuth = "";
            try {
                JSONObject jsonObj = api.UserAuthorization(params[0], params[1]);
                JSONParser parser = new JSONParser();
                userAuth = parser.parseUserAuth(jsonObj);
                username = params[0];

            } catch (Exception ex) {
                Log.d("AsyncLogin", ex.getMessage());
                ex.printStackTrace();
            }
            return userAuth;
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